using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

internal sealed class RazorFormValidation
{
	private readonly Compilation compilation;
	private readonly AnalyzerOptions options;
	private readonly HtmxorRouteSymbols symbols;
	private readonly CancellationToken token;
	private readonly RazorComponentTypeNames? typeNames;

	private RazorFormValidation(Compilation compilation, AnalyzerOptions options, HtmxorRouteSymbols symbols, CancellationToken token)
	{
		this.compilation = compilation;
		this.options = options;
		this.symbols = symbols;
		this.token = token;
		typeNames = RazorComponentTypeNames.Create(options.AnalyzerConfigOptionsProvider, options.AdditionalFiles, token);
	}

	public static void AnalyzeLocal(SemanticModelAnalysisContext context)
	{
		if (HtmxorRouteSymbols.Resolve(context.SemanticModel.Compilation) is not { } symbols ||
			!HtmxorRouteManifest.IsRazorGeneratedPath(context.SemanticModel.SyntaxTree.FilePath))
		{
			return;
		}
		var validation = new RazorFormValidation(context.SemanticModel.Compilation, context.Options, symbols, context.CancellationToken);
		var actions = FindLocalPostActions(context.SemanticModel, context.CancellationToken);
		validation.AnalyzeForms(RazorCompiledForm.Find(context.SemanticModel, context.CancellationToken), actions,
			local: true, context.ReportDiagnostic);
	}

	public static void Analyze(CompilationAnalysisContext context)
	{
		if (HtmxorRouteSymbols.Resolve(context.Compilation) is not { } symbols)
		{
			return;
		}
		var validation = new RazorFormValidation(context.Compilation, context.Options, symbols, context.CancellationToken);
		var actions = RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken)
			.Where(static binding => binding.HttpMethod == "POST").ToArray();
		validation.AnalyzeForms(RazorCompiledForm.Find(context.Compilation, context.CancellationToken), actions,
			local: false, context.ReportDiagnostic);
	}

	private static RazorCompiledEventBinding[] FindLocalPostActions(SemanticModel model, CancellationToken token)
		=> model.SyntaxTree.GetRoot(token).DescendantNodes().OfType<InvocationExpressionSyntax>()
			.Select(invocation => RazorCompiledEventBinding.TryRead(model, invocation, token))
			.OfType<RazorCompiledEventBinding>().Where(static binding => binding.HttpMethod == "POST").ToArray();

	private void AnalyzeForms(IEnumerable<RazorCompiledForm> forms, RazorCompiledEventBinding[] actions,
		bool local, Action<Diagnostic> report)
	{
		foreach (var group in forms.GroupBy(static form => form.Component, SymbolEqualityComparer.Default))
		{
			var ownedForms = group.ToArray();
			var component = ownedForms[0].Component;
			var routes = component.GetAttributes().Where(attribute => IsRoute(attribute, symbols.HtmxRoute)).ToArray();
			var route = routes.FirstOrDefault();
			if (!HasKnownOwner(component, routes)) continue;
			var declarations = ReadDeclarations(component);
			var ownedActions = actions.Where(action => SymbolEqualityComparer.Default.Equals(action.Component, component)).ToArray();
			for (var index = 0; index < ownedForms.Length; index++)
			{
				var form = ownedForms[index];
				if (!form.IsValid) continue;
				var location = GetLocation(ownedForms, index, declarations);
				ReportIssues(form, route, ownedActions, location, local, report);
			}
		}
	}

	private bool HasKnownOwner(INamedTypeSymbol component, AttributeData[] routes)
	{
		if (routes.Length == 0)
		{
			return component.GetAttributes().Any(attribute => IsRoute(attribute, symbols.Route));
		}
		if (routes.Length != 1) return false;
		var route = routes[0];
		return route.ConstructorArguments.Length == 1 &&
			!HtmxorRoutedComponent.HasBindingError(route.ConstructorArguments[0]);
	}

	private static bool IsRoute(AttributeData attribute, INamedTypeSymbol? type)
		=> type is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, type);

	private IReadOnlyList<RazorFormDeclaration> ReadDeclarations(INamedTypeSymbol component)
	{
		var file = options.AdditionalFiles.FirstOrDefault(file =>
			typeNames?.GetTypeName(file, token) == component.ToDisplayString() &&
			HtmxorRouteManifest.HasCompiledRazorDeclaration(component, new[] { typeNames.GetGeneratedPath(file.Path) }));
		return file is null ? Array.Empty<RazorFormDeclaration>() :
			HtmxorFormManifestDeclaration.ReadStockForms(file, component.ToDisplayString(), options.AdditionalFiles, token);
	}

	private void ReportIssues(RazorCompiledForm form, AttributeData? route, RazorCompiledEventBinding[] actions,
		Location location, bool local, Action<Diagnostic> report)
	{
		var hasLocalPost = actions.Any(action => action.SemanticModel.SyntaxTree == form.Location.SourceTree);
		foreach (var issue in GetLocalIssues(form, route, hasLocalPost))
		{
			// External authored fallbacks have no source tree and remain owned by complete analysis.
			if (local == (location.SourceTree is not null))
			{
				report(local ? issue.ToLocalDiagnostic(location) : issue.ToDiagnostic(location));
			}
		}
		if (!local && GetAggregateIssue(form, route, actions.Length > 0, hasLocalPost) is { } aggregate)
		{
			report(aggregate.ToDiagnostic(location));
		}
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetLocalIssues(RazorCompiledForm form, AttributeData? route, bool hasPostAction)
	{
		if (form.HasValidName && form.IsNamed && hasPostAction)
		{
			yield return NamedConflict();
		}
		if (route is null || !form.IsPost) yield break;
		if (form.HasValidName && !form.IsNamed)
		{
			yield return form.IsEditForm
				? new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.UnnamedEditForm, "an EditForm on an HtmxRoute owner requires FormName")
				: new HtmxorDiagnosticIssue(HtmxorDiagnosticCause.UnnamedPostForm, "a POST form on an HtmxRoute owner requires @formname");
		}
		if (ExcludesPost(route))
		{
			yield return new(HtmxorDiagnosticCause.ExplicitMethodsConflict,
				"explicit HtmxRoute.Methods does not allow POST for its form; add POST or remove the form");
		}
	}

	private static bool ExcludesPost(AttributeData route)
	{
		var methods = route.NamedArguments.FirstOrDefault(static argument => argument.Key == "Methods");
		return methods.Key is not null && HtmxorRoutedComponent.HasSupportedExplicitMethods(methods.Value) &&
			!methods.Value.Values.Any(static value => string.Equals(value.Value as string, "POST", StringComparison.OrdinalIgnoreCase));
	}

	private HtmxorDiagnosticIssue? GetAggregateIssue(RazorCompiledForm form, AttributeData? route, bool hasPostAction, bool hasLocalPost)
	{
		if (!form.HasValidName) return null;
		if (form.IsNamed && hasPostAction)
		{
			return hasLocalPost ? null : NamedConflict();
		}
		if (route is null || !form.IsPost || !form.IsNamed || route.NamedArguments.Any(static argument => argument.Key == "Methods") ||
			HtmxorCompiledFormManifest.Contains(compilation, form.Component, token))
		{
			return null;
		}
		return new(HtmxorDiagnosticCause.UnmatchedCompiledForm,
			"the compiled POST form has no matching generated POST inference for its component; move the form into the route owner's own Razor markup, or replace the form with a supported explicit POST action");
	}

	private static HtmxorDiagnosticIssue NamedConflict()
		=> new(HtmxorDiagnosticCause.NamedFormPostConflict,
			"named forms and a POST action cannot share one route owner; use named forms or a POST action");

	private static Location GetLocation(RazorCompiledForm[] forms, int index, IReadOnlyList<RazorFormDeclaration> declarations)
	{
		var form = forms[index];
		if (form.Location.GetMappedLineSpan().Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
		{
			return form.Location;
		}

		var shape = (form.IsEditForm, form.IsNamed, form.IsPost);
		var ordinal = forms.Take(index).Count(candidate => (candidate.IsEditForm, candidate.IsNamed, candidate.IsPost) == shape);
		return declarations.Where(candidate => (candidate.IsEditForm, candidate.IsNamed, candidate.IsPost) == shape)
			.Skip(ordinal).FirstOrDefault()?.Location ?? form.Location;
	}

}
