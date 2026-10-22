namespace Htmxor;

internal static class HtmxorRejectionReasons
{
	public const string RouteTarget = "the HtmxRoute target must be a concrete, non-generic Blazor component";
	public const string BlankTemplate = "HtmxRoute must resolve one nonblank constant route template";
	public const string RouteMethods = "explicit HtmxRoute Methods must resolve to a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY";
	public const string CurrentUrl = "HtmxRoute named argument 'CurrentUrl' must resolve to a valid relative or absolute HTTP(S) URI or null";
	public const string Target = "HtmxRoute named argument 'Target' must resolve to a valid element identity or null or whitespace";
	public const string Targets = "HtmxRoute named argument 'Targets' must resolve to a constant array of valid element identities";
	public const string StockRoute = "an HTMX-only component cannot also declare a normal Blazor route";
	public const string NormalOnlyRoute = "DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type";
	public const string UnroutedBinding = "the component owns no route, so the binding has no action to declare; add @page or HtmxRoute, or remove the binding";
	public const string LocalPageOwner = "a component with a local @page action must compile with exactly one stock route and no HtmxRoute";
	public const string NonlocalStockOwner = "a component action without a local @page cannot use a compiled stock route";
	public const string ExplicitVerb = "HtmxAction method must be POST, PUT, PATCH, DELETE or QUERY";
	public const string ExplicitOwner = "HtmxAction must be declared on the component type that owns its route, not an inherited or unrouted owner";
	public const string NormalOnlyExplicit = "HtmxAction is not supported on a component marked with DisableHtmxDirectRouting; remove the action or marker";
	public const string OrdinaryHandler = "is not an ordinary callback method; declare an ordinary instance method returning void or Task";
	public const string StaticHandler = "is static; make it an instance method";
	public const string GenericHandler = "is generic; make it non-generic";
	public const string AsyncVoid = "is async void; return Task instead";
	public const string ValueReturn = "returns a value; return void or Task";
	public const string ParameterCount = "has unsupported parameters; use zero parameters or one required HtmxEventArgs parameter";
	public const string ByRefParameter = "has unsupported parameters; use one by-value HtmxEventArgs parameter";
	public const string OptionalParameter = "has an optional parameter; make it required";
	public const string ParameterType = "has a parameter that is not HtmxEventArgs; its parameter must be HtmxEventArgs";
	public const string ExplicitOverload = "is overloaded; give the handler a unique name";
	public const string FormMethods = "explicit HtmxRoute.Methods does not allow POST for its form; add POST or remove the form";
	public const string NamedFormPost = "named forms and a POST action cannot share one route owner; use named forms or a POST action";
	public const string NamedFormExplicitPost = "named forms and a POST HtmxAction cannot share one route owner; choose named forms or the action";

	public static string InferredMethods(string method)
		=> "explicit HtmxRoute.Methods is authoritative and does not allow the " + method +
			" binding; add the method or remove the binding";

	public static string ExplicitMethods(string method)
		=> "explicit HtmxRoute.Methods does not allow the " + method + " HtmxAction; add the method or remove the action";

	public static string ConflictingHandlers(string method)
		=> method + " declarations on one route owner must name the same handler; select one handler";

	public static string NormalOnlyInferred(string bindingName = "")
		=> "an inferred" + bindingName + " binding on a component marked with DisableHtmxDirectRouting is not supported; remove the binding or marker";
}
