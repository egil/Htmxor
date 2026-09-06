using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Htmxor.UpstreamMonitor;

internal static partial class LocalFrameworkDependencyDiscovery
{
	private static readonly Lazy<PortableExecutableReference[]> frameworkReferences = new(ReadFrameworkReferences);
	private static readonly CSharpParseOptions parseOptions = new(LanguageVersion.Preview);

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

	public static IEnumerable<LocalFrameworkDependency> Discover(string repositoryRoot)
	{
		var root = Path.Combine(repositoryRoot, "src", "Htmxor");
		if (!Directory.Exists(root))
		{
			return [];
		}
		var sources = ReadLocalSources(repositoryRoot, root);
		// This compilation binds local declarations only. It is never emitted; fetched upstream source stays in the separate text comparer.
		var compilation = CSharpCompilation.Create("Htmxor.LocalDependencies", sources, frameworkReferences.Value,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		return sources.SelectMany(source => ReadProvenance(source).Concat(ReadBases(source, compilation.GetSemanticModel(source))));
	}

	private static SyntaxTree[] ReadLocalSources(string repositoryRoot, string root) =>
		Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
			.Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
			.Order(StringComparer.Ordinal)
			.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions,
				Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))).ToArray();

	private static PortableExecutableReference[] ReadFrameworkReferences()
	{
		// Assembly paths come only from the installed platform and ASP.NET framework, never local source, manifests or provider responses.
		var directories = new[]
		{
			Path.GetDirectoryName(typeof(object).Assembly.Location)!,
			Path.GetDirectoryName(typeof(ComponentBase).Assembly.Location)!,
		};
		return directories.Distinct(StringComparer.Ordinal).SelectMany(directory => Directory.EnumerateFiles(directory, "*.dll"))
			.Order(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
	}

	private static IEnumerable<LocalFrameworkDependency> ReadBases(SyntaxTree source, SemanticModel model)
	{
		var bases = source.GetRoot().DescendantNodes().OfType<BaseListSyntax>().SelectMany(list => list.Types);
		foreach (var baseType in bases)
		{
			if (model.GetTypeInfo(baseType.Type).Type is INamedTypeSymbol symbol && IsFrameworkType(symbol))
			{
				var identity = MetadataIdentity(symbol.OriginalDefinition);
				var upstreamPath = frameworkSources.GetValueOrDefault(identity, "unresolved:" + identity);
				var relationship = symbol.TypeKind == TypeKind.Interface ? WatchRelationship.Implements : WatchRelationship.Subclasses;
				yield return new(source.FilePath, upstreamPath, relationship);
			}
		}
	}

	private static bool IsFrameworkType(INamedTypeSymbol symbol)
	{
		if (symbol.TypeKind == TypeKind.Error || !symbol.Locations.Any(location => location.IsInMetadata))
		{
			return false;
		}
		var typeNamespace = symbol.ContainingNamespace.ToDisplayString();
		return typeNamespace == "Microsoft.AspNetCore" || typeNamespace.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal);
	}

	private static string MetadataIdentity(INamedTypeSymbol symbol) => symbol.ContainingType is { } parent
		? MetadataIdentity(parent) + "+" + symbol.MetadataName
		: symbol.ContainingNamespace.ToDisplayString() + "." + symbol.MetadataName;

	private static IEnumerable<LocalFrameworkDependency> ReadProvenance(SyntaxTree source)
	{
		foreach (var comment in source.GetRoot().DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)))
		{
			var marker = Provenance().Match(comment.ToString());
			if (marker.Success)
			{
				yield return new(source.FilePath, marker.Groups[1].Value, WatchManifestFile.ParseRelationship(marker.Groups[2].Value));
			}
		}
	}

	[GeneratedRegex(@"^// Htmxor upstream dependency: (src/[^\s|]+) \| (mirrors|reimplements|private-accesses)$")]
	private static partial Regex Provenance();
}
