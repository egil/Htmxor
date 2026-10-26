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
