using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;

internal static class HtmxorExplicitComponentActionCatalog
{
	private const BindingFlags DeclaredMethods = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	private static readonly string[] SupportedMethods = ["POST", "PUT", "PATCH", "DELETE", "QUERY"];

	public static IReadOnlyList<HtmxorComponentActionDescriptor> Build(Assembly applicationAssembly,
		IReadOnlyList<string> routedComponentTypeNames, IReadOnlyList<string> namedFormComponentTypeNames)
	{
		ArgumentNullException.ThrowIfNull(applicationAssembly);
		ArgumentNullException.ThrowIfNull(routedComponentTypeNames);
		ArgumentNullException.ThrowIfNull(namedFormComponentTypeNames);
		var actions = new List<HtmxorComponentActionDescriptor>();
		foreach (var component in applicationAssembly.DefinedTypes)
		{
			var handlers = component.GetMethods(DeclaredMethods);
			foreach (var handler in handlers)
			{
				foreach (var attribute in handler.GetCustomAttributes<HtmxActionAttribute>(inherit: false))
				{
					AddAction(actions, component, handler, attribute.Method, handlers, routedComponentTypeNames, namedFormComponentTypeNames);
				}
			}
		}

		return actions;
	}

	private static void AddAction(List<HtmxorComponentActionDescriptor> actions, Type component, MethodInfo handler,
		string method, MethodInfo[] handlers, IReadOnlyList<string> manifest, IReadOnlyList<string> namedForms)
	{
		var normalized = SupportedMethods.FirstOrDefault(allowed => string.Equals(method, allowed, StringComparison.OrdinalIgnoreCase));
		if (normalized is not ("POST" or "PUT" or "PATCH" or "DELETE" or "QUERY"))
		{
			throw Unsupported(component, "HtmxAction method must be POST, PUT, PATCH, DELETE or QUERY");
		}

		ValidateOwner(component, manifest);
		ValidateHandler(component, handler, handlers);
		ValidateAllowList(component, normalized);
		if (normalized == "POST" && namedForms.Contains(component.FullName!, StringComparer.Ordinal))
		{
			throw Unsupported(component, "named forms and a POST action cannot share one route owner");
		}

		foreach (var route in GetRoutes(component))
		{
			actions.Add(new HtmxorComponentActionDescriptor(component, route, normalized,
				component.FullName + "." + normalized + "." + handler.Name, ExplicitHandler: handler));
		}
	}

	private static string[] GetRoutes(Type component)
		=> component.GetCustomAttributes<RouteAttribute>(inherit: false).Select(static route => route.Template)
			.Concat(component.GetCustomAttributes<HtmxRouteAttribute>(inherit: false).Select(static route => route.Template)).ToArray();

	private static void ValidateOwner(Type component, IReadOnlyList<string> manifest)
	{
		if (!typeof(IComponent).IsAssignableFrom(component) || component.IsAbstract || component.ContainsGenericParameters ||
			!manifest.Contains(component.FullName!, StringComparer.Ordinal) || GetRoutes(component).Length == 0)
		{
			throw Unsupported(component, "HtmxAction must be declared on the concrete component type that owns its route");
		}

		if (component.IsDefined(typeof(DisableHtmxDirectRoutingAttribute), inherit: false))
		{
			throw Unsupported(component, "HtmxAction is not supported with DisableHtmxDirectRouting");
		}
	}

	private static void ValidateHandler(Type component, MethodInfo handler, MethodInfo[] handlers)
	{
		if (!IsOrdinaryHandler(handler))
		{
			throw Unsupported(component, "HtmxAction handler must be an ordinary instance callback method");
		}

		if (handler.IsStatic || handler.IsGenericMethod ||
			(handler.ReturnType == typeof(void) && handler.IsDefined(typeof(AsyncStateMachineAttribute), inherit: false)) ||
			(handler.ReturnType != typeof(void) && handler.ReturnType != typeof(Task)))
		{
			throw Unsupported(component, "HtmxAction handler must be a non-generic instance method returning void or Task, never async void");
		}

		var parameters = handler.GetParameters();
		if (parameters.Length > 1 || parameters.Any(static parameter =>
			parameter.IsOptional || parameter.ParameterType != typeof(HtmxEventArgs)))
		{
			throw Unsupported(component, "HtmxAction handler must have zero parameters or one required HtmxEventArgs parameter");
		}

		if (handlers.Count(candidate => candidate.Name == handler.Name) > 1)
		{
			throw Unsupported(component, "HtmxAction handler must not be overloaded");
		}
	}

	// C# emits local functions and lambdas with names that cannot be authored as identifiers.
	private static bool IsOrdinaryHandler(MethodInfo handler)
		=> !(handler.IsPrivate && handler.IsVirtual && handler.IsFinal) &&
			(handler.Name.Length == 0 || handler.Name[0] != '<') &&
			(handler.Name != "Finalize" || handler.GetBaseDefinition().DeclaringType != typeof(object)) &&
			!IsPropertyAccessor(handler);

	private static bool IsPropertyAccessor(MethodInfo handler)
		=> handler.DeclaringType!.GetProperties(DeclaredMethods).Any(property =>
			property.GetMethod == handler || property.SetMethod == handler);

	private static void ValidateAllowList(Type component, string method)
	{
		var route = component.CustomAttributes.FirstOrDefault(static attribute => attribute.AttributeType == typeof(HtmxRouteAttribute));
		var methods = route?.NamedArguments.Where(static argument => argument.MemberName == nameof(HtmxRouteAttribute.Methods)).ToArray() ?? [];
		if (methods.Length > 0 &&
			(methods[0].TypedValue.Value is not IEnumerable<CustomAttributeTypedArgument> values ||
			 !values.Any(value => value.Value is string allowed && string.Equals(allowed, method, StringComparison.OrdinalIgnoreCase))))
		{
			throw Unsupported(component, $"explicit HtmxRoute.Methods does not allow the {method} HtmxAction");
		}
	}

	public static HtmxorComponentActionDescriptor[] Merge(IEnumerable<HtmxorComponentActionDescriptor> actions)
		=> actions.GroupBy(static action => action.HttpMethod, StringComparer.OrdinalIgnoreCase).Select(MergeMethod).ToArray();

	private static HtmxorComponentActionDescriptor MergeMethod(IGrouping<string, HtmxorComponentActionDescriptor> actions)
	{
		var first = actions.First();
		if (actions.Any(action => action.HandlerIdentity != first.HandlerIdentity))
		{
			throw Unsupported(first.ComponentType, $"{actions.Key} declarations must name the same handler");
		}

		return actions.FirstOrDefault(static action => action.GeneratedAction is not null) ?? first;
	}

	private static InvalidOperationException Unsupported(Type component, string reason)
		=> new($"Component '{component.FullName}' has an invalid explicit action: {reason}.");
}
