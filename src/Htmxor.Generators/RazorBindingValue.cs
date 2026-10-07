using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Htmxor.Generators;

// Reads an action binding's raw attribute value as the C# expression Razor compiles. A bare method-group
// name (M or this.M, quoted or not, behind the Razor @ and any parentheses) is a handler; anything else is
// classified by why it is not one.
internal static class RazorBindingValue
{
	public static string? TryReadHandler(string? value)
		=> Parse(value) switch
		{
			IdentifierNameSyntax name => name.Identifier.ValueText,
			MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name }
				=> name.Identifier.ValueText,
			_ => null,
		};

	public static string GetUnsupportedCause(string? value)
		=> Parse(value) switch
		{
			LambdaExpressionSyntax or AnonymousMethodExpressionSyntax => "a lambda or closure",
			InvocationExpressionSyntax => "a method call",
			_ => "a computed expression",
		};

	private static ExpressionSyntax? Parse(string? value)
	{
		var expression = StripRazorTransition(Unquote(value));
		if (expression.Length == 0)
		{
			return null;
		}

		var syntax = SyntaxFactory.ParseExpression(expression);
		if (syntax.ContainsDiagnostics || syntax.FullSpan.Length != expression.Length)
		{
			return null;
		}

		while (syntax is ParenthesizedExpressionSyntax parenthesized)
		{
			syntax = parenthesized.Expression;
		}

		return syntax;
	}

	private static string Unquote(string? value)
	{
		var trimmed = (value ?? string.Empty).Trim();
		return trimmed.Length >= 2 && trimmed[0] is '"' or '\'' && trimmed[trimmed.Length - 1] == trimmed[0]
			? trimmed.Substring(1, trimmed.Length - 2).Trim()
			: trimmed;
	}

	private static string StripRazorTransition(string value)
		=> value.StartsWith("@", StringComparison.Ordinal) ? value.Substring(1).Trim() : value;
}
