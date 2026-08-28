using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

internal static class HtmxorActionDiagnostics
{
	public static DiagnosticDescriptor UnsupportedDeclaration { get; } = new(
		"HTMXOR002",
		"Unsupported component action declaration",
		"Unsupported component action declaration: {0}",
		"Htmxor.Generators",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		customTags: new[]
		{
			WellKnownDiagnosticTags.NotConfigurable,
			WellKnownDiagnosticTags.CompilationEnd,
		});
}
