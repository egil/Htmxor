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

	private HtmxorFormManifestDeclaration(string typeName, string condition, string imports)
	{
		TypeName = typeName;
		Condition = condition;
		Imports = imports;
	}

	public string TypeName { get; }

	private string Condition { get; }

	private string Imports { get; }

	public static HtmxorFormManifestDeclaration? Read(AdditionalText file, RazorComponentTypeNames? typeNames,
		ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
	{
		var name = typeNames?.GetTypeName(file, cancellationToken);
		var importNames = ReadImports(file, files, cancellationToken).ToArray();
		var forms = RazorFormDeclaration.Read(file, cancellationToken)
			.Where(form => form.IsNamed && (!form.IsEditForm || IsStockEditFormName(form.TagName, importNames))).ToArray();
		if (name is null || forms.Length == 0)
		{
			return null;
		}

		var condition = forms.Any(static form => !form.IsEditForm) ? "true" : string.Join(" || ", forms
			.Select(static form => "typeof(" + form.TagName + ") == typeof(global::Microsoft.AspNetCore.Components.Forms.EditForm)")
			.Distinct(StringComparer.Ordinal));
		var imports = string.Concat(importNames
			.Prepend("Microsoft.AspNetCore.Components.Forms").Distinct(StringComparer.Ordinal)
			.Select(static import => "using " + import.TrimEnd(';') + ";\n"));
		return new HtmxorFormManifestDeclaration(name, condition, imports);
	}

	private static bool IsStockEditFormName(string tagName, string[] imports)
		=> tagName is "EditForm" or "Microsoft.AspNetCore.Components.Forms.EditForm" or "global::Microsoft.AspNetCore.Components.Forms.EditForm" ||
			imports.Any(import => IsStockAlias(tagName, import));

	private static bool IsStockAlias(string tagName, string import)
	{
		var equals = import.IndexOf('=');
		return equals > 0 && import.Substring(0, equals).Trim() == tagName &&
			import.Substring(equals + 1).Trim().TrimEnd(';') is
				"Microsoft.AspNetCore.Components.Forms.EditForm" or "global::Microsoft.AspNetCore.Components.Forms.EditForm";
	}

	private static IEnumerable<string> ReadImports(AdditionalText file, ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
		=> files.Where(candidate => candidate.Path == file.Path ||
			(RazorImportsFile.Matches(candidate.Path) && IsAncestor(candidate.Path, file.Path)))
			.SelectMany(candidate => candidate.GetText(cancellationToken) is { } text
				? RazorMarkupScanner.Scan(text.ToString(), NoAttributes).UsingDirectives : Array.Empty<string>())
			.Select(NormalizeImport).OfType<string>();

	private static string? NormalizeImport(string import)
	{
		var directive = SyntaxFactory.ParseCompilationUnit("using " + import + "\n;").Usings.FirstOrDefault();
		if (directive is null || directive.ContainsDiagnostics)
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

	public string Render()
	{
		var separator = TypeName.LastIndexOf('.');
		var name = separator < 0 ? TypeName : TypeName.Substring(separator + 1);
		var body = Imports + "partial class " + name + "\n{\n" +
			"\tinternal static bool __HtmxorHasOwnNamedForms => " + Condition + ";\n}\n";
		return separator < 0 ? body : "namespace " + TypeName.Substring(0, separator) + "\n{\n" + body + "}\n";
	}
}
