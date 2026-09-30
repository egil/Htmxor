using System.Text;

namespace Htmxor.AspNetCore10;

// Reads a switch-on streaming response incrementally instead of buffering the whole body, so a test can prove
// ordering: that content already arrived before a gate release, and whether anything further arrives after.
internal sealed class Issue264StreamingBodyReader : IAsyncDisposable
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

	private readonly Stream stream;
	private readonly StringBuilder buffer = new();
	private readonly byte[] chunk = new byte[4096];
	private bool faulted;

	private Issue264StreamingBodyReader(Stream stream) => this.stream = stream;

	public static async Task<Issue264StreamingBodyReader> CreateAsync(HttpResponseMessage response)
		=> new(await response.Content.ReadAsStreamAsync());

	// Reads until the accumulated body so far contains any one of `markers`, or the timeout elapses. Returns
	// everything read so far either way (including on timeout), and the caller decides whether that satisfies
	// its assertion; a timeout here is expected, not exceptional, when a marker never arrives.
	public async Task<string> ReadUntilAsync(string[] markers, TimeSpan? timeout = null)
	{
		using var cancellation = new CancellationTokenSource(timeout ?? DefaultTimeout);
		while (!ContainsAny(markers) && await TryReadChunkLenientAsync(cancellation.Token))
		{
		}

		return buffer.ToString();
	}

	// Reads until the stream reaches a genuine end-of-stream (a real zero-byte read), which is what stock and
	// a fixed candidate both produce once RenderComponentCore returns and the connection closes. Unlike
	// ReadUntilAsync, a timeout or a cancelled/faulted read here is not completion and must not be reported as
	// though it were: a failed release call, a delayed continuation, or a hung response would otherwise yield
	// an empty tail and let a parity assertion pass without ever exercising the forbidden post-navigation
	// update (see PR #265 review discussion_r4140656945). Both failure modes now throw instead of returning.
	public async Task<string> ReadToEndAsync(TimeSpan? timeout = null)
	{
		var deadline = timeout ?? DefaultTimeout;
		using var cancellation = new CancellationTokenSource(deadline);
		while (await ReadChunkOrThrowAsync(cancellation.Token, deadline) > 0)
		{
		}

		return buffer.ToString();
	}

	private bool ContainsAny(string[] markers) =>
		markers.Length > 0 && markers.Any(marker => buffer.ToString().Contains(marker, StringComparison.Ordinal));

	private async Task<bool> TryReadChunkLenientAsync(CancellationToken cancellationToken)
	{
		if (faulted)
		{
			return false;
		}

		try
		{
			var read = await stream.ReadAsync(chunk, cancellationToken);
			if (read == 0)
			{
				return false;
			}

			buffer.Append(Encoding.UTF8.GetString(chunk, 0, read));
			return true;
		}
		catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or IOException)
		{
			// A cancelled read on a chunked HTTP response can leave the underlying connection torn down rather
			// than merely paused (observed: a later read then throws ObjectDisposedException instead of a
			// second, cleanly catchable cancellation). Either way, no further bytes are coming on this stream.
			faulted = true;
			return false;
		}
	}

	private async Task<int> ReadChunkOrThrowAsync(CancellationToken cancellationToken, TimeSpan timeout)
	{
		int read;
		try
		{
			read = await stream.ReadAsync(chunk, cancellationToken);
		}
		catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or IOException)
		{
			throw new TimeoutException(
				$"Expected the stream to reach a real end-of-stream within {timeout}, but the read was cancelled " +
				$"or the connection faulted instead ({exception.GetType().Name}: {exception.Message}). A genuine " +
				$"zero-byte read is the only thing that counts as completion here. Accumulated so far: {buffer}",
				exception);
		}

		if (read > 0)
		{
			buffer.Append(Encoding.UTF8.GetString(chunk, 0, read));
		}

		return read;
	}

	public ValueTask DisposeAsync() => stream.DisposeAsync();
}
