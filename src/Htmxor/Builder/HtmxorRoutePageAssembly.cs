using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;

// Razor builds a component endpoint, and applies its conventions, only for a public [Route] page type that
// it discovers through Assembly.GetExportedTypes. A runtime AssemblyBuilder throws there, so the page types
// are persisted to a real assembly image and loaded. Each type is metadata only and never rendered: Htmxor's
// Finally convention swaps in the real component, and the type stays Router's compiled route processor.
internal static class HtmxorRoutePageAssembly
{
	private const string NamePrefix = "Htmxor.HtmxRoutePages.";

	// Loading two images with one name into a load context fails, and Router caches every processor type.
	// One assembly per template sequence serves every host in the process.
	private static readonly ConcurrentDictionary<string, Lazy<HtmxorRoutePages>> Assemblies = new(StringComparer.Ordinal);

	public static HtmxorRoutePages GetOrCreate(IReadOnlyList<string> templates)
	{
		ArgumentNullException.ThrowIfNull(templates);
		var key = string.Concat(templates.Select(static template => $"{template.Length}:{template}"));
		return Assemblies.GetOrAdd(
			key,
			static (key, templates) => new Lazy<HtmxorRoutePages>(() => Create(key, templates)),
			templates.ToArray()).Value;
	}

	private static HtmxorRoutePages Create(string key, string[] templates)
	{
		var name = NamePrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0, 16);
		var assembly = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
		var module = assembly.DefineDynamicModule(name);
		var routeConstructor = typeof(RouteAttribute).GetConstructor([typeof(string)])!;
		var typeNames = new string[templates.Length];
		for (var index = 0; index < templates.Length; index++)
		{
			var type = module.DefineType(
				$"{name}.HtmxRoutePage{index}",
				TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
				typeof(ComponentBase));
			type.DefineDefaultConstructor(MethodAttributes.Public);
			type.SetCustomAttribute(new CustomAttributeBuilder(routeConstructor, [templates[index]]));
			typeNames[index] = type.CreateType().FullName!;
		}

		using var image = new MemoryStream();
		assembly.Save(image);
		image.Position = 0;
		// Resolve the image's Components reference to the instance this process renders with.
		var loaded = (AssemblyLoadContext.GetLoadContext(typeof(ComponentBase).Assembly) ?? AssemblyLoadContext.Default)
			.LoadFromStream(image);
		return new HtmxorRoutePages(
			loaded,
			typeNames.Select(typeName => loaded.GetType(typeName, throwOnError: true)!).ToArray());
	}
}
