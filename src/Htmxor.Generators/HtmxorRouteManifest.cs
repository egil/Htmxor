using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

internal static class HtmxorRouteManifest
{
	public static ImmutableArray<string> GetTypeNames(
		ImmutableArray<AdditionalText> razorComponents,
		ImmutableArray<CSharpRoutedComponent> csharpComponents,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		var omittedCSharpComponents = csharpComponents
			.Where(static component =>
				!component.HasExplicitMethods &&
				!IsRazorGeneratedPath(component.Path))
			.Select(static component => component.TypeName)
			.ToImmutableHashSet(StringComparer.Ordinal);

		return ProjectRootComponentManifest.GetTypeNames(razorComponents, optionsProvider)
			.Where(typeName => !omittedCSharpComponents.Contains(typeName))
			.Concat(csharpComponents
				.Where(component =>
					component.HasExplicitMethods &&
					!omittedCSharpComponents.Contains(component.TypeName) &&
					IsProjectRoot(component, optionsProvider))
				.Select(static component => component.TypeName))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static typeName => typeName, StringComparer.Ordinal)
			.ToImmutableArray();
	}

	public static CSharpRoutedComponent? GetCSharpComponent(
		GeneratorAttributeSyntaxContext attributeContext)
	{
		if (attributeContext.TargetSymbol is not INamedTypeSymbol { ContainingType: null } type)
		{
			return null;
		}

		var namespaceName = type.ContainingNamespace.ToDisplayString();
		var typeName = string.IsNullOrEmpty(namespaceName)
			? type.MetadataName
			: namespaceName + "." + type.MetadataName;
		var hasExplicitMethods = attributeContext.Attributes.Length == 1 &&
			attributeContext.Attributes[0].NamedArguments.Any(static argument =>
				string.Equals(argument.Key, "Methods", StringComparison.Ordinal));

		return new CSharpRoutedComponent(
			typeName,
			namespaceName,
			attributeContext.TargetNode.SyntaxTree.FilePath,
			hasExplicitMethods);
	}

	public static bool IsProjectRoot(
		CSharpRoutedComponent component,
		AnalyzerConfigOptionsProvider optionsProvider)
		=> IsProjectRoot(
			component.Namespace,
			component.Path,
			optionsProvider);

	public static bool IsProjectRoot(
		string namespaceName,
		string path,
		AnalyzerConfigOptionsProvider optionsProvider)
		=> ProjectRootComponentManifest.TryGetProject(
				optionsProvider,
				out var projectDirectory,
				out var rootNamespace) &&
			ProjectRootComponentManifest.PathsEqual(
			Path.GetDirectoryName(path),
				projectDirectory) &&
			string.Equals(namespaceName, rootNamespace, StringComparison.Ordinal);

	public static bool HasCompiledRazorDeclaration(
		INamedTypeSymbol type,
		string? razorPath = null)
	{
		var componentName = razorPath is null
			? type.Name
			: Path.GetFileNameWithoutExtension(razorPath);
		var generatedFileName = componentName + "_razor.g.cs";
		return type.DeclaringSyntaxReferences.Any(reference =>
			IsRazorGeneratedPath(reference.SyntaxTree.FilePath) &&
			string.Equals(
				Path.GetFileName(reference.SyntaxTree.FilePath),
				generatedFileName,
				StringComparison.Ordinal));
	}

	public static bool IsMatchingRazorCodeBehind(
		INamedTypeSymbol type,
		string path)
		=> string.Equals(
			Path.GetFileName(path),
			type.Name + ".razor.cs",
			Path.DirectorySeparatorChar == '\\'
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal);

	public static bool IsRazorGeneratedPath(string path)
	{
		// This compiler-owned path is the ownership fence when a same-named Razor file
		// compiles into another namespace. Revalidate the marker with each supported SDK.
		var generatorDirectory = Path.Combine(
			"Microsoft.CodeAnalysis.Razor.Compiler",
			"Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator");
		var directory = Path.GetDirectoryName(path);
		return directory is not null && directory.EndsWith(
			generatorDirectory,
			Path.DirectorySeparatorChar == '\\'
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal);
	}
}

internal sealed class CSharpRoutedComponent : IEquatable<CSharpRoutedComponent>
{
	public CSharpRoutedComponent(
		string typeName,
		string @namespace,
		string path,
		bool hasExplicitMethods)
	{
		TypeName = typeName;
		Namespace = @namespace;
		Path = path;
		HasExplicitMethods = hasExplicitMethods;
	}

	public string TypeName { get; }

	public string Namespace { get; }

	public string Path { get; }

	public bool HasExplicitMethods { get; }

	public bool Equals(CSharpRoutedComponent? other)
		=> other is not null &&
			string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
			string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) &&
			string.Equals(Path, other.Path, StringComparison.Ordinal) &&
			HasExplicitMethods == other.HasExplicitMethods;

	public override bool Equals(object? obj)
		=> obj is CSharpRoutedComponent other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = StringComparer.Ordinal.GetHashCode(TypeName);
			hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Namespace);
			hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
			return (hash * 397) ^ HasExplicitMethods.GetHashCode();
		}
	}
}
