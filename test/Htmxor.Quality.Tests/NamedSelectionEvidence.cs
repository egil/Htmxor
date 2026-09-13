using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal static class NamedSelectionEvidence
{
	public static void AssertPackageTarget(string consumerDirectory, string packageVersion, string framework)
	{
		using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(consumerDirectory, "obj", "project.assets.json")));
		var target = assets.RootElement.GetProperty("targets").GetProperty(framework).GetProperty("Htmxor/" + packageVersion);
		Assert.Equal("lib/" + framework + "/Htmxor.dll", Assert.Single(target.GetProperty("compile").EnumerateObject()).Name);
		Assert.Equal("lib/" + framework + "/Htmxor.dll", Assert.Single(target.GetProperty("runtime").EnumerateObject()).Name);
	}

	public static void Retain(string consumerDirectory, string trxPath, ProcessResult result, string scenario, string framework)
	{
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue218", scenario, framework);
		Directory.CreateDirectory(evidence);
		File.Copy(trxPath, Path.Combine(evidence, "consumer.trx"), overwrite: true);
		File.Copy(Path.Combine(consumerDirectory, "obj", "project.assets.json"), Path.Combine(evidence, "project.assets.json"), overwrite: true);
		File.Copy(Path.Combine(consumerDirectory, "global.json"), Path.Combine(evidence, "global.json"), overwrite: true);
		var projectPath = Directory.EnumerateFiles(consumerDirectory, "*.csproj").Single();
		File.Copy(projectPath, Path.Combine(evidence, Path.GetFileName(projectPath)), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
	}
}
