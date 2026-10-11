using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Htmxor.Generators;

internal static class HtmxorCompiledFormManifest
{
	public static bool Contains(Compilation compilation, INamedTypeSymbol component, CancellationToken cancellationToken)
	{
		var registration = compilation.Assembly.GetTypeByMetadataName(
			"Microsoft.AspNetCore.Builder.HtmxorGeneratedRouteRegistrationExtensions");
		return HasStockFormDeclaration(compilation, component, cancellationToken) && registration?.GetMembers("NamedFormComponentTypeNames").OfType<IFieldSymbol>()
			.SelectMany(static field => field.DeclaringSyntaxReferences)
			.Where(static reference => reference.SyntaxTree.FilePath.Replace('\\', '/').EndsWith(
				"Htmxor.Generators.HtmxorRouteGenerator/HtmxorGeneratedRouteRegistration.g.cs", StringComparison.Ordinal))
			.Any(reference => ContainsOwner(compilation, reference, component, cancellationToken)) == true;
	}

	private static bool HasStockFormDeclaration(Compilation compilation, INamedTypeSymbol component, CancellationToken cancellationToken)
		=> component.GetMembers("__HtmxorHasOwnNamedForms").OfType<IPropertySymbol>()
			.SelectMany(static property => property.DeclaringSyntaxReferences)
			.Where(static reference => reference.SyntaxTree.FilePath.Replace('\\', '/').EndsWith(
				"Htmxor.Generators.HtmxorRouteGenerator/HtmxorGeneratedForms.g.cs", StringComparison.Ordinal))
			.Any(reference => IsStockFormCondition(compilation, reference, cancellationToken));

	private static bool IsStockFormCondition(Compilation compilation, SyntaxReference reference, CancellationToken cancellationToken)
	{
		var model = compilation.GetSemanticModel(reference.SyntaxTree);
		var syntax = reference.GetSyntax(cancellationToken);
		return syntax.DescendantNodes().OfType<LiteralExpressionSyntax>().Any(static literal => literal.IsKind(SyntaxKind.TrueLiteralExpression)) ||
			syntax.DescendantNodes().OfType<BinaryExpressionSyntax>().Where(static binary => binary.IsKind(SyntaxKind.EqualsExpression))
				.Any(binary => binary.Left is TypeOfExpressionSyntax left &&
					SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(left.Type, cancellationToken).Type,
						compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.Forms.EditForm")));
	}

	private static bool ContainsOwner(Compilation compilation, SyntaxReference reference,
		INamedTypeSymbol component, CancellationToken cancellationToken)
	{
		var declaration = reference.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
		var model = compilation.GetSemanticModel(reference.SyntaxTree);
		return declaration?.Initializer?.Value.DescendantNodes().OfType<LiteralExpressionSyntax>()
			.Any(literal => Equals(model.GetConstantValue(literal, cancellationToken).Value, component.ToDisplayString())) == true;
	}
}
