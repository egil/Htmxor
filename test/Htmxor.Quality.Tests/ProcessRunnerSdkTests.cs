using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class ProcessRunnerSdkTests
{
	[Fact]
	public async Task External_workspace_uses_its_pinned_SDK_instead_of_the_parent_test_SDK()
	{
		using var workspace = new TemporaryDirectory();
		File.WriteAllText(Path.Combine(workspace.Path, "global.json"),
			"""{"sdk":{"version":"10.0.400","rollForward":"disable"}}""");
		File.WriteAllText(Path.Combine(workspace.Path, "Probe.csproj"),
			"""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""");
		var result = await new ProcessRunner().RunAsync(new("dotnet", workspace.Path,
			["msbuild", "Probe.csproj", "-getProperty:NETCoreSdkVersion,MSBuildSDKsPath"], EnsureSuccess: false));
		Assert.True(result.ExitCode == 0, result.StandardOutput + result.StandardError);
		using var document = JsonDocument.Parse(result.StandardOutput);
		var properties = document.RootElement.GetProperty("Properties");
		Assert.Equal("10.0.400", properties.GetProperty("NETCoreSdkVersion").GetString());
		Assert.EndsWith(Path.Combine("10.0.400", "Sdks"), properties.GetProperty("MSBuildSDKsPath").GetString());
	}
}
