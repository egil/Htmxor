using System.Collections.Concurrent;

namespace Htmxor.AspNetCore10;

internal sealed class Issue214OverlapGate
{
	private readonly ConcurrentDictionary<string, string> arrivals = new(StringComparer.Ordinal);
	private readonly TaskCompletionSource allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private int participants;

	public void HoldRequests(int count) => participants = count;

	public async Task ArriveAsync(string user, string value)
	{
		if (participants == 0)
		{
			return;
		}

		if (!arrivals.TryAdd(user, value))
		{
			throw new InvalidOperationException($"Duplicate overlap participant: {user}");
		}

		if (arrivals.Count == participants)
		{
			allArrived.TrySetResult();
		}

		await released.Task.WaitAsync(TimeSpan.FromSeconds(20));
	}

	public async Task<KeyValuePair<string, string>[]> WaitForArrivalsAsync()
	{
		await allArrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
		return arrivals.OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
	}

	public void Release() => released.TrySetResult();
}
