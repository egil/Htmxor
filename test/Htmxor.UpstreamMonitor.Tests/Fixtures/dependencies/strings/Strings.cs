namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.Strings;

internal static class Strings
{
	public const string Declaration = "internal class StringRenderer : Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure.StaticHtmlRenderer { }";
	public const string VerbatimDeclaration = @"internal interface IStringInvoker : Microsoft.AspNetCore.Components.Endpoints.IRazorComponentEndpointInvoker { }";
	public const string SourceExample = """
		using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
		internal class ExampleRenderer : StaticHtmlRenderer { }
		// Htmxor upstream dependency: src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs | mirrors
		// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
		""";
}
