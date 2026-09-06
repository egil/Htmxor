namespace Htmxor.Quality;

internal enum QualityAction
{
	Check,
	Fix,
}

internal enum QualityProfile
{
	Fast,
	Full,
	Mutation,
	Upstream,
}

internal sealed record QualityOptions(QualityAction Action, QualityProfile Profile)
{
	private const string Usage =
		"Usage: check --profile fast|full|mutation|upstream | fix";

	public static QualityOptions Parse(IReadOnlyList<string> args)
	{
		if (args.Count == 1 && args[0].Equals("fix", StringComparison.Ordinal))
		{
			return new(QualityAction.Fix, QualityProfile.Fast);
		}

		if (args.Count != 3 ||
			!args[0].Equals("check", StringComparison.Ordinal) ||
			!args[1].Equals("--profile", StringComparison.Ordinal))
		{
			throw new ArgumentException(Usage);
		}

		var profile = args[2] switch
		{
			"fast" => QualityProfile.Fast,
			"full" => QualityProfile.Full,
			"mutation" => QualityProfile.Mutation,
			"upstream" => QualityProfile.Upstream,
			_ => throw new ArgumentException($"Unknown quality profile '{args[2]}'. {Usage}"),
		};

		return new(QualityAction.Check, profile);
	}
}
