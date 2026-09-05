using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Components;

namespace Htmxor.UpstreamMonitor;

internal static class TrustedFrameworkTypes
{
	private static readonly Lazy<IReadOnlyDictionary<string, WatchRelationship>> identities = new(ReadIdentities);

	public static bool Contains(string identity) => identities.Value.ContainsKey(identity);

	public static WatchRelationship Relationship(string identity) => identities.Value[identity];

	private static IReadOnlyDictionary<string, WatchRelationship> ReadIdentities()
	{
		// Only installed framework metadata defines trusted identities. Fetched upstream text never supplies assembly paths or executable code.
		var directory = Path.GetDirectoryName(typeof(ComponentBase).Assembly.Location)!;
		var result = new Dictionary<string, WatchRelationship>(StringComparer.Ordinal);
		foreach (var path in Directory.EnumerateFiles(directory, "Microsoft.AspNetCore.*.dll").Order(StringComparer.Ordinal))
		{
			ReadAssembly(path, result);
		}
		return result;
	}

	private static void ReadAssembly(string path, Dictionary<string, WatchRelationship> result)
	{
		using var stream = File.OpenRead(path);
		using var portableExecutable = new PEReader(stream);
		var metadata = portableExecutable.GetMetadataReader();
		foreach (var handle in metadata.TypeDefinitions)
		{
			var definition = metadata.GetTypeDefinition(handle);
			var typeNamespace = metadata.GetString(definition.Namespace);
			if (!typeNamespace.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal))
			{
				continue;
			}
			var identity = typeNamespace + "." + metadata.GetString(definition.Name);
			result[identity] = (definition.Attributes & TypeAttributes.Interface) != 0
				? WatchRelationship.Implements : WatchRelationship.Subclasses;
		}
	}
}
