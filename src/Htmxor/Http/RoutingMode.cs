using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Htmxor.Http;

/// <summary>
/// Specifies the routing mode used by Htmxor to handle a HTTP request.
/// </summary>
internal enum RoutingMode
{
	/// <summary>
	/// In standard routing mode is the same as normal Blazor Static Web app
	/// page routing, i.e. a request will go through the root component
	/// (specified by <see cref="RazorComponentsEndpointRouteBuilderExtensions.MapRazorComponents{TRootComponent}(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)"/>),
	/// and the root component will decide what to do with the request,
	/// usually rendering the <see cref="Router"/> component which will
	/// inspect the HTTP request and then render the component/page that
	/// matches the request, usually wrapped in a <see cref="LayoutComponentBase"/>.
	/// </summary>
	Standard,

	/// <summary>
	/// In direct routing mode, Htmxor will route direct to the component/page that
	/// matches the request. If the component specifies an <see cref="HtmxLayoutAttribute"/>,
	/// the highest-priority inherited attribute selects the layout surrounding whole-component output.
	/// Named-fragment selection emits only the requested fragments from the layout or component.
	/// The matched component's <see cref="LayoutAttribute"/> does not select its direct layout.
	/// </summary>
	Direct,
}
