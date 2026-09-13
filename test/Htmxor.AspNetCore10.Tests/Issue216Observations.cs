#if NET11_0_OR_GREATER
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace Htmxor.AspNetCore10;

public sealed class Issue216Model
{
	[Required]
	public string Name { get; set; } = string.Empty;
}

internal sealed class Issue216Journal
{
	private readonly ConcurrentDictionary<string, Issue216Submission> submissions = new();
	public Issue216Submission For(string id) => submissions.GetOrAdd(id, _ => new());
}

internal sealed class Issue216Submission
{
	public ConcurrentQueue<(Guid Instance, string Phase)> Events { get; } = new();
	public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public void Record(Guid instance, string phase) => Events.Enqueue((instance, phase));
}
#endif
