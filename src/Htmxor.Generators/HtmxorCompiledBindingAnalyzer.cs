using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorCompiledBindingAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		HtmxorDiagnosticRules.ActionDiagnostics.AddRange(HtmxorDiagnosticRules.LocalActionDiagnostics);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterSemanticModelAction(RazorFormValidation.AnalyzeLocal);
		context.RegisterCompilationAction(RazorFormValidation.Analyze);
		context.RegisterCompilationAction(static context =>
		{
			var symbols = HtmxorRouteSymbols.Resolve(context.Compilation);
			if (symbols is not null)
			{
				var validation = new HtmxorInferredActionValidation(context.Options, symbols, context.CancellationToken);
				AnalyzeCompleteness(context, validation);
			}
		});
	}


	private static void AnalyzeCompleteness(CompilationAnalysisContext context, HtmxorInferredActionValidation validation)
	{
		var actionType = context.Compilation.GetTypeByMetadataName("Htmxor.Builder.HtmxorGeneratedComponentAction");
		if (actionType is null)
		{
			return;
		}
		foreach (var binding in RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken))
		{
			var declaration = validation.FindDeclaration(binding);
			// A known fault explains this binding's missing manifest, but cannot excuse a different binding's mismatch.
			if (declaration is not null && (!validation.MatchesOwner(binding.Component, declaration) || !declaration.UnsupportedIssues.IsEmpty) ||
				validation.GetIssues(binding.SemanticModel, binding, declaration,
					context.CancellationToken).Any() ||
				HtmxorCompiledAction.Matches(context.Compilation, binding, actionType, context.CancellationToken))
			{
				continue;
			}
			context.ReportDiagnostic(HtmxorInferredActionValidation.WithRemediation(new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.UnmatchedCompiledBinding,
				"the compiled @" + binding.AttributeName + " binding has no matching generated action for its component, method and handler"))
				.ToDiagnostic(binding.Location));
		}
	}

}
