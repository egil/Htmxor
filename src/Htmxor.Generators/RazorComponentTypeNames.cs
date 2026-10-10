using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

// Generators cannot see Razor's generated types, so a component's type name is derived the way Razor
// composes it: an in-file @namespace verbatim, else the nearest ancestor _Imports.razor @namespace plus
// the folders below it, else RootNamespace plus the folders below the project directory. The derived
// name is a guess; the final-compilation analyzers confirm it against Razor's generated declaration
// for the same file and fail closed on a mismatch.
internal sealed class RazorComponentTypeNames : IEquatable<RazorComponentTypeNames>
{
	private const string ProjectDirectoryOption = "build_property.MSBuildProjectDirectory";
	private const string RootNamespaceOption = "build_property.RootNamespace";
	private const string NamespaceDirective = "@namespace";

	private static readonly char[] DirectorySeparators = { '/', '\\' };

	private static readonly StringComparison PathComparison = Path.DirectorySeparatorChar == '\\'
		? StringComparison.OrdinalIgnoreCase
		: StringComparison.Ordinal;

	private readonly string projectDirectory;
	private readonly string rootNamespace;
	private readonly ImmutableArray<RazorImportsNamespace> importsNamespaces;

	private RazorComponentTypeNames(
		string projectDirectory,
		string rootNamespace,
		ImmutableArray<RazorImportsNamespace> importsNamespaces)
	{
		this.projectDirectory = projectDirectory;
		this.rootNamespace = rootNamespace;
		this.importsNamespaces = importsNamespaces;
	}

	public static IncrementalValueProvider<RazorComponentTypeNames?> CreateProvider(
		IncrementalGeneratorInitializationContext context)
		=> context.AdditionalTextsProvider
			.Select(static (file, cancellationToken) => GetImportsNamespace(file, cancellationToken))
			.Where(static importsNamespace => importsNamespace is not null)
			.Select(static (importsNamespace, _) => importsNamespace!)
			.Collect()
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (input, _) => Create(input.Right, input.Left));

	public static RazorComponentTypeNames? Create(
		AnalyzerConfigOptionsProvider optionsProvider,
		ImmutableArray<AdditionalText> additionalFiles,
		CancellationToken cancellationToken)
		=> Create(
			optionsProvider,
			additionalFiles
				.Select(file => GetImportsNamespace(file, cancellationToken))
				.Where(static importsNamespace => importsNamespace is not null)
				.Select(static importsNamespace => importsNamespace!)
				.ToImmutableArray());

	private static RazorComponentTypeNames? Create(
		AnalyzerConfigOptionsProvider optionsProvider,
		ImmutableArray<RazorImportsNamespace> importsNamespaces)
	{
		var options = optionsProvider.GlobalOptions;
		return options.TryGetValue(ProjectDirectoryOption, out var projectDirectory) &&
			options.TryGetValue(RootNamespaceOption, out var rootNamespace) &&
			!string.IsNullOrWhiteSpace(projectDirectory)
			? new RazorComponentTypeNames(
				NormalizeDirectory(projectDirectory),
				rootNamespace,
				importsNamespaces
					.OrderBy(static importsNamespace => importsNamespace.Directory, StringComparer.Ordinal)
					.ToImmutableArray())
			: null;
	}

	public ImmutableArray<string> GetTypeNames(
		ImmutableArray<AdditionalText> additionalFiles,
		CancellationToken cancellationToken)
		=> additionalFiles
			.Select(file => GetTypeName(file, cancellationToken))
			.Where(static typeName => typeName is not null)
			.Select(static typeName => typeName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static typeName => typeName, StringComparer.Ordinal)
			.ToImmutableArray();

	public string? GetTypeName(AdditionalText additionalFile, CancellationToken cancellationToken)
	{
		var directory = GetComponentDirectory(additionalFile.Path);
		if (directory is null)
		{
			return null;
		}

		var componentName = SanitizeIdentifier(Path.GetFileNameWithoutExtension(additionalFile.Path));
		var namespaceName = FindNamespaceDirective(additionalFile.GetText(cancellationToken)) ??
			GetFolderNamespace(directory);
		return string.IsNullOrEmpty(namespaceName)
			? componentName
			: namespaceName + "." + componentName;
	}

	// Razor writes each component's generated file under a hint path mirroring the file's path relative
	// to the project directory, with every character other than a letter, digit, or folder separator
	// replaced by '_' (Components/Admin/Reports/X.razor becomes Components/Admin/Reports/X_razor.g.cs).
	public string? GetGeneratedPath(string razorPath)
	{
		if (GetComponentDirectory(razorPath) is null)
		{
			return null;
		}

		var relativePath = NormalizePath(razorPath).Substring(projectDirectory.Length);
		var segments = relativePath
			.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries)
			.Select(static segment => new string(segment
				.Select(static character => char.IsLetterOrDigit(character) ? character : '_')
				.ToArray()));
		return string.Join("/", segments) + ".g.cs";
	}

	public static bool GeneratedPathsEqual(string? left, string? right)
		=> left is not null && right is not null && string.Equals(left, right, PathComparison);

	private string? GetComponentDirectory(string path)
	{
		if (!path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || RazorImportsFile.Matches(path))
		{
			return null;
		}

		var directory = Path.GetDirectoryName(NormalizePath(path));
		return directory is not null && IsWithin(directory, projectDirectory) ? directory : null;
	}

	private string GetFolderNamespace(string directory)
	{
		for (var current = directory; IsWithin(current, projectDirectory); current = Path.GetDirectoryName(current)!)
		{
			var importsNamespace = importsNamespaces.FirstOrDefault(candidate =>
				string.Equals(candidate.Directory, current, PathComparison));
			if (importsNamespace is not null)
			{
				return ComposeNamespace(importsNamespace.Namespace, current, directory);
			}

			if (string.Equals(current, projectDirectory, PathComparison))
			{
				break;
			}
		}

		return ComposeNamespace(rootNamespace, projectDirectory, directory);
	}

	private static string ComposeNamespace(string baseNamespace, string baseDirectory, string directory)
		=> string.Join(
			".",
			baseNamespace.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
				.Concat(directory.Substring(baseDirectory.Length)
					.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries))
				.Select(SanitizeIdentifier));

	private static RazorImportsNamespace? GetImportsNamespace(
		AdditionalText additionalFile,
		CancellationToken cancellationToken)
	{
		if (!RazorImportsFile.Matches(additionalFile.Path))
		{
			return null;
		}

		var namespaceName = FindNamespaceDirective(additionalFile.GetText(cancellationToken));
		var directory = Path.GetDirectoryName(NormalizePath(additionalFile.Path));
		return namespaceName is null || directory is null
			? null
			: new RazorImportsNamespace(directory, namespaceName);
	}

	// Reads only the @namespace directive, which Razor accepts on any line of the file.
	private static string? FindNamespaceDirective(SourceText? text)
		=> text?.Lines
			.Select(static line => ParseNamespaceDirective(line.ToString()))
			.FirstOrDefault(static namespaceName => namespaceName is not null);

	private static string? ParseNamespaceDirective(string line)
	{
		var directive = line.TrimStart();
		if (!directive.StartsWith(NamespaceDirective, StringComparison.Ordinal) ||
			directive.Length == NamespaceDirective.Length ||
			!char.IsWhiteSpace(directive[NamespaceDirective.Length]))
		{
			return null;
		}

		var namespaceName = new string(directive
			.Substring(NamespaceDirective.Length)
			.TrimStart()
			.TakeWhile(static character => character == '.' || IsIdentifierPart(character))
			.ToArray());
		return namespaceName.Length == 0 ? null : namespaceName;
	}

	// Razor's identifier sanitizing: a leading digit gains a '_' prefix and every other character
	// that cannot appear in an identifier becomes '_'.
	private static string SanitizeIdentifier(string value)
	{
		var builder = new StringBuilder(value.Length + 1);
		if (value.Length > 0 && char.IsDigit(value[0]))
		{
			builder.Append('_');
		}

		foreach (var character in value)
		{
			builder.Append(IsIdentifierPart(character) ? character : '_');
		}

		return builder.ToString();
	}

	private static bool IsIdentifierPart(char character)
		=> character == '_' || char.IsLetterOrDigit(character);

	private static bool IsWithin(string directory, string root)
		=> string.Equals(directory, root, PathComparison) ||
			directory.StartsWith(root + Path.DirectorySeparatorChar, PathComparison);

	private static string NormalizePath(string path) => Path.GetFullPath(path);

	private static string NormalizeDirectory(string directory)
	{
		var fullPath = Path.GetFullPath(directory);
		var trimmed = fullPath.TrimEnd(DirectorySeparators);
		return trimmed.Length == 0 ? fullPath : trimmed;
	}

	public bool Equals(RazorComponentTypeNames? other)
		=> other is not null &&
			string.Equals(projectDirectory, other.projectDirectory, StringComparison.Ordinal) &&
			string.Equals(rootNamespace, other.rootNamespace, StringComparison.Ordinal) &&
			importsNamespaces.SequenceEqual(other.importsNamespaces);

	public override bool Equals(object? obj) => obj is RazorComponentTypeNames other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = StringComparer.Ordinal.GetHashCode(projectDirectory);
			return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(rootNamespace);
		}
	}

	private sealed class RazorImportsNamespace : IEquatable<RazorImportsNamespace>
	{
		public RazorImportsNamespace(string directory, string @namespace)
		{
			Directory = directory;
			Namespace = @namespace;
		}

		public string Directory { get; }

		public string Namespace { get; }

		public bool Equals(RazorImportsNamespace? other)
			=> other is not null &&
				string.Equals(Directory, other.Directory, StringComparison.Ordinal) &&
				string.Equals(Namespace, other.Namespace, StringComparison.Ordinal);

		public override bool Equals(object? obj) => obj is RazorImportsNamespace other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				return (StringComparer.Ordinal.GetHashCode(Directory) * 397) ^
					StringComparer.Ordinal.GetHashCode(Namespace);
			}
		}
	}
}
