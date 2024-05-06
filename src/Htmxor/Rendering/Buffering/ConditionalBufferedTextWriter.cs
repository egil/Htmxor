namespace Htmxor.Rendering.Buffering;

/// <summary>
/// A text writer that will only output when its <see cref="IsWritingEnabled"/> returns true.
/// </summary>
internal sealed class ConditionalBufferedTextWriter : BufferedTextWriter
{
	internal bool IsWritingEnabled { get; set; } = true;

	public ConditionalBufferedTextWriter(TextWriter underlying) : base(underlying)
	{
	}

	public override void Write(char value)
	{
		if (IsWritingEnabled)
		{
			base.Write(value);
		}
	}

	public override void Write(char[] buffer, int index, int count)
	{
		if (IsWritingEnabled)
		{
			base.Write(buffer, index, count);
		}
	}

	public override void Write(string? value)
	{
		if (IsWritingEnabled)
		{
			base.Write(value);
		}
	}

	public override void Write(int value)
	{
		if (IsWritingEnabled)
		{
			base.Write(value);
		}
	}
}