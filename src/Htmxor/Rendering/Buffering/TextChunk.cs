// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Htmxor.Rendering.Buffering;

// Holds different types of outputs that can be written to BufferedTextWriter
internal readonly struct TextChunk
{
	private readonly TextChunkType type;

	// If expanding this struct to hold many other possible value types,
	// consider making it into a [StructLayout(Layout.Explicit)] so different
	// value/reference types can share the same memory slots, discriminated
	// by _type. That will reduce memory usage and improve locality.
	private readonly string? stringValue;
	private readonly char charValue;
	private readonly int charArraySegmentStart;
	private readonly int charArraySegmentLength;
	private readonly int intValue;

	public TextChunk(string value)
	{
		type = TextChunkType.String;
		stringValue = value;
	}

	public TextChunk(char value)
	{
		type = TextChunkType.Char;
		charValue = value;
	}

	public TextChunk(ArraySegment<char> value, StringBuilder charArraySegmentScope)
	{
		// An ArraySegment<char> is mutable (as in, its underlying buffer is). So
		// we must copy its value. To avoid this being a separate allocation each time,
		// use a StringBuilder as a growable buffer for these values. We rely on
		// the caller of WriteToAsync being able to supply the .ToString() result
		// of that StringBuilder, since we don't want to call that on each WriteToAsync.
		type = TextChunkType.CharArraySegment;
		charArraySegmentStart = charArraySegmentScope.Length;
		charArraySegmentLength = value.Count;
		charArraySegmentScope.Append((Span<char>)value);
	}

	public TextChunk(int value)
	{
		type = TextChunkType.Int;
		intValue = value;
	}

	public Task WriteToAsync(TextWriter writer, string charArraySegments, ref StringBuilder? tempBuffer)
	{
		switch (type)
		{
			case TextChunkType.String:
				return writer.WriteAsync(stringValue);
			case TextChunkType.Char:
				return writer.WriteAsync(charValue);
			case TextChunkType.CharArraySegment:
				return writer.WriteAsync(charArraySegments.AsMemory(charArraySegmentStart, charArraySegmentLength));
			case TextChunkType.Int:
				// The same technique could be used to optimize writing other
				// nonstring types, but currently only int is often used
				tempBuffer ??= new();
				tempBuffer.Clear();
				tempBuffer.Append(intValue);
				return writer.WriteAsync(tempBuffer);
			default:
				throw new InvalidOperationException($"Unknown type {type}");
		}
	}

	private enum TextChunkType { Int, String, Char, CharArraySegment };
}
