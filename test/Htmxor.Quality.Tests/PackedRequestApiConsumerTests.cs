using System.Text.RegularExpressions;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackedConsumerCollection.Name)]
public sealed class PackedRequestApiConsumerTests(PackedPackageFixture package)
{
	[Theory]
	[InlineData("net10.0")]
	[InlineData("net11.0")]
	public void Package_only_public_surface_omits_request_routing_details(string framework)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);

		var surface = PackagePublicSurface.Format(workspace.PackagePath, framework);
		var requestSurface = surface.Split("TYPE Htmxor.Http.HtmxRequest ", StringSplitOptions.None)[1]
			.Split("TYPE ", StringSplitOptions.None)[0];
		var retiredExports = requestSurface.Split('\n').Where(line =>
			Regex.IsMatch(line, @"\b(get_)?(RoutingMode|Method|Path)(\(\))?\s*$"))
			.Concat(surface.Split('\n').Where(line =>
				line.StartsWith("TYPE Htmxor.Http.RoutingMode ", StringComparison.Ordinal)));

		Assert.True(!retiredExports.Any(), "The package still exports retired request API:" + Environment.NewLine +
			string.Join(Environment.NewLine, retiredExports));
	}

	[Theory]
	[InlineData("net10.0", "10.0.402", "10.0.13")]
	[InlineData("net11.0", "11.0.100-rc.2.26504.105", "11.0.0-rc.2.26504.105")]
	public async Task Package_only_consumer_uses_request_type_and_HttpContext_but_cannot_use_retired_request_API(
		string framework, string sdk, string runtime)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find(), package);
		workspace.UseRequestApiScenario(framework, sdk, runtime);
		workspace.AddRetiredRequestApiReferences();
		var retired = await workspace.BuildForDiagnosticAsync();
		var output = retired.StandardOutput + Environment.NewLine + retired.StandardError;
		Assert.True(retired.ExitCode != 0, "The packed consumer still compiled references to all four retired API entries." + Environment.NewLine + output);
		Assert.All(new[] { 5, 6, 7, 8 }, line =>
			Assert.Matches(@"RetiredRequestApi\.cs\(" + line + @",\d+\): error CS(0122|0234|1061)", output));
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	public void UseRequestApiScenario(string framework, string sdk, string runtime)
	{
		UseSelectionScenario("Issue298");
		UseFramework(framework, sdk, runtime);
		File.WriteAllText(Path.Combine(consumerDirectory, "SupportedRequestApi.cs"),
			"""
			using Htmxor.Http;
			using Microsoft.AspNetCore.Http;
			public static class SupportedRequestApi
			{
			    public static HtmxRequestType? Representation(HtmxRequest request) => request.RequestType;
			    public static string Method(HttpContext context) => context.Request.Method;
			    public static PathString Path(HttpContext context) => context.Request.Path;
			}
			""");
	}

	public void AddRetiredRequestApiReferences() =>
		File.WriteAllText(Path.Combine(consumerDirectory, "RetiredRequestApi.cs"),
			"""
			using Htmxor.Http;
			public static class RetiredRequestApi
			{
			    public static object[] Read(HtmxRequest request) => new object[] {
			        Htmxor.Http.RoutingMode.Standard,
			        request.RoutingMode,
			        request.Method,
			        request.Path,
			    };
			}
			""");

	public Task<ProcessResult> BuildRequestApiProbeAsync() => BuildAsync();
}
