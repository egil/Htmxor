using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Htmxor.AspNetCore10;

// Wraps a request's IHttpResponseBodyFeature so the test can pause the response at a point it defines by
// content rather than by guessing a fixed number of writes: once a write to the body has carried `marker`
// (the not-found template, in full, since HttpResponseStreamWriter always flushes its whole buffered string
// in one call), every later write, flush, or completion attempt on this response is suspended until the test
// releases it. Installed identically on both the stock and the candidate host (see
// Issue261PostStartNotFoundParityTests), so neither side gets special timing.
//
// This is deliberately not armed by the write that carries the marker itself: stock's own
// SignalRendererToFinishRendering runs only after that write's own HttpResponseStreamWriter.FlushAsync call
// has returned to its caller, so suspending inside that same call would delay the stop on stock too (see
// EndpointHtmlRenderer.EventDispatch.cs's SetNotFoundWhenResponseHasStarted, mirrored by
// HtmxorEndpointCandidateRenderer.Streaming.cs's WriteNotFoundAfterResponseStarted). Arming happens only once
// that write has already returned.
internal sealed class Issue261ResponseHold
{
	private readonly string marker;
	private readonly TaskCompletionSource holdReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly object sync = new();
	private bool markerWritten;
	private bool armed;

	public Issue261ResponseHold(string marker) => this.marker = marker;

	// Resolves once the response has been suspended at the first operation after the marker was written, or
	// (if no further stream operation exists before the endpoint's own request-handling task completes) once
	// EnsureHeldIfArmedAsync forces that same suspension from the middleware.
	public Task HoldReached => holdReached.Task;

	public void Release() => release.TrySetResult();

	internal async Task BeforeOperationAsync()
	{
		bool mustWait;
		lock (sync)
		{
			mustWait = armed;
		}

		if (mustWait)
		{
			holdReached.TrySetResult();
			await release.Task;
		}
	}

	internal void AfterWrite(ReadOnlySpan<byte> written)
	{
		lock (sync)
		{
			if (!markerWritten && Contains(written, marker))
			{
				markerWritten = true;
			}
		}
	}

	internal void AfterFlushOrComplete()
	{
		lock (sync)
		{
			if (markerWritten && !armed)
			{
				armed = true;
			}
		}
	}

	// Backstop for a response that never performs another body operation once the marker is written:
	// HttpResponseStreamWriter never flushes or disposes its underlying stream once its own char buffer is
	// already empty (see HttpResponseStreamWriter.FlushInternalAsync). Called by the middleware immediately
	// after the endpoint's own request-handling task has completed, so this still only engages once that task
	// -- including any stop the renderer set along the way -- has already finished.
	internal async Task EnsureHeldIfArmedAsync()
	{
		lock (sync)
		{
			if (markerWritten)
			{
				armed = true;
			}
		}

		await BeforeOperationAsync();
	}

	private static bool Contains(ReadOnlySpan<byte> haystack, string needle)
	{
		var needleBytes = Encoding.UTF8.GetBytes(needle);
		return haystack.IndexOf(needleBytes.AsSpan()) >= 0;
	}
}

// The response-body Stream half of the hold: every write, flush, and dispose attempt is routed through
// Issue261ResponseHold.BeforeOperationAsync before reaching the real stream.
internal sealed class Issue261HoldingStream(Stream inner, Issue261ResponseHold hold) : Stream
{
	public override bool CanRead => inner.CanRead;

	public override bool CanSeek => inner.CanSeek;

	public override bool CanWrite => inner.CanWrite;

	public override long Length => inner.Length;

	public override long Position
	{
		get => inner.Position;
		set => inner.Position = value;
	}

	public override void Flush() => FlushAsync(CancellationToken.None).GetAwaiter().GetResult();

	public override async Task FlushAsync(CancellationToken cancellationToken)
	{
		await hold.BeforeOperationAsync();
		await inner.FlushAsync(cancellationToken);
		hold.AfterFlushOrComplete();
	}

	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

	public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

	public override void SetLength(long value) => inner.SetLength(value);

	public override void Write(byte[] buffer, int offset, int count)
		=> WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		await hold.BeforeOperationAsync();
		await inner.WriteAsync(buffer, cancellationToken);
		hold.AfterWrite(buffer.Span);

		// A write that is itself empty, or that never contained the marker, cannot arm the hold; only
		// AfterFlushOrComplete (called once the writer's own flush for *that* write has returned to its
		// caller) does, per this type's own header comment.
	}

	protected override void Dispose(bool disposing)
	{
		// HttpResponseStreamWriter never disposes the stream it wraps: disposal also calls FlushInternalAsync,
		// which only ever reaches the stream through WriteAsync, and only when its own char buffer is
		// non-empty. This override exists so a future caller that does dispose the stream directly still
		// routes through the same armed-write bookkeeping instead of silently bypassing it.
		if (disposing)
		{
			hold.AfterFlushOrComplete();
		}

		base.Dispose(disposing);
	}
}

// The IHttpResponseBodyFeature half of the hold, so CompleteAsync -- a completion signal some hosts use
// instead of, or in addition to, a final stream write -- is covered the same way as Issue261HoldingStream
// covers Write/Flush/Dispose.
internal sealed class Issue261HoldingBodyFeature(IHttpResponseBodyFeature inner, Issue261ResponseHold hold) : IHttpResponseBodyFeature
{
	private readonly Issue261HoldingStream stream = new(inner.Stream, hold);

	public Stream Stream => stream;

	public System.IO.Pipelines.PipeWriter Writer => inner.Writer;

	public void DisableBuffering() => inner.DisableBuffering();

	public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);

	public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
		=> inner.SendFileAsync(path, offset, count, cancellationToken);

	public async Task CompleteAsync()
	{
		await hold.BeforeOperationAsync();
		await inner.CompleteAsync();
		hold.AfterFlushOrComplete();
	}
}

internal static class Issue261ResponseHoldMiddleware
{
	// Installed identically on both hosts via Issue260Host's configurePipeline hook. Holds the response open
	// at the first operation after the not-found template was written, or -- per Issue261ResponseHold's own
	// backstop -- immediately after the endpoint has otherwise finished, whichever comes first.
	public static void Use(WebApplication app, string marker, Action<Issue261ResponseHold> onCreated)
	{
		app.Use(async (context, next) =>
		{
			var hold = new Issue261ResponseHold(marker);
			onCreated(hold);
			var originalFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
			context.Features.Set<IHttpResponseBodyFeature>(new Issue261HoldingBodyFeature(originalFeature, hold));
			await next(context);
			await hold.EnsureHeldIfArmedAsync();
		});
	}
}
