using System.Text.RegularExpressions;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class CSharpImports
{
	private readonly IReadOnlyList<ImportScope> scopes;
	private readonly IReadOnlyList<SourceUsing> directives;

	public CSharpImports(string text)
	{
		scopes = NamespaceDeclaration().Matches(text).Select(match => new ImportScope(
			CSharpTypeName.Compact(match.Groups[1].Value), match.Index, match.Value.EndsWith(';') ? text.Length : CSharpSource.ClosingBrace(text, match.Index + match.Length - 1))).ToArray();
		directives = UsingDirective().Matches(text).Select(match => new SourceUsing(
			CSharpTypeName.Compact(match.Groups["alias"].Value), CSharpTypeName.Compact(match.Groups["target"].Value),
			match.Groups["global"].Success, ScopeAt(match.Index))).ToArray();
	}

	public IEnumerable<SourceUsing> Global => directives.Where(directive => directive.IsGlobal);

	public string NamespaceAt(int position) => string.Join('.', scopes.Where(scope => scope.Contains(position)).Select(scope => scope.Name));

	public IEnumerable<SourceImportScope> At(int position, IReadOnlyList<SourceUsing> global)
	{
		foreach (var scope in scopes.Where(scope => scope.Contains(position)).OrderByDescending(scope => scope.Start))
		{
			yield return new(NamespaceAt(scope.Start) is { Length: > 0 } parent ? parent + "." + scope.Name : scope.Name,
				directives.Where(directive => !directive.IsGlobal && directive.ScopeStart == scope.Start).ToArray());
		}
		yield return new(string.Empty, directives.Where(directive => !directive.IsGlobal && directive.ScopeStart == -1).Concat(global).ToArray());
	}

	private int ScopeAt(int position) => scopes.Where(scope => scope.Contains(position))
		.Select(scope => scope.Start).DefaultIfEmpty(-1).Max();

	[GeneratedRegex(@"\bnamespace\s+([\w.\s]+?)\s*[;{]")]
	private static partial Regex NamespaceDeclaration();
	[GeneratedRegex(@"\b(?<global>global\s+)?using\s+(?:(?<alias>\w+)\s*=\s*)?(?<target>[\w.:<>?,\s]+?)\s*;")]
	private static partial Regex UsingDirective();

	private sealed record ImportScope(string Name, int Start, int End)
	{
		public bool Contains(int position) => Start < position && position < End;
	}
}

internal sealed record SourceUsing(string Alias, string Target, bool IsGlobal, int ScopeStart);

internal sealed record SourceImportScope(string Namespace, IReadOnlyList<SourceUsing> Directives);
