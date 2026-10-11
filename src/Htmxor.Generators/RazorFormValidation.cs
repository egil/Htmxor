using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Htmxor.Generators;

internal static class RazorFormValidation
{
	public static void Analyze(CompilationAnalysisContext context)
	{
		var symbols = HtmxorRouteSymbols.Resolve(context.Compilation);
		if (symbols is null)
		{
			return;
		}

		var typeNames = RazorComponentTypeNames.Create(context.Options.AnalyzerConfigOptionsProvider,
			context.Options.AdditionalFiles, context.CancellationToken);
		var postActions = RazorCompiledEventBinding.Find(context.Compilation, context.CancellationToken)
			.Where(static binding => binding.HttpMethod == "POST").Select(static binding => binding.Component)
			.ToArray();
		foreach (var group in RazorCompiledForm.Find(context.Compilation, context.CancellationToken).GroupBy(static form => form.Component, SymbolEqualityComparer.Default))
		{
			AnalyzeOwner(context, symbols, typeNames, group.ToArray(), postActions);
		}
	}

	private static void AnalyzeOwner(CompilationAnalysisContext context, HtmxorRouteSymbols symbols,
		RazorComponentTypeNames? typeNames, RazorCompiledForm[] forms, INamedTypeSymbol[] postActions)
	{
		var component = forms[0].Component;
		var attributes = component.GetAttributes();
		var htmxRoute = attributes.FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.HtmxRoute));
		var stockRoute = attributes.Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.Route));
		if (htmxRoute is null && !stockRoute)
		{
			return;
		}

		var file = context.Options.AdditionalFiles.FirstOrDefault(file =>
			typeNames?.GetTypeName(file, context.CancellationToken) == component.ToDisplayString() &&
			HtmxorRouteManifest.HasCompiledRazorDeclaration(component, new[] { typeNames.GetGeneratedPath(file.Path) }));
		var declarations = file is null ? Array.Empty<RazorFormDeclaration>() : RazorFormDeclaration.Read(file, context.CancellationToken);
		for (var index = 0; index < forms.Length; index++)
		{
			var form = forms[index];
			var reason = GetReason(form, htmxRoute, postActions.Any(action => SymbolEqualityComparer.Default.Equals(action, component)),
				HtmxorCompiledFormManifest.Contains(context.Compilation, component, context.CancellationToken));
			if (reason is not null)
			{
				var location = form.Location.GetMappedLineSpan().Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
					? form.Location : index < declarations.Count ? declarations[index].Location : form.Location;
				context.ReportDiagnostic(Diagnostic.Create(HtmxorActionDeclarationAnalyzer.UnsupportedDeclaration, location, reason));
			}
		}
	}

	private static string? GetReason(RazorCompiledForm form, AttributeData? route, bool hasPostAction, bool inferredNamedForm)
	{
		if (form.IsNamed && hasPostAction)
		{
			return "named forms and a POST action cannot share one route owner; use named forms or a POST action";
		}

		if (route is null || !form.IsPost)
		{
			return null;
		}

		if (!form.IsNamed)
		{
			return form.IsEditForm
				? "an EditForm on an HtmxRoute owner requires FormName"
				: "a POST form on an HtmxRoute owner requires @formname";
		}

		return AllowsPost(route, inferredNamedForm);
	}

	private static string? AllowsPost(AttributeData route, bool inferredNamedForm)
	{
		var methods = route.NamedArguments.FirstOrDefault(static argument => argument.Key == "Methods");
		if (methods.Key is not null)
		{
			return methods.Value.Kind == TypedConstantKind.Array && methods.Value.Values.Any(static value =>
				value.Value is string method && string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
				? null : "explicit HtmxRoute.Methods does not allow POST for its form";
		}

		return inferredNamedForm ? null : "the compiled POST form has no matching generated POST inference for its component";
	}
}
