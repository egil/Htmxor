using System.Security.Cryptography;

namespace Htmxor.Quality.Tests;

public sealed class SampleRuntimeOwnershipTests
{
	private const string LegacyRuntimeHash =
		"73EABC44D978B226A667C62CA3C40E99236D11AA6F8FC8A27BE6F0B36A73B42D";

	[Theory]
	[InlineData("samples/BlazingPizza")]
	[InlineData("samples/HtmxorExamples")]
	[InlineData("samples/MinimalHtmxorApp")]
	public void Unsafe_sample_uses_application_owned_legacy_runtime_until_htmx4_adapter_is_proved(
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
			"legacy-htmx-1.9.12.min.js");

		Assert.Matches(@"hx-(post|put|patch|delete)", razorSources);
		Assert.Contains(
			"<meta name=\"htmx-config\"",
			app,
			StringComparison.Ordinal);
		Assert.Contains(
			"<script defer src=\"legacy-htmx-1.9.12.min.js\"></script>",
			app,
			StringComparison.Ordinal);
		Assert.DoesNotContain("htmx-4.0.0.min.js", app, StringComparison.Ordinal);
		Assert.True(File.Exists(assetPath), $"Missing application-owned asset: {assetPath}");
		Assert.Equal(
			LegacyRuntimeHash,
			Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assetPath))));
	}
}
