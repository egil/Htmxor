using Microsoft.AspNetCore.Http;

namespace Htmxor.Builder;

internal static class HtmxorNamedFormCatalog
{
	public static void Validate(
		IReadOnlyList<string> routedComponentTypeNames,
		IReadOnlyList<string> namedFormComponentTypeNames,
		IReadOnlyList<HtmxorGeneratedComponentAction> generatedActions)
	{
		string? previous = null;
		foreach (var name in namedFormComponentTypeNames)
		{
			if (string.IsNullOrWhiteSpace(name) ||
				(previous is not null && StringComparer.Ordinal.Compare(previous, name) >= 0) ||
				!routedComponentTypeNames.Contains(name, StringComparer.Ordinal))
			{
				throw new InvalidOperationException(
					"The generated named-form manifest must contain unique component names in ordinal order from the component manifest.");
			}

			previous = name;
		}

		foreach (var action in generatedActions.Where(static action => HttpMethods.IsPost(action.HttpMethod)))
		{
			if (namedFormComponentTypeNames.Contains(action.ComponentType.FullName!, StringComparer.Ordinal))
			{
				throw new InvalidOperationException(
					$"Component '{action.ComponentType.FullName}' {HtmxorRejectionReasons.NamedFormPost}.");
			}
		}
	}
}
