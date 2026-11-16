// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted for #262 from ASP.NET Core v10.0.15, commit 8e0f8ed2312954727f8919d7d1c2f78b483b4ee7, synchronized
// 2026-11-16; byte-identical at v11.0.0 (297acf930fb0f5c07fd4e51d78e521b4ad2e5d1f). Exact sources and license:
// docs/engineering/candidate-form-adapter.md.
// https://github.com/dotnet/dotnet/blob/v10.0.15/src/aspnetcore/src/Components/Endpoints/src/Rendering/Buffering/BufferedTextWriter.cs
// https://github.com/dotnet/dotnet/blob/8e0f8ed2312954727f8919d7d1c2f78b483b4ee7/src/aspnetcore/src/Components/Endpoints/src/Rendering/Buffering/BufferedTextWriter.cs
// https://github.com/dotnet/dotnet/blob/v11.0.0/src/aspnetcore/src/Components/Endpoints/src/Rendering/Buffering/BufferedTextWriter.cs
// https://github.com/dotnet/dotnet/blob/297acf930fb0f5c07fd4e51d78e521b4ad2e5d1f/src/aspnetcore/src/Components/Endpoints/src/Rendering/Buffering/BufferedTextWriter.cs
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/Buffering/BufferedTextWriter.cs | reimplements
// The chained flush follows upstream. A StringBuilder taken whole at each flush stands in for upstream's
// two swapped TextChunk page lists, which are not copied.

using System.Text;

namespace Htmxor.Endpoints;

// The renderer writes HTML synchronously, and a response writer that fills its own buffer mid-write writes to
// the response stream synchronously, which a server that disallows synchronous I/O rejects. This holds
// everything written between two flushes in memory, so only FlushAsync reaches the response.
internal sealed class HtmxorEndpointCandidateBufferedTextWriter(TextWriter underlying) : TextWriter
{
	private StringBuilder currentOutput = new();
	private Task currentFlushAsyncTask = Task.CompletedTask;

	public override Encoding Encoding => Encoding.UTF8;

	public override void Write(char value) => currentOutput.Append(value);

	public override void Write(char[] buffer, int index, int count) => currentOutput.Append(buffer, index, count);

	public override void Write(string? value) => currentOutput.Append(value);

	public override void Write(ReadOnlySpan<char> buffer) => currentOutput.Append(buffer);

	public override void Flush() => throw new NotSupportedException();

	public override Task FlushAsync()
	{
		currentFlushAsyncTask = FlushAsyncCore(currentFlushAsyncTask);
		return currentFlushAsyncTask;
	}

	private async Task FlushAsyncCore(Task priorTask)
	{
		// Flushes run one at a time and in order, so output written during a flush follows it.
		if (!priorTask.IsCompletedSuccessfully)
		{
			await priorTask;
		}

		// A fresh builder rather than a cleared one: clearing a builder that grew past one chunk allocates its
		// whole capacity again as a single array.
		var outputToFlush = currentOutput;
		currentOutput = new();
		foreach (var chunk in outputToFlush.GetChunks())
		{
			await underlying.WriteAsync(chunk);
		}

		await underlying.FlushAsync();
	}
}
