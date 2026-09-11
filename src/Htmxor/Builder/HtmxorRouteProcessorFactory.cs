using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;

internal static class HtmxorRouteProcessorFactory
{
	// Router requires a compiled RouteAttribute and caches the resulting type. Reuse metadata-only
	// types so repeated endpoint construction does not create another framework route-cache entry.
	private static readonly ConcurrentDictionary<string, Type> Processors = new(StringComparer.Ordinal);

	public static Type GetOrCreate(string validatedTemplate, Type? generatedProcessor)
		=> generatedProcessor is not null && generatedProcessor.GetCustomAttributes<RouteAttribute>()
			.Any(route => string.Equals(route.Template, validatedTemplate, StringComparison.Ordinal))
			? generatedProcessor
			: Processors.GetOrAdd(validatedTemplate, Create);

	private static Type Create(string template)
	{
		var assembly = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("Htmxor.RouteProcessor"),
			AssemblyBuilderAccess.RunAndCollect);
		var module = assembly.DefineDynamicModule("Htmxor.RouteProcessor");
		var type = module.DefineType(
			"HtmxorRouteProcessor",
			TypeAttributes.NotPublic | TypeAttributes.Sealed,
			typeof(ComponentBase));
		type.SetCustomAttribute(new CustomAttributeBuilder(
			typeof(RouteAttribute).GetConstructor([typeof(string)])!,
			[template]));
		return type.CreateType()!;
	}
}
