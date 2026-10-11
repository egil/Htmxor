using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class RazorFormDeclaration
{
	private static readonly ISet<string> FormAttributes = new HashSet<string>(StringComparer.Ordinal)
	{
		"FormName", "@formname", "method",
	};

	private RazorFormDeclaration(MarkupElement element, Location location)
	{
		TagName = element.Name;
		IsEditForm = !IsPlainFormName(element.Name);
		IsNamed = element.Attributes.Any(attribute => attribute.Name == (IsEditForm ? "FormName" : "@formname"));
		IsPost = IsEditForm || IsNamed || element.Attributes.Any(static attribute =>
			attribute.Name == "method" && string.Equals(Unquote(attribute.Value), "post", StringComparison.OrdinalIgnoreCase));
		Location = location;
	}

	public string TagName { get; }

	public bool IsEditForm { get; }

	public bool IsNamed { get; }

	public bool IsPost { get; }

	public Location Location { get; }

	public static IReadOnlyList<RazorFormDeclaration> Read(AdditionalText file, CancellationToken cancellationToken)
	{
		var text = file.GetText(cancellationToken);
		return text is null ? Array.Empty<RazorFormDeclaration>() : Read(text, file.Path);
	}

	public static IReadOnlyList<RazorFormDeclaration> Read(SourceText text, string path)
		=> RazorMarkupScanner.Scan(text.ToString(), FormAttributes).Elements
			.Where(static element => element.InOwnerMarkup && (IsPlainFormName(element.Name) || IsEditFormName(element.Name) ||
				(IsComponentName(element.Name) && element.Attributes.Any(static attribute => attribute.Name == "FormName"))))
			.Select(element => Create(element, text, path))
			.ToArray();

	private static RazorFormDeclaration Create(MarkupElement element, SourceText text, string path)
	{
		var span = new TextSpan(element.Index, element.Name.Length + 1);
		return new RazorFormDeclaration(element, Location.Create(path, span, text.Lines.GetLinePositionSpan(span)));
	}

	private static bool IsPlainFormName(string name) => string.Equals(name, "form", StringComparison.OrdinalIgnoreCase);

	private static bool IsComponentName(string name)
		=> name.Length > 0 && (char.IsUpper(name[0]) || name.IndexOf('.') >= 0 || name.StartsWith("global::", StringComparison.Ordinal));

	private static bool IsEditFormName(string name)
		=> name is "EditForm" or "Microsoft.AspNetCore.Components.Forms.EditForm" or "global::Microsoft.AspNetCore.Components.Forms.EditForm";

	private static string? Unquote(string? value) => value?.Trim('"', '\'');
}
