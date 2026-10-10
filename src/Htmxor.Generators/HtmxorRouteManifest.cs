using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

internal static class HtmxorRouteManifest
{
	private const string RazorGeneratorDirectory =
		"Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/";

	public static ImmutableArray<string> GetTypeNames(
		ImmutableArray<AdditionalText> razorComponents,
		ImmutableArray<CSharpRoutedComponent> csharpComponents,
		RazorComponentTypeNames? razorComponentTypeNames,
		CancellationToken cancellationToken)
	{
		var omittedCSharpComponents = csharpComponents
			.Where(static component =>
				!component.HasExplicitMethods &&
				!IsRazorGeneratedPath(component.Path))
			.Select(static component => component.TypeName)
			.ToImmutableHashSet(StringComparer.Ordinal);
		var razorTypeNames = razorComponentTypeNames?.GetTypeNames(razorComponents, cancellationToken) ??
			ImmutableArray<string>.Empty;

		return razorTypeNames
			.Where(typeName => !omittedCSharpComponents.Contains(typeName))
			.Concat(csharpComponents
				.Where(component =>
					component.HasExplicitMethods &&
					!omittedCSharpComponents.Contains(component.TypeName))
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
			attributeContext.TargetNode.SyntaxTree.FilePath,
			hasExplicitMethods);
	}

	// Any Razor-generated declaration of the type, wherever its .razor file sits.
	public static bool HasCompiledRazorDeclaration(INamedTypeSymbol type)
	{
		var generatedFileName = type.Name + "_razor.g.cs";
		return type.DeclaringSyntaxReferences.Any(reference =>
			IsRazorGeneratedPath(reference.SyntaxTree.FilePath) &&
			string.Equals(
				Path.GetFileName(reference.SyntaxTree.FilePath),
				generatedFileName,
				StringComparison.Ordinal));
	}

	// Confirms a derived type name: the type must have a Razor-generated declaration at the hint path
	// Razor writes for the claimed .razor file, so two same-named components in different folders each
	// match only their own file.
	public static bool HasCompiledRazorDeclaration(
		INamedTypeSymbol type,
		IEnumerable<string?> generatedPaths)
	{
		var declaredPaths = type.DeclaringSyntaxReferences
			.Select(static reference => GetRazorGeneratedHintPath(reference.SyntaxTree.FilePath))
			.Where(static path => path is not null)
			.ToImmutableArray();
		return generatedPaths.Any(generatedPath => declaredPaths.Any(declaredPath =>
			RazorComponentTypeNames.GeneratedPathsEqual(declaredPath, generatedPath)));
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
		=> GetRazorGeneratedHintPath(path) is not null;

	// This compiler-owned path is the ownership fence when a same-named Razor file compiles into
	// another namespace. Razor mirrors the component's folder below its generator directory, so the
	// remainder is the hint path. Revalidate the marker with each supported SDK.
	private static string? GetRazorGeneratedHintPath(string path)
	{
		var normalizedPath = path.Replace('\\', '/');
		var index = normalizedPath.LastIndexOf(
			RazorGeneratorDirectory,
			Path.DirectorySeparatorChar == '\\'
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal);
		if (index < 0 || (index > 0 && normalizedPath[index - 1] != '/'))
		{
			return null;
		}

		var hintPath = normalizedPath.Substring(index + RazorGeneratorDirectory.Length);
		return hintPath.Length == 0 ? null : hintPath;
	}
}

internal sealed class CSharpRoutedComponent : IEquatable<CSharpRoutedComponent>
{
	public CSharpRoutedComponent(
		string typeName,
		string path,
		bool hasExplicitMethods)
	{
		TypeName = typeName;
		Path = path;
		HasExplicitMethods = hasExplicitMethods;
	}

	public string TypeName { get; }

	public string Path { get; }

	public bool HasExplicitMethods { get; }

	public bool Equals(CSharpRoutedComponent? other)
		=> other is not null &&
			string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
			string.Equals(Path, other.Path, StringComparison.Ordinal) &&
			HasExplicitMethods == other.HasExplicitMethods;

	public override bool Equals(object? obj)
		=> obj is CSharpRoutedComponent other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = StringComparer.Ordinal.GetHashCode(TypeName);
			hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
			return (hash * 397) ^ HasExplicitMethods.GetHashCode();
		}
	}
}
