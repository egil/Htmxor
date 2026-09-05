using System.Text.RegularExpressions;

namespace Htmxor.UpstreamMonitor;

internal static partial class ApiSurfaceComparer
{
	public static IReadOnlyList<ApiChange> Compare(string baseline, string target, string watchedType)
	{
		var before = FindType(baseline, watchedType);
		var after = FindType(target, watchedType);
		if (before is null)
		{
			return after is null ? [] : [Change(after, ChangeKind.Added, ApiSymbolKind.Type, after.Signature)];
		}
		if (after is null)
		{
			return [Change(before, ChangeKind.Removed, ApiSymbolKind.Type, before.Signature)];
		}
		var oldSymbols = Symbols(before).ToHashSet();
		var newSymbols = Symbols(after).ToHashSet();
		return newSymbols.Except(oldSymbols).Select(symbol => Change(after, ChangeKind.Added, symbol.Kind, symbol.Signature))
			.Concat(oldSymbols.Except(newSymbols).Select(symbol => Change(before, ChangeKind.Removed, symbol.Kind, symbol.Signature))).ToArray();
	}

	private static SourceType? FindType(string source, string name) => new CSharpSource(source).Types
		.FirstOrDefault(type => type.Name.Split('<')[0].Trim() == name);

	private static ApiChange Change(SourceType type, ChangeKind kind, ApiSymbolKind symbolKind, string signature) =>
		new(type.Name, kind, symbolKind, signature, kind == ChangeKind.Added
			? ReviewClassification.ExtensibilityOpportunity : ReviewClassification.CompatibilityRisk);

	private static IEnumerable<ApiSymbol> Symbols(SourceType type)
	{
		yield return new(ApiSymbolKind.Type, type.Signature);
		if (type.PrimaryConstructorParameters is not null)
		{
			var accessibility = type.Signature.Split(' ').Contains("abstract", StringComparer.Ordinal) ? "protected" : "public";
			yield return new(ApiSymbolKind.Constructor, $"{accessibility} {type.Name.Split('<')[0]}{type.PrimaryConstructorParameters}");
		}
		foreach (var baseType in type.Bases)
		{
			yield return new(ApiSymbolKind.BaseType, baseType);
		}
		foreach (var constraint in type.Constraints)
		{
			yield return new(ApiSymbolKind.Constraint, constraint);
		}
		foreach (var declaration in MemberDeclarations(type.Body, type.DeclarationBody))
		{
			var signature = CSharpSource.NormalizeDeclaration(Attributes().Replace(declaration, string.Empty));
			if (Visible(signature, type.IsInterface))
			{
				var simpleName = type.Name.Split('<')[0];
				var isConstructor = signature.Split('(')[0].Split(' ').Last() == simpleName;
				yield return new(isConstructor ? ApiSymbolKind.Constructor : ApiSymbolKind.Member, signature);
			}
		}
	}

	private static bool Visible(string signature, bool isInterface) =>
		signature.Length > 0 && (PublicOrProtected().IsMatch(signature) || (isInterface && !NonPublic().IsMatch(signature)));

	private static IEnumerable<string> MemberDeclarations(string body, string declarationBody)
	{
		var start = 0;
		for (var index = 0; index < body.Length; index++)
		{
			if (body[index] is not (';' or '{'))
			{
				continue;
			}
			var expression = body.IndexOf("=>", start, index - start, StringComparison.Ordinal);
			var declaration = declarationBody[start..(expression < 0 ? index : expression)];
			declaration = WithAccessors(declaration, body, declarationBody, index, expression >= 0);
			yield return declaration;
			if (body[index] == '{')
			{
				index = CSharpSource.ClosingBrace(body, index);
			}
			start = index + 1;
		}
	}

	private static string WithAccessors(string declaration, string body, string declarationBody, int boundary, bool expression)
	{
		var header = Attributes().Replace(declaration, string.Empty);
		if (header.Contains('(', StringComparison.Ordinal))
		{
			return declaration;
		}
		if (expression)
		{
			return declaration + " { get; }";
		}
		if (body[boundary] != '{')
		{
			return declaration;
		}
		var closing = CSharpSource.ClosingBrace(body, boundary);
		var accessors = MemberDeclarations(body[(boundary + 1)..closing], declarationBody[(boundary + 1)..closing])
			.Select(CSharpSource.Normalize).Where(accessor => Accessor().IsMatch(accessor)).ToArray();
		return accessors.Length == 0 ? declaration : declaration + " { " + string.Join("; ", accessors) + "; }";
	}

	[GeneratedRegex(@"^(?:(?:public|private|protected|internal)\s+)*(?:get|set|init|add|remove)$")]
	private static partial Regex Accessor();

	[GeneratedRegex(@"^\s*(?:\[[^\]]*\]\s*)+")]
	private static partial Regex Attributes();
	[GeneratedRegex(@"\b(public|protected)\b")]
	private static partial Regex PublicOrProtected();
	[GeneratedRegex(@"\b(private|internal)\b")]
	private static partial Regex NonPublic();
	private sealed record ApiSymbol(ApiSymbolKind Kind, string Signature);
}
