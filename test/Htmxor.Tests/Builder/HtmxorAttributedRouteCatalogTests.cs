using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Builder;

public sealed class HtmxorAttributedRouteCatalogTests
{
	[Fact]
	public void Build_preserves_two_distinct_declarations_in_type_name_order()
	{
		var fixture = DynamicComponentAssembly.Create(
			new("PackageConsumer.SummaryComponent", "/summaries/{SummaryId:int}", "summary.policy"),
			new("PackageConsumer.ReportComponent", "/reports/{ReportId:guid}", "report.policy", UseNamedPolicy: true));

		var descriptors = HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest);

		Assert.Collection(
			descriptors,
			descriptor => AssertDescriptor(
				descriptor,
				fixture.Types[1],
				"/reports/{ReportId:guid}",
				"report.policy"),
			descriptor => AssertDescriptor(
				descriptor,
				fixture.Types[0],
				"/summaries/{SummaryId:int}",
				"summary.policy"));
	}

	[Fact]
	public async Task Bridge_maps_both_declarations_through_the_supplied_group()
	{
		var fixture = DynamicComponentAssembly.Create(
			new("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"),
			new("PackageConsumer.SummaryComponent", "/summaries/{SummaryId:int}", "summary.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out var groupMetadata);

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest);

		var endpoints = GetGeneratedEndpoints(app);
		Assert.Collection(
			endpoints.OrderBy(static endpoint => endpoint.RoutePattern.RawText, StringComparer.Ordinal),
			endpoint => AssertEndpoint(endpoint, fixture.Types[0], "/catalog/reports/{ReportId:int}", "report.policy", groupMetadata),
			endpoint => AssertEndpoint(endpoint, fixture.Types[1], "/catalog/summaries/{SummaryId:int}", "summary.policy", groupMetadata));
	}

	[Fact]
	public void Build_rejects_a_route_declaration_outside_the_manifest()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.ReportComponent",
				"/reports/{ReportId:int}",
				"report.policy"));

		var exception = Assert.Throws<InvalidOperationException>(() =>
			HtmxorAttributedRouteCatalog.Build(fixture.Assembly, []));

		Assert.Contains("outside the project-root component manifest", exception.Message, StringComparison.Ordinal);
		Assert.Contains("PackageConsumer.ReportComponent", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Build_ignores_an_unrouted_manifest_name_that_does_not_resolve()
	{
		var fixture = DynamicComponentAssembly.Create();

		var descriptors = HtmxorAttributedRouteCatalog.Build(
			fixture.Assembly,
			["PackageConsumer.UnroutedCustomNamespaceComponent"]);

		Assert.Empty(descriptors);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Bridge_maps_nothing_when_the_second_components_metadata_is_unsupported(bool throwsOnConstruction)
	{
		var fixture = DynamicComponentAssembly.Create(
			new("PackageConsumer.AValidComponent", "/valid/{Id:int}", "valid.policy"),
			new(
				"PackageConsumer.ZInvalidComponent",
				"/invalid/{Id:int}",
				"invalid.policy",
				HasAdditionalRouteFilter: !throwsOnConstruction,
				HasThrowingMetadata: throwsOnConstruction));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest));

		Assert.Contains("PackageConsumer.ZInvalidComponent", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_a_generated_action_is_outside_the_project_root_manifest()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			typeof(global::Htmxor.TestApp.App),
			HttpMethods.Put,
			"PackageConsumer.App.PutReport");

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		Assert.Contains("does not belong to the project-root component manifest", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_does_not_widen_an_explicit_get_only_route_for_a_generated_stock_action()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.ReportComponent.PutReport");

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest,
			[generatedAction]);

		var endpoint = Assert.Single(GetGeneratedEndpoints(app));
		Assert.Equal(
			[HttpMethods.Get],
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
		Assert.Empty(endpoint.Metadata.GetOrderedMetadata<HtmxorComponentActionDescriptor>());
		Assert.Null(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>());
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_a_generated_action_uses_an_unsupported_method()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Post,
			"PackageConsumer.ReportComponent.PostReport");

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		Assert.Contains("supports only a generated PUT action", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_more_than_one_generated_action_is_supplied()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var firstAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.ReportComponent.PutReport");
		var secondAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.ReportComponent.PutReportAgain");

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[firstAction, secondAction]));

		Assert.Contains("supports exactly one generated component action", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	private static void AssertDescriptor(
		HtmxorComponentGetRouteDescriptor descriptor,
		Type componentType,
		string route,
		string policy)
	{
		Assert.Same(componentType, descriptor.ComponentType);
		Assert.Equal(route, descriptor.NormalizedRoute);
		var routeMetadata = Assert.Single(descriptor.Metadata.OfType<HtmxRouteAttribute>());
		Assert.Equal(route, routeMetadata.Template);
		Assert.Equal(HttpMethods.Get, Assert.Single(routeMetadata.Methods));
		var authorization = Assert.Single(descriptor.Metadata.OfType<AuthorizeAttribute>());
		Assert.Equal(policy, authorization.Policy);
	}

	private static void AssertEndpoint(
		RouteEndpoint endpoint,
		Type componentType,
		string route,
		string policy,
		GroupMetadata groupMetadata)
	{
		Assert.Equal(route, endpoint.RoutePattern.RawText);
		Assert.Same(componentType, endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type);
		Assert.Same(groupMetadata, endpoint.Metadata.GetRequiredMetadata<GroupMetadata>());
		Assert.Equal(
			policy,
			Assert.Single(endpoint.Metadata.GetOrderedMetadata<AuthorizeAttribute>()).Policy);
		Assert.Equal(
			route["/catalog".Length..],
			endpoint.Metadata.GetRequiredMetadata<HtmxRouteAttribute>().Template);
	}

	private static WebApplication CreateApplication(
		out RouteGroupBuilder group,
		out RazorComponentsEndpointConventionBuilder componentBuilder,
		out GroupMetadata groupMetadata)
	{
		var builder = WebApplication.CreateBuilder();
		builder.Services.AddRazorComponents();
		var app = builder.Build();
		groupMetadata = new GroupMetadata("catalog-group");
		group = app.MapGroup("/catalog").WithMetadata(groupMetadata);
		componentBuilder = group.MapRazorComponents<global::Htmxor.TestApp.App>();

		return app;
	}

	private static RouteEndpoint[] GetGeneratedEndpoints(WebApplication app)
		=> ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(static dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(static endpoint => endpoint.Metadata.GetMetadata<HtmxorDirectEndpointMetadata>() is not null)
			.ToArray();

	private sealed record GroupMetadata(string Value);

	private sealed record ComponentDefinition(
		string TypeName,
		string Route,
		string Policy,
		bool UseNamedPolicy = false,
		bool HasAdditionalRouteFilter = false,
		bool HasThrowingMetadata = false);

	private sealed record DynamicComponentAssembly(Assembly Assembly, Type[] Types, string[] Manifest)
	{
		public static DynamicComponentAssembly Create(params ComponentDefinition[] definitions)
		{
			var assemblyName = new AssemblyName($"HtmxorCatalogTests_{Guid.NewGuid():N}");
			var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			var module = assembly.DefineDynamicModule(assemblyName.Name!);
			var types = definitions.Select(definition => CreateComponent(module, definition)).ToArray();
			var manifest = types
				.Select(static type => type.FullName!)
				.OrderBy(static typeName => typeName, StringComparer.Ordinal)
				.ToArray();

			return new DynamicComponentAssembly(assembly, types, manifest);
		}

		private static Type CreateComponent(ModuleBuilder module, ComponentDefinition definition)
		{
			var type = module.DefineType(
				definition.TypeName,
				TypeAttributes.Public | TypeAttributes.Class,
				typeof(ComponentBase));
			type.DefineDefaultConstructor(MethodAttributes.Public);
			AddRoute(type, definition);
			AddAuthorization(type, definition);
			if (definition.HasThrowingMetadata)
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(ThrowingMetadataAttribute).GetConstructor(Type.EmptyTypes)!,
					[]));
			}

			return type.CreateType()!;
		}

		private static void AddRoute(TypeBuilder type, ComponentDefinition definition)
		{
			var properties = new List<PropertyInfo>
			{
				typeof(HtmxRouteAttribute).GetProperty(nameof(HtmxRouteAttribute.Methods))!,
			};
			var values = new List<object> { new[] { HttpMethods.Get } };
			if (definition.HasAdditionalRouteFilter)
			{
				properties.Add(typeof(HtmxRouteAttribute).GetProperty(nameof(HtmxRouteAttribute.Target))!);
				values.Add("invalid-target");
			}

			type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(HtmxRouteAttribute).GetConstructor([typeof(string)])!,
				[definition.Route],
				properties.ToArray(),
				values.ToArray()));
		}

		private static void AddAuthorization(TypeBuilder type, ComponentDefinition definition)
		{
			if (definition.UseNamedPolicy)
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
					[],
					[typeof(AuthorizeAttribute).GetProperty(nameof(AuthorizeAttribute.Policy))!],
					[definition.Policy]));
				return;
			}

			type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AuthorizeAttribute).GetConstructor([typeof(string)])!,
				[definition.Policy]));
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ThrowingMetadataAttribute : Attribute
	{
		public ThrowingMetadataAttribute() => throw new InvalidOperationException("metadata constructor control");
	}
}
