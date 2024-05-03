// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Htmxor.Rendering.Buffering;

// This behaves like a List<TextChunk>, but is more optimized for growing to larger sizes
// since its underlying storage is pages rather than a single contiguous array. That means
// when expanding it doesn't have to copy the old data to a new location.
internal class TextChunkListBuilder(int pageLength)
{
	private TextChunkPage? currentPage;
	private List<TextChunkPage>? priorPages;

	public void Add(TextChunk value)
	{
		if (currentPage is null)
		{
			currentPage = new TextChunkPage(pageLength);
		}

		if (!currentPage.TryAdd(value))
		{
			priorPages ??= new();
			priorPages.Add(currentPage);
			currentPage = new TextChunkPage(pageLength);
			if (!currentPage.TryAdd(value))
			{
				throw new InvalidOperationException("New page didn't accept write");
			}
		}
	}

	public async Task WriteToAsync(TextWriter writer, string charArraySegments)
	{
		StringBuilder? tempBuffer = null;

		if (priorPages is not null)
		{
			foreach (var page in priorPages)
			{
				var (count, buffer) = (page.Count, page.Buffer);
				for (var i = 0; i < count; i++)
				{
					await buffer[i].WriteToAsync(writer, charArraySegments, ref tempBuffer);
				}
			}
		}

		if (currentPage is not null)
		{
			var (count, buffer) = (currentPage.Count, currentPage.Buffer);
			for (var i = 0; i < count; i++)
			{
				await buffer[i].WriteToAsync(writer, charArraySegments, ref tempBuffer);
			}
		}
	}

	public void Clear()
	{
		if (currentPage is not null)
		{
			currentPage = null;
		}

		priorPages?.Clear();
	}
}
