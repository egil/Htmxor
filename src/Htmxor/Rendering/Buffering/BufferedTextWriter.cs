// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Htmxor.Rendering.Buffering;

internal class BufferedTextWriter : TextWriter
{
    private const int PageSize = 256;
	[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Instance not owned by this type.")]
	private readonly TextWriter underlying;
    private readonly StringBuilder charArraySegmentBuilder = new();
    private TextChunkListBuilder currentOutput;
    private TextChunkListBuilder? previousOutput;
    private Task currentFlushAsyncTask = Task.CompletedTask;

    public BufferedTextWriter(TextWriter underlying)
    {
        this.underlying = underlying;
        currentOutput = new(PageSize);
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
        => currentOutput.Add(new TextChunk(value));

    public override void Write(char[] buffer, int index, int count)
        => currentOutput.Add(new TextChunk(new ArraySegment<char>(buffer, index, count), charArraySegmentBuilder));

    public override void Write(string? value)
    {
        if (value is not null)
        {
            currentOutput.Add(new TextChunk(value));
        }
    }

    public override void Write(int value)
        => currentOutput.Add(new TextChunk(value));

    public override void Flush()
        => throw new NotSupportedException();

    public override Task FlushAsync()
    {
        currentFlushAsyncTask = FlushAsyncCore(currentFlushAsyncTask);
        return currentFlushAsyncTask;
    }

    private async Task FlushAsyncCore(Task priorTask)
    {
        // Must always wait for prior flushes to complete first, since they are
        // using _previousOutput and nothing else is allowed to do so
        if (!priorTask.IsCompletedSuccessfully)
        {
            await priorTask;
        }

        // Swap buffers
        var outputToFlush = currentOutput;
        currentOutput = previousOutput ?? new(PageSize);
        previousOutput = outputToFlush;

        await outputToFlush.WriteToAsync(underlying, charArraySegmentBuilder.ToString());
        outputToFlush.Clear();
        await underlying.FlushAsync();
    }
}
