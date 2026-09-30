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
		while (!ContainsAny(markers) && await TryReadChunkAsync(cancellation.Token))
		{
		}

		return buffer.ToString();
	}

	// Reads until the stream ends (a fixed short quiet period counts as "ended" for this purpose: stock and a
	// fixed candidate both close their connection once RenderComponentCore returns) or the timeout elapses.
	public async Task<string> ReadToEndOrTimeoutAsync(TimeSpan? timeout = null)
	{
		using var cancellation = new CancellationTokenSource(timeout ?? DefaultTimeout);
		while (await TryReadChunkAsync(cancellation.Token))
		{
		}

		return buffer.ToString();
	}

	private bool ContainsAny(string[] markers) =>
		markers.Length > 0 && markers.Any(marker => buffer.ToString().Contains(marker, StringComparison.Ordinal));

	private async Task<bool> TryReadChunkAsync(CancellationToken cancellationToken)
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

	public ValueTask DisposeAsync() => stream.DisposeAsync();
}
