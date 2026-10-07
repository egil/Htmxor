using System;
using System.Text.RegularExpressions;

namespace Htmxor.Generators;

// Reads an action binding's raw attribute value. A value that names one method group is a handler:
// "M", M and "@M" (and the same inside @(...)). Anything else is classified by why it is not one.
internal static class RazorBindingValue
{
	private static readonly Regex Identifier = new(
		"^[A-Za-z_][A-Za-z0-9_]*$",
		RegexOptions.CultureInvariant);

	private static readonly Regex MethodCall = new(
		"^[A-Za-z_][A-Za-z0-9_.]*\\s*\\(.*\\)$",
		RegexOptions.CultureInvariant | RegexOptions.Singleline);

	public static string? TryReadHandler(string? value)
	{
		var expression = Unwrap(Unquote(value));
		return Identifier.IsMatch(expression) ? expression : null;
	}

	public static string GetUnsupportedCause(string? value)
	{
		var expression = Unwrap(Unquote(value));
		if (expression.IndexOf("=>", StringComparison.Ordinal) >= 0 ||
			expression.StartsWith("delegate", StringComparison.Ordinal))
		{
			return "a lambda or closure";
		}

		return MethodCall.IsMatch(expression) ? "a method call" : "a computed expression";
	}

	private static string Unquote(string? value)
	{
		var trimmed = (value ?? string.Empty).Trim();
		return trimmed.Length >= 2 && trimmed[0] is '"' or '\'' && trimmed[trimmed.Length - 1] == trimmed[0]
			? trimmed.Substring(1, trimmed.Length - 2).Trim()
			: trimmed;
	}

	private static string Unwrap(string value)
	{
		if (value.StartsWith("@(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal))
		{
			return value.Substring(2, value.Length - 3).Trim();
		}

		return value.StartsWith("@", StringComparison.Ordinal) ? value.Substring(1) : value;
	}
}
