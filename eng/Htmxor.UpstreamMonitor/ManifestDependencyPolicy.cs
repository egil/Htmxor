using System.Text.RegularExpressions;

namespace Htmxor.UpstreamMonitor;

internal static partial class ManifestDependencyPolicy
{
	// This map supplies reviewed source locations only. Trusted framework metadata independently determines which local relationships require coverage.
	private static readonly IReadOnlyDictionary<string, string> frameworkSources = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["Microsoft.AspNetCore.Components.RenderTree.Renderer"] = "src/Components/Components/src/RenderTree/Renderer.cs",
		["Microsoft.AspNetCore.Components.Rendering.ComponentState"] = "src/Components/Components/src/Rendering/ComponentState.cs",
		["Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure.StaticHtmlRenderer"] = "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs",
		["Microsoft.AspNetCore.Components.Endpoints.IRazorComponentEndpointInvoker"] = "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs",
		["Microsoft.AspNetCore.Components.ComponentBase"] = "src/Components/Components/src/ComponentBase.cs",
		["Microsoft.AspNetCore.Components.Forms.AntiforgeryStateProvider"] = "src/Components/Web/src/Forms/AntiforgeryStateProvider.cs",
		["Microsoft.AspNetCore.Components.Forms.InputBase`1"] = "src/Components/Web/src/Forms/InputBase.cs",
		["Microsoft.AspNetCore.Components.IComponent"] = "src/Components/Components/src/IComponent.cs",
		["Microsoft.AspNetCore.Components.LayoutComponentBase"] = "src/Components/Components/src/LayoutComponentBase.cs",
		["Microsoft.AspNetCore.Components.NavigationException"] = "src/Components/Components/src/NavigationException.cs",
		["Microsoft.AspNetCore.Components.NavigationManager"] = "src/Components/Components/src/NavigationManager.cs",
		["Microsoft.AspNetCore.Components.Routing.IHostEnvironmentNavigationManager"] = "src/Components/Components/src/Routing/IHostEnvironmentNavigationManager.cs",
		["Microsoft.AspNetCore.Components.Routing.IRoutingStateProvider"] = "src/Components/Components/src/Routing/IRoutingStateProvider.cs",
		["Microsoft.AspNetCore.Html.IHtmlAsyncContent"] = "src/Html.Abstractions/src/IHtmlAsyncContent.cs",
		["Microsoft.AspNetCore.Routing.EndpointDataSource"] = "src/Http/Routing/src/EndpointDataSource.cs",
		["Microsoft.AspNetCore.Routing.MatcherPolicy"] = "src/Http/Routing/src/Matching/MatcherPolicy.cs",
		["Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy"] = "src/Http/Routing/src/Matching/IEndpointSelectorPolicy.cs",
	};

	public static IReadOnlyList<string> FindMissingDependencies(string repositoryRoot, WatchManifest manifest) =>
		manifest.Targets.SelectMany(target => target.LocalDependencies).Distinct(StringComparer.Ordinal)
			.Where(path => !File.Exists(Path.Combine(repositoryRoot, path))).Order(StringComparer.Ordinal).ToArray();

	public static IReadOnlyList<LocalFrameworkDependency> FindUntrackedDependencies(string repositoryRoot, WatchManifest manifest)
	{
		var root = Path.Combine(repositoryRoot, "src", "Htmxor");
		if (!Directory.Exists(root))
		{
			return [];
		}
		var sources = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
			.Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
			.ToDictionary(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'), path => new CSharpSource(File.ReadAllText(path)));
		var localTypes = sources.Values.SelectMany(source => source.Types)
			.Select(type => Qualify(type.Scope, CSharpTypeName.MetadataIdentity(type.Name))).ToHashSet(StringComparer.Ordinal);
		return sources.SelectMany(source => Discover(source.Key, source.Value, localTypes))
			.Distinct().Where(dependency => !Covered(manifest, dependency))
			.OrderBy(dependency => dependency.LocalPath, StringComparer.Ordinal).ThenBy(dependency => dependency.UpstreamPath, StringComparer.Ordinal)
			.ThenBy(dependency => dependency.Relationship).ToArray();
	}

	private static bool Covered(WatchManifest manifest, LocalFrameworkDependency dependency) => manifest.Targets.Any(watch =>
		UpstreamMonitorApplication.Matches(watch, dependency.UpstreamPath) && watch.Relationship == dependency.Relationship &&
		watch.LocalDependencies.Contains(dependency.LocalPath, StringComparer.Ordinal));

	private static IEnumerable<LocalFrameworkDependency> Discover(string localPath, CSharpSource source, HashSet<string> localTypes)
	{
		foreach (var comment in source.Comments)
		{
			var marker = Provenance().Match(comment);
			if (marker.Success)
			{
				yield return new(localPath, marker.Groups[1].Value, WatchManifestFile.ParseRelationship(marker.Groups[2].Value));
			}
		}
		var imports = Imports().Matches(source.Text).Select(match => match.Groups[1].Value).ToArray();
		foreach (var type in source.Types)
		{
			foreach (var name in type.Bases)
			{
				var identity = Resolve(name, type.Scope, source, imports, localTypes);
				if (identity is not null && TrustedFrameworkTypes.Contains(identity))
				{
					var upstreamPath = frameworkSources.GetValueOrDefault(identity, "unresolved:" + identity);
					yield return new(localPath, upstreamPath, TrustedFrameworkTypes.Relationship(identity));
				}
			}
		}
	}

	private static string? Resolve(string name, string scope, CSharpSource source, string[] imports, HashSet<string> localTypes)
	{
		name = CSharpTypeName.MetadataIdentity(ExpandAlias(name, source));
		if (IsLocal(name, scope, localTypes))
		{
			return null;
		}
		if (name.Contains('.', StringComparison.Ordinal))
		{
			return name;
		}
		if (imports.Any(import => localTypes.Contains(Qualify(import, name))))
		{
			return null;
		}
		return imports.Select(import => import + "." + name).FirstOrDefault(TrustedFrameworkTypes.Contains);
	}

	private static bool IsLocal(string name, string scope, HashSet<string> localTypes)
	{
		if (name.Contains('.', StringComparison.Ordinal))
		{
			return localTypes.Contains(name);
		}
		while (!localTypes.Contains(Qualify(scope, name)))
		{
			if (scope.Length == 0)
			{
				return false;
			}
			var separator = scope.LastIndexOf('.');
			scope = separator < 0 ? string.Empty : scope[..separator];
		}
		return true;
	}

	private static string Qualify(string typeNamespace, string name) => typeNamespace.Length == 0 ? name : typeNamespace + "." + name;

	private static string ExpandAlias(string name, CSharpSource source)
	{
		name = CSharpTypeName.Compact(name);
		var separator = name.IndexOfAny(['.', ':', '<', '(']);
		var prefix = separator < 0 ? name : name[..separator];
		var alias = Aliases().Matches(source.Text).FirstOrDefault(match => match.Groups[1].Value == prefix);
		return alias is null ? name : alias.Groups[2].Value + name[prefix.Length..].Replace("::", ".", StringComparison.Ordinal);
	}

	[GeneratedRegex(@"^// Htmxor upstream dependency: (src/[^\s|]+) \| (mirrors|reimplements|private-accesses)$")]
	private static partial Regex Provenance();
	[GeneratedRegex(@"\busing\s+([\w.]+)\s*;")]
	private static partial Regex Imports();
	[GeneratedRegex(@"\busing\s+(\w+)\s*=\s*([^;]+)\s*;")]
	private static partial Regex Aliases();
}
