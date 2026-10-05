using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators;

// A DisableHtmxDirectRouting marker counts only on the routed component's own declaration; every other placement
// is reported rather than silently ignored at runtime.
internal sealed class HtmxorNormalOnlyDeclaration
{
	private HtmxorNormalOnlyDeclaration(INamedTypeSymbol type, AttributeData marker)
	{
		Type = type;
		Marker = marker;
	}

	private INamedTypeSymbol Type { get; }

	private AttributeData Marker { get; }

	public static IEnumerable<HtmxorNormalOnlyDeclaration> FindAll(
		IAssemblySymbol assembly,
		INamedTypeSymbol marker)
		=> HtmxorRoutedComponent.GetTypes(assembly.GlobalNamespace)
			.SelectMany(type => type.GetAttributes()
				.Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, marker))
				.Select(attribute => new HtmxorNormalOnlyDeclaration(type, attribute)));

	public string? GetUnsupportedReason(HtmxorRouteSymbols symbols, CancellationToken cancellationToken)
	{
		if (GetLocation(cancellationToken).GetMappedLineSpan().Path is { } path &&
			path.EndsWith("_Imports.razor", StringComparison.OrdinalIgnoreCase))
		{
			return "DisableHtmxDirectRouting declarations from _Imports.razor are not supported";
		}

		if (HasAttribute(symbols.HtmxRoute))
		{
			return "DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type";
		}

		return Type.IsAbstract ||
			symbols.Component is null ||
			!Type.AllInterfaces.Contains(symbols.Component, SymbolEqualityComparer.Default) ||
			symbols.Route is null ||
			!HasAttribute(symbols.Route)
			? "DisableHtmxDirectRouting requires a local stock @page or Route declaration"
			: null;
	}

	public Location GetLocation(CancellationToken cancellationToken)
		=> Marker.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
			?? Type.Locations.FirstOrDefault()
			?? Location.None;

	private bool HasAttribute(INamedTypeSymbol attributeType)
		=> Type.GetAttributes().Any(attribute =>
			SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
}
