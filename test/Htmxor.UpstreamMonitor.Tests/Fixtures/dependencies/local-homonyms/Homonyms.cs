using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.RenderTree;
using LocalRenderer = Htmxor.UpstreamMonitor.Tests.DependencyFixtures.LocalHomonyms.StaticHtmlRenderer;

namespace Htmxor.UpstreamMonitor.Tests.DependencyFixtures.LocalHomonyms;

internal class StaticHtmlRenderer
{
}

internal class Renderer
{
}

internal interface IRazorComponentEndpointInvoker
{
}

internal sealed class LocalHtmlRenderer : StaticHtmlRenderer
{
}

internal sealed class LocalTreeRenderer : Renderer
{
}

internal interface ILocalInvoker : IRazorComponentEndpointInvoker
{
}

internal sealed class LocalAliasedRenderer : LocalRenderer
{
}
