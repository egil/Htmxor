using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Htmxor.Generators;

internal sealed class HtmxorFormManifestDeclaration
{
	private static readonly ISet<string> NoAttributes = new HashSet<string>(StringComparer.Ordinal);

	private HtmxorFormManifestDeclaration(string typeName)
	{
		TypeName = typeName;
	}

	public string TypeName { get; }

	public static HtmxorFormManifestDeclaration? Read(AdditionalText file, RazorComponentTypeNames? typeNames,
		ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
	{
		var name = typeNames?.GetTypeName(file, cancellationToken);
		if (name is null || !ReadStockForms(file, name, files, cancellationToken).Any(static form => form.IsNamed))
		{
			return null;
		}

		return new HtmxorFormManifestDeclaration(name);
	}

	public static IReadOnlyList<RazorFormDeclaration> ReadStockForms(AdditionalText file, string? ownerName,
		ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
	{
		var importNames = ReadImports(file, files, cancellationToken).ToArray();
		return RazorFormDeclaration.Read(file, cancellationToken)
			.Where(form => !form.IsEditForm || IsStockEditFormTag(form.TagName, ownerName, importNames)).ToArray();
	}

	// Razor tags use namespace scope; a C# lookup within the owner can select an alias or nested model instead.
	private static bool IsStockEditFormTag(string tagName, string? ownerName, string[] imports)
		=> tagName == "Microsoft.AspNetCore.Components.Forms.EditForm" ||
			(tagName == "EditForm" && (imports.Any(static import => import is
				"Microsoft.AspNetCore.Components.Forms" or "global::Microsoft.AspNetCore.Components.Forms") ||
				ownerName?.Substring(0, ownerName.LastIndexOf('.') + 1) == "Microsoft.AspNetCore.Components.Forms."));

	private static IEnumerable<string> ReadImports(AdditionalText file, ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
		=> files.Where(candidate => candidate.Path == file.Path ||
			(RazorImportsFile.Matches(candidate.Path) && IsAncestor(candidate.Path, file.Path)))
			.SelectMany(candidate => candidate.GetText(cancellationToken) is { } text
				? RazorMarkupScanner.Scan(text.ToString(), NoAttributes).UsingDirectives : Array.Empty<string>())
			.Select(NormalizeImport).OfType<string>();

	private static string? NormalizeImport(string import)
	{
		var directive = SyntaxFactory.ParseCompilationUnit("using " + import + "\n;").Usings.FirstOrDefault();
		if (directive is null || directive.ContainsDiagnostics || directive.Alias is not null ||
			directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
		{
			return null;
		}

		var normalized = directive.ReplaceTrivia(directive.DescendantTrivia(), static (_, _) => default)
			.NormalizeWhitespace().ToFullString();
		return normalized.Substring("using ".Length).TrimEnd(';');
	}

	private static bool IsAncestor(string importsPath, string componentPath)
	{
		var directory = Path.GetDirectoryName(Path.GetFullPath(importsPath))!;
		return Path.GetFullPath(componentPath).StartsWith(directory + Path.DirectorySeparatorChar,
			Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}
}
