using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using Htmxor.DependencyInjection;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using static Htmxor.LinkerFlags;
using RouteData = Microsoft.AspNetCore.Components.RouteData;

namespace Htmxor.Rendering;

/// <summary>
/// </summary>
internal partial class HtmxorRenderer : Renderer
{
	private static readonly Type HttpContextFormDataProviderType = AppDomain
			.CurrentDomain
			.GetAssemblies()
			.SelectMany(assembly => assembly.GetTypes())
			.First(t => string.Equals(t.FullName, "Microsoft.AspNetCore.Components.Endpoints.HttpContextFormDataProvider", StringComparison.Ordinal));

	private static readonly Task CanceledRenderTask = Task.FromCanceled(new CancellationToken(canceled: true));

	private readonly IServiceProvider services;
	private readonly RazorComponentsServiceOptions options;
	private readonly NavigationManager? navigationManager;
	private readonly Dictionary<ulong, (string HtmxorEventId, Delegate Handler)> htmxorEventsByEventHandlerId = new();
	private HttpContext httpContext = default!; // Always set at the start of an inbound call
	private HtmxContext htmxContext = default!; // Always set at the start of an inbound call

	internal HttpContext? HttpContext => httpContext;

	internal HtmxContext? HtmxContext => htmxContext;

	/// <inheritdoc/>
	public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

	public HtmxorRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		: base(serviceProvider, loggerFactory)
	{
		services = serviceProvider;
		options = serviceProvider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>().Value;
		navigationManager = serviceProvider.GetService<NavigationManager>();
		htmlEncoder = serviceProvider.GetService<HtmlEncoder>() ?? HtmlEncoder.Default;
		javaScriptEncoder = serviceProvider.GetService<JavaScriptEncoder>() ?? JavaScriptEncoder.Default;
	}

	private void SetHttpContext(HttpContext httpContext)
	{
		if (this.httpContext is null)
		{
			this.httpContext = httpContext;
			this.htmxContext = httpContext.GetHtmxContext();
		}
		else if (this.httpContext != httpContext)
		{
			throw new InvalidOperationException("The HttpContext cannot change value once assigned.");
		}
	}

	/// <summary>
	/// Adds a root component of the specified type and begins rendering it.
	/// </summary>
	/// <param name="componentType">The component type. This must implement <see cref="IComponent"/>.</param>
	/// <param name="initialParameters">Parameters for the component.</param>
	/// <returns>An <see cref="HtmxorRootComponent"/> that can be used to obtain the rendered HTML.</returns>
	public HtmxorRootComponent BeginRenderingComponent(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
		ParameterView initialParameters)
	{
		var component = InstantiateComponent(componentType);
		return BeginRenderingComponent(component, initialParameters);
	}

	/// <summary>
	/// Adds a root component and begins rendering it.
	/// </summary>
	/// <param name="component">The root component instance to be added and rendered. This must not already be associated with any renderer.</param>
	/// <param name="initialParameters">Parameters for the component.</param>
	/// <returns>An <see cref="HtmxorRootComponent"/> that can be used to obtain the rendered HTML.</returns>
	public HtmxorRootComponent BeginRenderingComponent(
		IComponent component,
		ParameterView initialParameters)
	{
		var componentId = AssignRootComponentId(component);
		var quiescenceTask = RenderRootComponentAsync(componentId, initialParameters);

		if (quiescenceTask.IsFaulted)
		{
			ExceptionDispatchInfo.Capture(quiescenceTask.Exception.InnerException ?? quiescenceTask.Exception).Throw();
		}

		return new HtmxorRootComponent(this, componentId, quiescenceTask);
	}

	/// <inheritdoc/>
	protected override void HandleException(Exception exception)
		=> ExceptionDispatchInfo.Capture(exception).Throw();

	internal static void InitializeStandardComponentServices(
		HttpContext httpContext,
		[DynamicallyAccessedMembers(Component)] Type? componentType = null,
		[DynamicallyAccessedMembers(Component)] Type? layoutType = null,
		string? handler = null,
		IFormCollection? form = null)
	{
		var hostEnvNavMan = httpContext.RequestServices.GetRequiredService<NavigationManager>() as IHostEnvironmentNavigationManager;
		hostEnvNavMan?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));

		if (httpContext.RequestServices.GetService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider authenticationStateProvider)
		{
			var authenticationState = new AuthenticationState(httpContext.User);
			authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
		}

		if (form is not null)
		{
			// This code has been replaced by the reflection based code below it.
			//httpContext
			//    .RequestServices
			//    .GetRequiredService<HttpContextFormDataProvider>()
			//    .SetFormData(handler ?? "", new FormCollectionReadOnlyDictionary(form), form.Files);

			var httpContextFormDataProvider = httpContext
				.RequestServices
				.GetService(HttpContextFormDataProviderType);
			HttpContextFormDataProviderType
				.GetMethod("SetFormData", BindingFlags.Instance | BindingFlags.Public)!
				.Invoke(httpContextFormDataProvider, [handler ?? "", new FormCollectionReadOnlyDictionary(form), form.Files]);
		}

		var antiforgery = httpContext.RequestServices.GetRequiredService<AntiforgeryStateProvider>();
		if (antiforgery.GetType().GetMethod("SetRequestContext", BindingFlags.Instance | BindingFlags.NonPublic) is MethodInfo setRequestContextMethod)
		{
			setRequestContextMethod.Invoke(antiforgery, [httpContext]);
		}

		if (componentType is not null)
		{
			SetRouteData(httpContext, componentType, layoutType);
		}
	}

	protected override HtmxorComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
		=> new HtmxorComponentState(this, componentId, component, parentComponentState as HtmxorComponentState);

	internal void WriteComponentHtml(int componentId, TextWriter output)
	{
		var componentState = (HtmxorComponentState)GetComponentState(componentId);
		WriteRootComponent(componentState, output);
	}

	private static void SetRouteData(HttpContext httpContext, Type componentType, Type? layoutType)
	{
		// Saving RouteData to avoid routing twice in Router component
		var routingStateProvider = httpContext.RequestServices.GetRequiredService<EndpointRoutingStateProvider>();
		routingStateProvider.LayoutType = layoutType;
		routingStateProvider.RouteData = new RouteData(componentType, httpContext.GetRouteData().Values);
		if (httpContext.GetEndpoint() is RouteEndpoint endpoint)
		{
			routingStateProvider.RoutePattern = endpoint.RoutePattern;
			routingStateProvider.RouteData.Template = endpoint.RoutePattern.RawText;
		}
	}

	protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
	{
		UpdateHtmxorEvents(in renderBatch);
		UpdateNamedSubmitEvents(in renderBatch);
		return CanceledRenderTask;
	}

	private static string GetFullUri(HttpRequest request)
	{
		return UriHelper.BuildAbsolute(
			request.Scheme,
			request.Host,
			request.PathBase,
			request.Path,
			request.QueryString);
	}

	private static string GetContextBaseUri(HttpRequest request)
	{
		var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

		// PathBase may be "/" or "/some/thing", but to be a well-formed base URI
		// it has to end with a trailing slash
		return result.EndsWith('/') ? result : result += "/";
	}

	private sealed class FormCollectionReadOnlyDictionary : IReadOnlyDictionary<string, StringValues>
	{
		private readonly IFormCollection form;
		private List<StringValues>? values;

		public FormCollectionReadOnlyDictionary(IFormCollection form)
		{
			this.form = form;
		}

		public StringValues this[string key] => form[key];

		public IEnumerable<string> Keys => form.Keys;

		public IEnumerable<StringValues> Values => values ??= MaterializeValues(form);

		private static List<StringValues> MaterializeValues(IFormCollection form)
		{
			var result = new List<StringValues>(form.Keys.Count);
			foreach (var key in form.Keys)
			{
				result.Add(form[key]);
			}

			return result;
		}

		public int Count => form.Count;

		public bool ContainsKey(string key)
		{
			return form.ContainsKey(key);
		}

		public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
		{
			return form.GetEnumerator();
		}

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value)
		{
			return form.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return form.GetEnumerator();
		}
	}
}
