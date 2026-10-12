using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Htmxor.Generators;

internal static class HtmxorCompiledFormManifest
{
	public static bool Contains(Compilation compilation, INamedTypeSymbol component, CancellationToken cancellationToken)
	{
		var registration = compilation.Assembly.GetTypeByMetadataName(
			"Microsoft.AspNetCore.Builder.HtmxorGeneratedRouteRegistrationExtensions");
		return registration?.GetMembers("NamedFormComponentTypeNames").OfType<IFieldSymbol>()
			.SelectMany(static field => field.DeclaringSyntaxReferences)
			.Where(static reference => reference.SyntaxTree.FilePath.Replace('\\', '/').EndsWith(
				"Htmxor.Generators.HtmxorRouteGenerator/HtmxorGeneratedRouteRegistration.g.cs", StringComparison.Ordinal))
			.Any(reference => ContainsOwner(compilation, reference, component, cancellationToken)) == true;
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
