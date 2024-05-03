namespace Htmxor.Rendering.Buffering;

/// <summary>
/// A text writer that will only output when its <see cref="ShouldWrite"/> returns true.
/// </summary>
internal sealed class ConditionalBufferedTextWriter : BufferedTextWriter
{
	internal bool ShouldWrite { get; set; } = true;

	public ConditionalBufferedTextWriter(TextWriter underlying) : base(underlying)
	{
	}

	public override void Write(char value)
	{
		if (ShouldWrite)
		{
			base.Write(value);
		}
	}

	public override void Write(char[] buffer, int index, int count)
	{
		if (ShouldWrite)
		{
			base.Write(buffer, index, count);
		}
	}

	public override void Write(string? value)
	{
		if (ShouldWrite)
		{
			base.Write(value);
		}
	}

	public override void Write(int value)
	{
		if (ShouldWrite)
		{
			base.Write(value);
		}
	}
}