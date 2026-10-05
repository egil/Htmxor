using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Htmxor;

public sealed class DisableHtmxDirectRoutingAttributeTests
{
	[Fact]
	public void Shape_matches_the_approved_170_decision()
	{
		var type = typeof(DisableHtmxDirectRoutingAttribute);

		Assert.True(type.IsSealed);
		Assert.Empty(Assert.Single(type.GetConstructors()).GetParameters());
		Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

		var usage = type.GetCustomAttribute<AttributeUsageAttribute>();
		Assert.NotNull(usage);
		Assert.Equal(AttributeTargets.Class, usage!.ValidOn);
		Assert.False(usage.AllowMultiple);
		Assert.False(usage.Inherited);
	}

	[Fact]
	public void Inherited_false_means_a_base_class_marker_is_invisible_to_a_derived_type_at_runtime()
	{
		// Characterization, not red: this is "ignored at runtime" for free, from the real
		// AttributeUsage(Inherited = false) shape added in commit 82fc101, with no Htmxor
		// behavior involved. #175 still needs the analyzer to flag this declaration shape
		// (HTMXOR003 cause (b)); that half is red, pinned in HtmxorRouteDeclarationAnalyzerTests.
		var derivedMarker = typeof(DisableHtmxDirectRoutingDerivedFixture)
			.GetCustomAttribute<DisableHtmxDirectRoutingAttribute>(inherit: false);
		var inheritedLookup = typeof(DisableHtmxDirectRoutingDerivedFixture)
			.GetCustomAttribute<DisableHtmxDirectRoutingAttribute>(inherit: true);
		var baseMarker = typeof(DisableHtmxDirectRoutingMarkedBaseFixture)
			.GetCustomAttribute<DisableHtmxDirectRoutingAttribute>(inherit: false);

		Assert.Null(derivedMarker);
		Assert.Null(inheritedLookup);
		Assert.NotNull(baseMarker);
	}

	[Fact]
	public void A_duplicate_declaration_is_CS0579_from_AllowMultiple_false_alone()
	{
		// Characterization: AllowMultiple = false on the real compiled attribute already makes a
		// duplicate marker a compiler error, with no Htmxor-authored diagnostic involved.
		var compilation = CSharpCompilation.Create(
			"Htmxor.DisableHtmxDirectRoutingAttribute.Cs0579Tests",
			new[]
			{
				CSharpSyntaxTree.ParseText(
					"""
					[Htmxor.DisableHtmxDirectRouting]
					[Htmxor.DisableHtmxDirectRouting]
					public sealed class DuplicateMarkerComponent : Microsoft.AspNetCore.Components.ComponentBase;
					"""),
			},
			CreateCs0579References(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var diagnostic = Assert.Single(compilation.GetDiagnostics());

		Assert.Equal("CS0579", diagnostic.Id);
	}

	private static MetadataReference[] CreateCs0579References()
	{
		var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
			.Split(Path.PathSeparator) ?? Array.Empty<string>();
		var requiredAssemblies = new[]
		{
			typeof(object).Assembly.Location,
			typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.Location,
			typeof(DisableHtmxDirectRoutingAttribute).Assembly.Location,
		};

		return platformAssemblies
			.Concat(requiredAssemblies)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToArray();
	}
}

[DisableHtmxDirectRouting]
internal class DisableHtmxDirectRoutingMarkedBaseFixture;

internal sealed class DisableHtmxDirectRoutingDerivedFixture : DisableHtmxDirectRoutingMarkedBaseFixture;
