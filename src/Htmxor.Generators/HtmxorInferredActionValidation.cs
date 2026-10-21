using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

internal sealed class HtmxorInferredActionValidation
{
	private readonly RazorComponentTypeNames? typeNames;
	private readonly HtmxorRouteSymbols symbols;

	public HtmxorInferredActionValidation(AnalyzerOptions options, HtmxorRouteSymbols symbols, CancellationToken token)
	{
		this.symbols = symbols;
		typeNames = RazorComponentTypeNames.Create(options.AnalyzerConfigOptionsProvider, options.AdditionalFiles, token);
		Declarations = options.AdditionalFiles
			.Where(static file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
			.SelectMany(file => HtmxorComponentActionDeclaration.ParseAll(file, typeNames?.GetTypeName(file, token), token))
			.ToImmutableArray();
	}

	public ImmutableArray<HtmxorComponentActionDeclaration> Declarations { get; }

	public HtmxorComponentActionDeclaration? FindDeclaration(RazorCompiledEventBinding binding)
	{
		var mapped = binding.Location.GetMappedLineSpan();
		return Declarations.FirstOrDefault(declaration => declaration.HttpMethod == binding.HttpMethod &&
			string.Equals(declaration.Path, mapped.Path, StringComparison.OrdinalIgnoreCase) &&
			declaration.BindingLineSpan.Start <= mapped.StartLinePosition &&
			declaration.BindingLineSpan.End >= mapped.EndLinePosition);
	}

	public bool MatchesOwner(INamedTypeSymbol? component, HtmxorComponentActionDeclaration declaration)
		=> component?.ToDisplayString() == declaration.ComponentTypeName &&
			HtmxorRouteManifest.HasCompiledRazorDeclaration(component, new[] { typeNames?.GetGeneratedPath(declaration.Path) });

	public IEnumerable<HtmxorDiagnosticIssue> GetIssues(SemanticModel model, RazorCompiledEventBinding binding,
		HtmxorComponentActionDeclaration? declaration, CancellationToken token)
	{
		if (declaration is not null && !MatchesOwner(binding.Component, declaration))
		{
			return Enumerable.Empty<HtmxorDiagnosticIssue>();
		}

		var handlerIssues = binding.IsBound ? GetBindingIssues(model, binding, declaration, token) :
			Enumerable.Empty<HtmxorDiagnosticIssue>();
		return handlerIssues.Concat(GetOwnerIssues(binding.Component, declaration, binding.HttpMethod));
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetBindingIssues(SemanticModel model, RazorCompiledEventBinding binding,
		HtmxorComponentActionDeclaration? declaration, CancellationToken token)
	{
		// Scanned parse facts already belong to the generator; only a compiled-only expression needs local recovery.
		var expression = binding.Expression.ToString();
		if (RazorBindingValue.TryReadHandler(expression) is null)
		{
			if (declaration is null)
			{
				yield return new(RazorBindingValue.GetUnsupportedDiagnosticCause(expression),
					"@" + binding.AttributeName + " must name a handler method, not " +
					RazorBindingValue.GetUnsupportedCause(expression) + "; use [HtmxAction] on a supported instance method");
			}
		}
		else
		{
			foreach (var issue in HtmxorActionHandler.GetUnsupportedIssues(model, binding.Component, binding.Expression, token))
			{
				yield return issue;
			}
		}

		if (declaration is null && binding.HttpMethod == "GET")
		{
			yield return new(HtmxorDiagnosticCause.ImplicitGetBinding,
				"@onget declares nothing: GET is implicit, so remove @onget");
		}
	}

	public IEnumerable<HtmxorDiagnosticIssue> GetOwnerIssues(INamedTypeSymbol component,
		HtmxorComponentActionDeclaration? declaration, string httpMethod)
	{
		var attributes = component.GetAttributes();
		if (attributes.Any(attribute => Is(attribute, symbols.DisableHtmxDirectRouting)))
		{
			var bindingName = declaration is null ? " @on" + httpMethod.ToLowerInvariant() : string.Empty;
			yield return new(HtmxorDiagnosticCause.NormalOnlyAction,
				"an inferred" + bindingName + " binding on a component marked with DisableHtmxDirectRouting is not supported; remove the binding or marker");
		}
		var stock = attributes.Where(attribute => Is(attribute, symbols.Route)).ToArray();
		var htmx = attributes.Where(attribute => Is(attribute, symbols.HtmxRoute)).ToArray();
		if (stock.Length == 0 && htmx.Length == 0)
		{
			yield return new(HtmxorDiagnosticCause.UnroutedBinding,
				"the component owns no route, so the binding has no action to declare; add @page or HtmxRoute, or remove the binding");
			yield break;
		}
		foreach (var issue in GetRouteShapeIssues(declaration, stock, htmx))
		{
			yield return issue;
		}
		if (htmx.Any(route => DisallowsMethod(route, httpMethod)))
		{
			yield return new(HtmxorDiagnosticCause.ExplicitMethodsConflict,
				"explicit HtmxRoute.Methods is authoritative and does not allow the " + httpMethod +
				" binding; add the method or remove the binding");
		}
	}

	private static IEnumerable<HtmxorDiagnosticIssue> GetRouteShapeIssues(
		HtmxorComponentActionDeclaration? declaration, AttributeData[] stock, AttributeData[] htmx)
	{
		if (declaration?.UsesStockRoute == true && (stock.Length != 1 || htmx.Length != 0))
		{
			yield return new(HtmxorDiagnosticCause.LocalPageRouteMismatch,
				"a component with a local @page action must compile with exactly one stock route and no HtmxRoute");
		}
		if (declaration?.UsesStockRoute == false && stock.Length > 0)
		{
			yield return new(HtmxorDiagnosticCause.StockRouteWithoutLocalPage,
				"a component action without a local @page cannot use a compiled stock route");
		}
	}

	private static bool DisallowsMethod(AttributeData route, string httpMethod)
	{
		var methods = route.NamedArguments.FirstOrDefault(static argument => argument.Key == "Methods");
		return methods.Key is not null && !HtmxorRoutedComponent.HasBindingError(methods.Value) &&
			(methods.Value.Kind != TypedConstantKind.Array || methods.Value.IsNull || !methods.Value.Values.Any(value =>
				value.Value is string allowed && string.Equals(allowed, httpMethod, StringComparison.OrdinalIgnoreCase)));
	}

	private static bool Is(AttributeData attribute, INamedTypeSymbol? type)
		=> type is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, type);
	internal static HtmxorDiagnosticIssue WithRemediation(HtmxorDiagnosticIssue issue)
		=> issue.Cause is HtmxorDiagnosticCause.NormalOnlyAction or HtmxorDiagnosticCause.ImplicitGetBinding ? issue :
			issue.WithReason(HtmxorActionDeclarationAnalyzer.WithExplicitRemediation(issue.Reason));
}
