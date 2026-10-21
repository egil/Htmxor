using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HtmxorActionDeclarationAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		HtmxorDiagnosticRules.ActionDiagnostics.AddRange(HtmxorDiagnosticRules.LocalActionDiagnostics);

	internal static string WithExplicitRemediation(string reason)
		=> "Use [HtmxAction] to explicitly declare a supported instance action instead of this binding; " + reason;

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static start =>
		{
			var action = start.Compilation.GetTypeByMetadataName("Htmxor.HtmxActionAttribute");
			var symbols = HtmxorRouteSymbols.Resolve(start.Compilation);
			if (action is not null && symbols is not null)
			{
				start.RegisterSyntaxNodeAction(
					context => HtmxorExplicitActionValidation.AnalyzeLocal(context, action, symbols),
					SyntaxKind.Attribute);
			}
			if (symbols is not null)
			{
				var validation = new HtmxorInferredActionValidation(start.Options, symbols, start.CancellationToken);
				start.RegisterSyntaxNodeAction(context => AnalyzeLocal(context, validation), SyntaxKind.InvocationExpression);
			}
			start.RegisterCompilationEndAction(AnalyzeCompilation);
		});
	}

	private static void AnalyzeLocal(SyntaxNodeAnalysisContext context, HtmxorInferredActionValidation validation)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		if (!HtmxorRouteManifest.IsRazorGeneratedPath(context.Node.SyntaxTree.FilePath) ||
			RazorCompiledEventBinding.TryRead(context.SemanticModel, (InvocationExpressionSyntax)context.Node,
				context.CancellationToken, includeUnbound: true) is not { } binding)
		{
			return;
		}
		foreach (var issue in validation.GetIssues(context.SemanticModel, binding, validation.FindDeclaration(binding), context.CancellationToken))
		{
			context.ReportDiagnostic(HtmxorInferredActionValidation.WithRemediation(issue).ToLocalDiagnostic(binding.Location));
		}
	}

	private static void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		HtmxorExplicitActionValidation.AnalyzeConflicts(context);
		var symbols = HtmxorRouteSymbols.Resolve(context.Compilation);
		if (symbols is null || symbols.Route is null)
		{
			return;
		}

		var validation = new HtmxorInferredActionValidation(context.Options, symbols, context.CancellationToken);
		var localDeclarations = new HashSet<HtmxorComponentActionDeclaration>();
		foreach (var binding in RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken, includeUnbound: true))
		{
			if (validation.FindDeclaration(binding) is { } declaration && validation.MatchesOwner(binding.Component, declaration))
			{
				localDeclarations.Add(declaration);
			}
		}
		foreach (var declaration in validation.Declarations)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			if (localDeclarations.Contains(declaration))
			{
				continue;
			}

			var component = context.Compilation.Assembly.GetTypeByMetadataName(declaration.ComponentTypeName);
			foreach (var issue in GetAggregateIssues(context, validation, component, declaration))
			{
				context.ReportDiagnostic(HtmxorInferredActionValidation.WithRemediation(issue)
					.ToDiagnostic(Location.Create(declaration.Path, declaration.Span, declaration.LineSpan)));
			}
		}
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetAggregateIssues(
		CompilationAnalysisContext context, HtmxorInferredActionValidation validation,
		INamedTypeSymbol? component, HtmxorComponentActionDeclaration declaration)
	{
		if (!validation.MatchesOwner(component, declaration))
		{
			yield return new(HtmxorDiagnosticCause.MismatchedRazorOwner,
				"the action owner must compile from the matching Razor component; " +
				"its type name is derived from the file's folder and @namespace");
			yield break;
		}
		if (!declaration.UnsupportedIssues.IsEmpty)
		{
			yield break;
		}
		if (declaration.HandlerName is not null && declaration.HandlerAccess is not null)
		{
			foreach (var issue in HtmxorActionHandler.GetUnsupportedIssues(
				context.Compilation, component!, declaration, context.CancellationToken))
			{
				yield return issue;
			}
		}
		foreach (var issue in validation.GetOwnerIssues(component!, declaration, declaration.HttpMethod))
		{
			yield return issue;
		}
	}
}
