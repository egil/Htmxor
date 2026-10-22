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
			throw Unsupported(component, HtmxorRejectionReasons.ExplicitVerb);
		}

		ValidateOwner(component, manifest);
		ValidateHandler(component, handler, handlers);
		ValidateAllowList(component, normalized);
		if (normalized == "POST" && namedForms.Contains(component.FullName!, StringComparer.Ordinal))
		{
			throw Unsupported(component, HtmxorRejectionReasons.NamedFormExplicitPost);
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
		if (!typeof(IComponent).IsAssignableFrom(component))
		{
			throw Unsupported(component, HtmxorRejectionReasons.ExplicitOwner);
		}

		if (component.IsAbstract || component.ContainsGenericParameters ||
			!manifest.Contains(component.FullName!, StringComparer.Ordinal))
		{
			throw Unsupported(component, "HtmxAction must be declared on the concrete component type that owns its route");
		}

		if (GetRoutes(component).Length == 0)
		{
			throw Unsupported(component, HtmxorRejectionReasons.ExplicitOwner);
		}

		if (component.IsDefined(typeof(DisableHtmxDirectRoutingAttribute), inherit: false))
		{
			throw Unsupported(component, HtmxorRejectionReasons.NormalOnlyExplicit);
		}
	}

	private static void ValidateHandler(Type component, MethodInfo handler, MethodInfo[] handlers)
	{
		if (!IsOrdinaryHandler(handler))
		{
			throw Unsupported(component, HtmxorRejectionReasons.OrdinaryHandler);
		}

		if (GetHandlerShapeReason(handler) is { } shapeReason)
		{
			throw Unsupported(component, shapeReason);
		}

		if (GetParameterReason(handler.GetParameters()) is { } parameterReason)
		{
			throw Unsupported(component, parameterReason);
		}

		if (handlers.Count(candidate => candidate.Name == handler.Name) > 1)
		{
			throw Unsupported(component, HtmxorRejectionReasons.ExplicitOverload);
		}
	}

	private static string? GetHandlerShapeReason(MethodInfo handler)
	{
		if (handler.IsStatic)
		{
			return HtmxorRejectionReasons.StaticHandler;
		}
		if (handler.IsGenericMethod)
		{
			return HtmxorRejectionReasons.GenericHandler;
		}
		if (handler.ReturnType == typeof(void))
		{
			return handler.IsDefined(typeof(AsyncStateMachineAttribute), inherit: false)
				? HtmxorRejectionReasons.AsyncVoid : null;
		}

		return handler.ReturnType != typeof(Task) ? HtmxorRejectionReasons.ValueReturn : null;
	}

	private static string? GetParameterReason(ParameterInfo[] parameters)
	{
		if (parameters.Length > 1)
		{
			return HtmxorRejectionReasons.ParameterCount;
		}
		foreach (var parameter in parameters)
		{
			if (parameter.ParameterType.IsByRef)
			{
				return HtmxorRejectionReasons.ByRefParameter;
			}
			if (parameter.IsOptional)
			{
				return HtmxorRejectionReasons.OptionalParameter;
			}
			if (parameter.ParameterType != typeof(HtmxEventArgs))
			{
				return HtmxorRejectionReasons.ParameterType;
			}
		}

		return null;
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
			throw Unsupported(component, HtmxorRejectionReasons.ExplicitMethods(method));
		}
	}

	public static HtmxorComponentActionDescriptor[] Merge(IEnumerable<HtmxorComponentActionDescriptor> actions)
		=> actions.GroupBy(static action => action.HttpMethod, StringComparer.OrdinalIgnoreCase).Select(MergeMethod).ToArray();

	private static HtmxorComponentActionDescriptor MergeMethod(IGrouping<string, HtmxorComponentActionDescriptor> actions)
	{
		var first = actions.First();
		if (actions.Any(action => action.HandlerIdentity != first.HandlerIdentity))
		{
			throw Unsupported(first.ComponentType, HtmxorRejectionReasons.ConflictingHandlers(actions.Key));
		}

		return actions.FirstOrDefault(static action => action.GeneratedAction is not null) ?? first;
	}

	private static InvalidOperationException Unsupported(Type component, string reason)
		=> new($"Component '{component.FullName}' has an invalid explicit action: {reason}.");
}
