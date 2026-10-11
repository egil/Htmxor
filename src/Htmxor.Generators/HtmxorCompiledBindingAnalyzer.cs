using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorCompiledBindingAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		ImmutableArray.Create(HtmxorActionDeclarationAnalyzer.UnsupportedDeclaration);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterCompilationAction(AnalyzeCompilation);
	}

	private static void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		RazorFormValidation.Analyze(context);
		var actionType = context.Compilation.GetTypeByMetadataName("Htmxor.Builder.HtmxorGeneratedComponentAction");
		if (actionType is null)
		{
			return;
		}

		var normalOnly = context.Compilation.GetTypeByMetadataName("Htmxor.DisableHtmxDirectRoutingAttribute");
		foreach (var binding in RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken))
		{
			if (HtmxorCompiledAction.Matches(context.Compilation, binding, actionType, context.CancellationToken))
			{
				continue;
			}

			var reason = binding.Component.GetAttributes().Any(attribute =>
				SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, normalOnly))
				? "an inferred @" + binding.AttributeName + " binding on a component marked with DisableHtmxDirectRouting is not supported"
				: "the compiled @" + binding.AttributeName + " binding has no matching generated action for its component, method and handler";
			context.ReportDiagnostic(Diagnostic.Create(
				HtmxorActionDeclarationAnalyzer.UnsupportedDeclaration,
				binding.Location,
				reason));
		}
	}

}
