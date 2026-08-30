using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Htmxor.Quality.Tests;

public sealed class SampleRuntimeOwnershipTests
{
	private const string Htmx4RuntimeHash =
		"E484D9171A9DB30A39C8F16E3D709D4137F3211C659F8E6125816635033D593F";

	[Theory]
	[InlineData("samples/BlazingPizza")]
	[InlineData("samples/HtmxorExamples")]
	[InlineData("samples/MinimalHtmxorApp")]
	public void Unsafe_sample_uses_application_owned_htmx4_without_legacy_configuration(
		string samplePath)
	{
		var repositoryRoot = RepositoryLocator.Find();
		var sampleRoot = Path.Combine(repositoryRoot, samplePath);
		var app = File.ReadAllText(Path.Combine(sampleRoot, "Components", "App.razor"));
		var razorSources = string.Join(
			Environment.NewLine,
			Directory.EnumerateFiles(sampleRoot, "*.razor", SearchOption.AllDirectories)
				.Select(File.ReadAllText));
		var assetPath = Path.Combine(
			sampleRoot,
			"wwwroot",
			"htmx-4.0.0.min.js");

		Assert.Matches(@"hx-(post|put|patch|delete)", razorSources);
		Assert.True(
			ContainsStockAntiforgeryUi(razorSources),
			$"Expected {samplePath} to use EditForm or AntiforgeryToken for stock antiforgery.");
		Assert.DoesNotContain("<meta name=\"htmx-config\"", app, StringComparison.Ordinal);
		Assert.Contains(
			"<script defer src=\"htmx-4.0.0.min.js\"></script>",
			app,
			StringComparison.Ordinal);
		Assert.DoesNotContain("legacy-htmx", app, StringComparison.Ordinal);
		Assert.True(File.Exists(assetPath), $"Missing application-owned asset: {assetPath}");
		Assert.Equal(
			Htmx4RuntimeHash,
			Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assetPath))));
		Assert.False(File.Exists(Path.Combine(sampleRoot, "wwwroot", "legacy-htmx-1.9.12.min.js")));
	}

	[Theory]
	[InlineData("<EditForm Model=\"model\">", true)]
	[InlineData("<AntiforgeryToken />", true)]
	[InlineData("<form method=\"post\">", false)]
	[InlineData("<EditFormWrapper>", false)]
	[InlineData("<AntiforgeryTokenProvider />", false)]
	public void Stock_antiforgery_ui_accepts_both_supported_components(
		string razorSource,
		bool expected)
	{
		Assert.Equal(expected, ContainsStockAntiforgeryUi(razorSource));
	}

	private static bool ContainsStockAntiforgeryUi(string razorSource)
		=> Regex.IsMatch(
			razorSource,
			@"<(?:EditForm|AntiforgeryToken)(?=\s|/?>)",
			RegexOptions.CultureInvariant);
}
