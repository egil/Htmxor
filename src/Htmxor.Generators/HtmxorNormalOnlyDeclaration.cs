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

	public static IEnumerable<HtmxorNormalOnlyDeclaration> Find(
		INamedTypeSymbol type,
		INamedTypeSymbol marker)
		=> type.GetAttributes()
			.Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, marker))
			.Select(attribute => new HtmxorNormalOnlyDeclaration(type, attribute));

	public IEnumerable<string> GetUnsupportedReasons(HtmxorRouteSymbols symbols, CancellationToken cancellationToken)
	{
		if (RazorImportsFile.Matches(GetLocation(cancellationToken).GetMappedLineSpan().Path))
		{
			yield return "DisableHtmxDirectRouting declarations from _Imports.razor are not supported";
		}

		if (HasAttribute(symbols.HtmxRoute))
		{
			yield return "DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type";
		}

		if (Type.IsAbstract ||
			symbols.Component is null ||
			!Type.AllInterfaces.Contains(symbols.Component, SymbolEqualityComparer.Default) ||
			symbols.Route is null ||
			!HasAttribute(symbols.Route))
		{
			yield return "DisableHtmxDirectRouting requires a local stock @page or Route declaration";
		}
	}

	public Location GetLocation(CancellationToken cancellationToken)
		=> Marker.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
			?? Type.Locations.FirstOrDefault()
			?? Location.None;

	private bool HasAttribute(INamedTypeSymbol attributeType)
		=> Type.GetAttributes().Any(attribute =>
			SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
}
