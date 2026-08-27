using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class QualityOptionsTests
{
	[Theory]
	[InlineData("fast", "Fast")]
	[InlineData("full", "Full")]
	[InlineData("mutation", "Mutation")]
	public void Parse_maps_each_check_profile(string value, string expected)
	{
		var actual = QualityOptions.Parse(["check", "--profile", value]);

		Assert.Equal(QualityAction.Check, actual.Action);
		Assert.Equal(expected, actual.Profile.ToString());
	}

	[Fact]
	public void Parse_maps_fix()
	{
		var actual = QualityOptions.Parse(["fix"]);

		Assert.Equal(new QualityOptions(QualityAction.Fix, QualityProfile.Fast), actual);
	}

	[Theory]
	[InlineData()]
	[InlineData("check")]
	[InlineData("check", "--profile")]
	[InlineData("check", "--profile", "mutation-changed")]
	[InlineData("check", "--base", "main")]
	[InlineData("fix", "extra")]
	public void Parse_rejects_unsupported_shapes(params string[] args)
	{
		var exception = Assert.Throws<ArgumentException>(() => QualityOptions.Parse(args));

		Assert.Contains("Usage", exception.Message, StringComparison.Ordinal);
	}
}
