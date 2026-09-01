using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;

namespace Htmxor.Builder;

internal sealed class ConditionalResponseBodyStream(Stream inner, HtmxResponse response) : Stream
{
	[SuppressMessage(
		"Usage",
		"CA2213:Disposable fields should be disposed",
		Justification = "The server owns the wrapped response stream; this request-scoped adapter must not dispose it.")]
	private readonly Stream inner = inner ?? throw new ArgumentNullException(nameof(inner));
	private readonly HtmxResponse response = response ?? throw new ArgumentNullException(nameof(response));

	public override bool CanRead => inner.CanRead;

	public override bool CanSeek => inner.CanSeek;

	public override bool CanTimeout => inner.CanTimeout;

	public override bool CanWrite => inner.CanWrite;

	public override long Length => inner.Length;

	public override long Position
	{
		get => inner.Position;
		set => inner.Position = value;
	}

	public override int ReadTimeout
	{
		get => inner.ReadTimeout;
		set => inner.ReadTimeout = value;
	}

	public override int WriteTimeout
	{
		get => inner.WriteTimeout;
		set => inner.WriteTimeout = value;
	}

	public override void Flush()
	{
		_ = response.SuppressResponseBodyWrite();
		inner.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		_ = response.SuppressResponseBodyWrite();
		return inner.FlushAsync(cancellationToken);
	}

	public override int Read(byte[] buffer, int offset, int count)
		=> inner.Read(buffer, offset, count);

	public override int Read(Span<byte> buffer) => inner.Read(buffer);

	public override Task<int> ReadAsync(
		byte[] buffer,
		int offset,
		int count,
		CancellationToken cancellationToken)
		=> inner.ReadAsync(buffer, offset, count, cancellationToken);

	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer,
		CancellationToken cancellationToken = default)
		=> inner.ReadAsync(buffer, cancellationToken);

	public override int ReadByte() => inner.ReadByte();

	public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

	public override void SetLength(long value) => inner.SetLength(value);

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (!response.SuppressResponseBodyWrite())
		{
			inner.Write(buffer, offset, count);
		}
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (!response.SuppressResponseBodyWrite())
		{
			inner.Write(buffer);
		}
	}

	public override Task WriteAsync(
		byte[] buffer,
		int offset,
		int count,
		CancellationToken cancellationToken)
	{
		if (!response.SuppressResponseBodyWrite())
		{
			return inner.WriteAsync(buffer, offset, count, cancellationToken);
		}

		return cancellationToken.IsCancellationRequested
			? Task.FromCanceled(cancellationToken)
			: Task.CompletedTask;
	}

	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer,
		CancellationToken cancellationToken = default)
	{
		if (!response.SuppressResponseBodyWrite())
		{
			return inner.WriteAsync(buffer, cancellationToken);
		}

		return cancellationToken.IsCancellationRequested
			? ValueTask.FromCanceled(cancellationToken)
			: ValueTask.CompletedTask;
	}

	public override void WriteByte(byte value)
	{
		if (!response.SuppressResponseBodyWrite())
		{
			inner.WriteByte(value);
		}
	}
}
