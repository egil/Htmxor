using System.Text.Json;

namespace Htmxor.Quality;

internal enum MutationStatus
{
	Pending,
	Killed,
	Survived,
	Timeout,
	CompileError,
	Ignored,
	NoCoverage,
	RuntimeError,
}

internal sealed record MutationCharacterization(
	int Generated,
	int Eligible,
	int Killed,
	int Survived,
	int Skipped,
	int TimedOut,
	int Errors,
	int Pending)
{
	public IReadOnlyList<string> GetValidityFailures()
	{
		var failures = new List<string>();
		if (Generated == 0)
		{
			failures.Add("Stryker generated zero mutants.");
		}

		if (Eligible == 0)
		{
			failures.Add("Stryker produced zero eligible mutants.");
		}

		if (Killed == 0)
		{
			failures.Add("Stryker killed zero mutants.");
		}

		if (TimedOut > 0)
		{
			failures.Add($"Stryker reported {TimedOut} timed-out mutants.");
		}

		if (Errors > 0)
		{
			failures.Add($"Stryker reported {Errors} error mutants.");
		}

		if (Pending > 0)
		{
			failures.Add($"Stryker left {Pending} mutants pending.");
		}

		return failures;
	}
}

internal static class MutationReport
{
	public static MutationCharacterization Characterize(string json)
	{
		using var document = JsonDocument.Parse(json);
		var statuses = new List<MutationStatus>();
		CollectStatuses(document.RootElement, statuses);
		return new(
			Generated: statuses.Count,
			Eligible: statuses.Count(status => !IsSkipped(status)),
			Killed: Count(statuses, MutationStatus.Killed),
			Survived: Count(statuses, MutationStatus.Survived),
			Skipped: statuses.Count(IsSkipped),
			TimedOut: Count(statuses, MutationStatus.Timeout),
			Errors: Count(statuses, MutationStatus.RuntimeError),
			Pending: Count(statuses, MutationStatus.Pending));
	}

	private static int Count(IReadOnlyCollection<MutationStatus> statuses, MutationStatus expected) =>
		statuses.Count(status => status == expected);

	private static bool IsSkipped(MutationStatus status) =>
		status is MutationStatus.Ignored or MutationStatus.NoCoverage or MutationStatus.CompileError;

	private static void CollectStatuses(JsonElement element, ICollection<MutationStatus> statuses)
	{
		if (element.ValueKind == JsonValueKind.Object)
		{
			CollectFromObject(element, statuses);
		}
		else if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (var child in element.EnumerateArray())
			{
				CollectStatuses(child, statuses);
			}
		}
	}

	private static void CollectFromObject(JsonElement element, ICollection<MutationStatus> statuses)
	{
		foreach (var property in element.EnumerateObject())
		{
			if (property.NameEquals("mutants"))
			{
				CollectMutants(property.Value, statuses);
			}
			else
			{
				CollectStatuses(property.Value, statuses);
			}
		}
	}

	private static void CollectMutants(JsonElement mutants, ICollection<MutationStatus> statuses)
	{
		if (mutants.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException("A Stryker mutants value was not an array.");
		}

		foreach (var mutant in mutants.EnumerateArray())
		{
			var value = mutant.TryGetProperty("status", out var status)
				? status.GetString()
				: null;
			statuses.Add(ParseStatus(value));
		}
	}

	private static MutationStatus ParseStatus(string? status)
	{
		var parsed = status?.ToUpperInvariant() switch
		{
			"PENDING" => MutationStatus.Pending,
			"KILLED" => MutationStatus.Killed,
			"SURVIVED" => MutationStatus.Survived,
			"TIMEOUT" => MutationStatus.Timeout,
			"COMPILEERROR" => MutationStatus.CompileError,
			"IGNORED" => MutationStatus.Ignored,
			"NOCOVERAGE" => MutationStatus.NoCoverage,
			"RUNTIMEERROR" => MutationStatus.RuntimeError,
			_ => (MutationStatus?)null,
		};
		if (parsed is not null)
		{
			return parsed.Value;
		}

		throw new InvalidOperationException(
			$"Unknown Stryker mutant status '{status ?? "<missing>"}'.");
	}
}
