using System;
using System.Collections.Generic;
using System.Linq;

namespace Htmxor;

internal static class HtmxorRouteTemplateContract
{
	public static bool IsSupported(string template)
	{
		if (string.IsNullOrWhiteSpace(template) || template[0] != '/')
		{
			return false;
		}

		var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var hasConstrainedParameter = false;
		var segmentStart = 1;
		for (var index = 1; index <= template.Length; index++)
		{
			if (index < template.Length && template[index] != '/')
			{
				continue;
			}

			var segmentLength = index - segmentStart;
			if (segmentLength == 0 || !IsSupportedSegment(
				template,
				segmentStart,
				segmentLength,
				parameterNames,
				ref hasConstrainedParameter))
			{
				return false;
			}

			segmentStart = index + 1;
		}

		return hasConstrainedParameter;
	}

	private static bool IsSupportedSegment(
		string template,
		int start,
		int length,
		HashSet<string> parameterNames,
		ref bool hasConstrainedParameter)
	{
		var openingBrace = template.IndexOf('{', start, length);
		var closingBrace = template.IndexOf('}', start, length);
		if (openingBrace < 0 && closingBrace < 0)
		{
			return IsSupportedLiteral(template, start, length);
		}

		if (!HasSingleOuterBraces(template, start, length, openingBrace, closingBrace))
		{
			return false;
		}

		var parameter = template.Substring(start + 1, length - 2);
		if (!TrySplitParameter(parameter, out var name, out var constraint))
		{
			return false;
		}

		if (!IsIdentifier(name) ||
			!IsSupportedConstraint(constraint) ||
			!parameterNames.Add(name))
		{
			return false;
		}

		hasConstrainedParameter = true;
		return true;
	}

	private static bool HasSingleOuterBraces(
		string template,
		int start,
		int length,
		int openingBrace,
		int closingBrace)
		=> openingBrace == start &&
			closingBrace == start + length - 1 &&
			template.IndexOf('{', openingBrace + 1, length - 1) < 0 &&
			template.IndexOf('}', start, length - 1) < 0;

	private static bool TrySplitParameter(
		string parameter,
		out string name,
		out string constraint)
	{
		name = string.Empty;
		constraint = string.Empty;
		var constraintSeparator = FindCharacter(parameter, ':', 0);
		if (constraintSeparator <= 0 || constraintSeparator == parameter.Length - 1)
		{
			return false;
		}

		if (FindCharacter(parameter, ':', constraintSeparator + 1) >= 0 ||
			parameter.IndexOfAny(['*', '?', '=']) >= 0)
		{
			return false;
		}

		name = parameter.Substring(0, constraintSeparator);
		constraint = parameter.Substring(constraintSeparator + 1);
		return true;
	}

	private static int FindCharacter(string value, char character, int start)
	{
		for (var index = start; index < value.Length; index++)
		{
			if (value[index] == character)
			{
				return index;
			}
		}

		return -1;
	}

	private static bool IsSupportedLiteral(string template, int start, int length)
	{
		for (var index = start; index < start + length; index++)
		{
			var character = template[index];
			if (!char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.' and not '~')
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsIdentifier(string value)
	{
		if (value.Length == 0 || (!char.IsLetter(value[0]) && value[0] != '_'))
		{
			return false;
		}

		return value.Skip(1).All(static character =>
			char.IsLetterOrDigit(character) || character == '_');
	}

	private static bool IsSupportedConstraint(string value)
		=> value.Equals("bool", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("datetime", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("double", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("float", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("guid", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("int", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("long", StringComparison.OrdinalIgnoreCase) ||
			value.Equals("nonfile", StringComparison.OrdinalIgnoreCase);
}
