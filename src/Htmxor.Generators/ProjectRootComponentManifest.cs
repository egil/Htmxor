using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

internal static class ProjectRootComponentManifest
{
	private const string ProjectDirectoryOption = "build_property.MSBuildProjectDirectory";
	private const string RootNamespaceOption = "build_property.RootNamespace";

	public static ImmutableArray<string> GetTypeNames(
		ImmutableArray<AdditionalText> additionalFiles,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		if (!TryGetProject(optionsProvider, out var projectDirectory, out var rootNamespace))
		{
			return ImmutableArray<string>.Empty;
		}

		return additionalFiles
			.Select(file => GetTypeName(file.Path, projectDirectory, rootNamespace))
			.Where(static typeName => typeName is not null)
			.Select(static typeName => typeName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static typeName => typeName, StringComparer.Ordinal)
			.ToImmutableArray();
	}

	public static string? GetTypeName(
		AdditionalText additionalFile,
		AnalyzerConfigOptionsProvider optionsProvider)
	{
		if (additionalFile is null)
		{
			throw new ArgumentNullException(nameof(additionalFile));
		}

		if (optionsProvider is null)
		{
			throw new ArgumentNullException(nameof(optionsProvider));
		}

		return TryGetProject(optionsProvider, out var projectDirectory, out var rootNamespace)
			? GetTypeName(additionalFile.Path, projectDirectory, rootNamespace)
			: null;
	}

	private static string? GetTypeName(
		string path,
		string projectDirectory,
		string rootNamespace)
	{
		if (!path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
			!PathsEqual(Path.GetDirectoryName(path), projectDirectory))
		{
			return null;
		}

		var componentName = Path.GetFileNameWithoutExtension(path);
		if (string.Equals(componentName, "_Imports", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return string.IsNullOrEmpty(rootNamespace)
			? componentName
			: rootNamespace + "." + componentName;
	}

	private static bool PathsEqual(string? left, string right)
		=> left is not null && string.Equals(
			Path.GetFullPath(left),
			Path.GetFullPath(right),
			Path.DirectorySeparatorChar == '\\'
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal);

	private static bool TryGetProject(
		AnalyzerConfigOptionsProvider optionsProvider,
		out string projectDirectory,
		out string rootNamespace)
	{
		var options = optionsProvider.GlobalOptions;
		var hasProjectDirectory = options.TryGetValue(ProjectDirectoryOption, out projectDirectory!);
		var hasRootNamespace = options.TryGetValue(RootNamespaceOption, out rootNamespace!);

		return hasProjectDirectory && hasRootNamespace;
	}
}
