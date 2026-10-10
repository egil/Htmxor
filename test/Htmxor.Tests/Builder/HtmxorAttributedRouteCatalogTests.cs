using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Builder;

public sealed class HtmxorAttributedRouteCatalogTests
{
	[Fact]
	public void Generated_action_preserves_the_original_public_constructor_shape()
	{
		var constructor = typeof(HtmxorGeneratedComponentAction).GetConstructor([
			typeof(Type),
			typeof(string),
			typeof(string),
			typeof(bool),
		]);

		Assert.NotNull(constructor);
	}

	/// <summary>
	/// <see cref="HtmxorAttributedRouteCatalog"/> accepts any stock authorization metadata, or
	/// none, exactly as it is accepted on a stock <c>@page</c>.
	/// </summary>
	[Fact]
	public void Build_accepts_no_authorization_metadata()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284NoneComponent",
			"/issue-284-shapes/none/{Id:int}",
			Policy: "unused",
			Authorization: static _ => { }));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		Assert.Empty(descriptor.Metadata.OfType<AuthorizeAttribute>());
		Assert.Empty(descriptor.Metadata.OfType<AllowAnonymousAttribute>());
	}

	[Fact]
	public void Build_accepts_bare_Authorize()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284BareComponent",
			"/issue-284-shapes/bare/{Id:int}",
			Policy: "unused",
			Authorization: static type => type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
				[]))));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		var authorize = Assert.Single(descriptor.Metadata.OfType<AuthorizeAttribute>());
		Assert.Null(authorize.Policy);
	}

	[Fact]
	public void Build_accepts_Authorize_with_Roles()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284RolesComponent",
			"/issue-284-shapes/roles/{Id:int}",
			Policy: "unused",
			Authorization: static type => type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
				[],
				[typeof(AuthorizeAttribute).GetProperty(nameof(AuthorizeAttribute.Roles))!],
				["admin"]))));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		var authorize = Assert.Single(descriptor.Metadata.OfType<AuthorizeAttribute>());
		Assert.Equal("admin", authorize.Roles);
	}

	[Fact]
	public void Build_accepts_Authorize_with_AuthenticationSchemes()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284SchemesComponent",
			"/issue-284-shapes/schemes/{Id:int}",
			Policy: "unused",
			Authorization: static type => type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
				[],
				[typeof(AuthorizeAttribute).GetProperty(nameof(AuthorizeAttribute.AuthenticationSchemes))!],
				["custom-scheme"]))));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		var authorize = Assert.Single(descriptor.Metadata.OfType<AuthorizeAttribute>());
		Assert.Equal("custom-scheme", authorize.AuthenticationSchemes);
	}

	[Fact]
	public void Build_accepts_two_Authorize_attributes()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284TwoComponent",
			"/issue-284-shapes/two/{Id:int}",
			Policy: "unused",
			Authorization: static type =>
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
					[],
					[typeof(AuthorizeAttribute).GetProperty(nameof(AuthorizeAttribute.Roles))!],
					["admin"]));
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
					[],
					[typeof(AuthorizeAttribute).GetProperty(nameof(AuthorizeAttribute.Policy))!],
					["issue-284.read"]));
			}));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		Assert.Equal(2, descriptor.Metadata.OfType<AuthorizeAttribute>().Count());
	}

	[Fact]
	public void Build_accepts_Authorize_inherited_from_a_base_component()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284InheritedComponent",
			"/issue-284-shapes/inherited/{Id:int}",
			Policy: "unused",
			Authorization: static _ => { },
			BaseAuthorization: static type => type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)!,
				[],
				[typeof(AuthorizeAttribute).GetProperty(nameof(AuthorizeAttribute.Policy))!],
				["issue-284.read"]))));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		var authorize = Assert.Single(descriptor.Metadata.OfType<AuthorizeAttribute>());
		Assert.Equal("issue-284.read", authorize.Policy);
	}

	[Fact]
	public void Build_accepts_AllowAnonymous()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284AnonymousComponent",
			"/issue-284-shapes/anonymous/{Id:int}",
			Policy: "unused",
			Authorization: static type => type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(AllowAnonymousAttribute).GetConstructor(Type.EmptyTypes)!,
				[]))));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		Assert.Single(descriptor.Metadata.OfType<AllowAnonymousAttribute>());
		Assert.Empty(descriptor.Metadata.OfType<AuthorizeAttribute>());
	}

	/// <summary>
	/// Stock <c>@page</c> honors any <see cref="IAuthorizeData"/>, not only <see
	/// cref="AuthorizeAttribute"/> itself.
	/// </summary>
	[Fact]
	public void Build_accepts_a_custom_IAuthorizeData_attribute()
	{
		var fixture = DynamicComponentAssembly.Create(new ComponentDefinition(
			"PackageConsumer.Issue284CustomAuthorizeDataComponent",
			"/issue-284-shapes/custom-authorize-data/{Id:int}",
			Policy: "unused",
			Authorization: static type => type.SetCustomAttribute(new CustomAttributeBuilder(
				typeof(CustomAuthorizeDataAttribute).GetConstructor(Type.EmptyTypes)!,
				[],
				[typeof(CustomAuthorizeDataAttribute).GetProperty(nameof(CustomAuthorizeDataAttribute.Policy))!],
				["custom.read"]))));

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		var authorize = Assert.Single(descriptor.Metadata.OfType<CustomAuthorizeDataAttribute>());
		Assert.Equal("custom.read", authorize.Policy);
	}

	[Fact]
	public void Build_preserves_arbitrary_declarations_in_type_name_order()
	{
		var fixture = DynamicComponentAssembly.Create(
			new("PackageConsumer.SummaryComponent", "/summaries/{SummaryId:int}", "summary.policy"),
			new("PackageConsumer.ReportComponent", "/reports/{ReportId:guid}", "report.policy", UseNamedPolicy: true),
			new("PackageConsumer.AuditComponent", "/audits/{AuditId:long}", "audit.policy"),
			new("PackageConsumer.ZebraComponent", "/zebras/{ZebraId:int}", "zebra.policy"));

		var descriptors = HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest);

		Assert.Collection(
			descriptors,
			descriptor => AssertDescriptor(
				descriptor,
				fixture.Types[2],
				"/audits/{AuditId:long}",
				"audit.policy"),
			descriptor => AssertDescriptor(
				descriptor,
				fixture.Types[1],
				"/reports/{ReportId:guid}",
				"report.policy"),
			descriptor => AssertDescriptor(
				descriptor,
				fixture.Types[0],
				"/summaries/{SummaryId:int}",
				"summary.policy"),
			descriptor => AssertDescriptor(
				descriptor,
				fixture.Types[3],
				"/zebras/{ZebraId:int}",
				"zebra.policy"));
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
	public async Task Generated_endpoint_rejects_nonmatching_route_representation()
	{
		await using var app = CreateRequestApplication(out var group);
		var route = new HtmxRouteAttribute("/issue-173")
		{
			CurrentUrl = "/orders",
			Target = "section#result",
			Targets = ["section#result", "div#fallback"],
		};
		group.MapHtmxorComponentEndpoint(
			new HtmxorComponentRouteDescriptor(
				typeof(RouteProbeComponent),
				"/issue-173",
				[route],
				[HttpMethods.Get]),
			[]);

		await app.StartAsync();
		using var client = app.GetTestClient();

		using var matchingRequest = CreateDirectRequest(
			"/catalog/issue-173",
			"https://example.test/orders",
			"section#result");
		using var matchingResponse = await client.SendAsync(matchingRequest);
		Assert.Equal(HttpStatusCode.OK, matchingResponse.StatusCode);

		using var nonmatchingRequest = CreateDirectRequest(
			"/catalog/issue-173",
			"https://example.test/other",
			"div#other");
		using var nonmatchingResponse = await client.SendAsync(nonmatchingRequest);
		Assert.Equal(HttpStatusCode.NotFound, nonmatchingResponse.StatusCode);
	}

	[Fact]
	public void Build_rejects_HtmxRoute_combined_with_DisableHtmxDirectRouting()
	{
		// Runtime counterpart of the analyzer's HTMXOR003 cause (c).
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.MarkedReportComponent",
				"/reports/{ReportId:int}",
				"report.policy",
				HasDisableHtmxDirectRouting: true));

		var exception = Assert.Throws<InvalidOperationException>(() =>
			HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

		Assert.Contains("DisableHtmxDirectRouting", exception.Message, StringComparison.Ordinal);
		Assert.Contains(fixture.Types[0].FullName!, exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Bridge_fails_closed_when_a_generated_stock_route_action_targets_a_DisableHtmxDirectRouting_marked_component()
	{
		// When analyzer execution is bypassed, the generator still emits a stock-route action for a
		// marked component (HtmxorActionGenerator only gates on the syntactic UnsupportedReason), and
		// that action's owner shape -- one compiled stock route, no HtmxRoute -- is exactly what a
		// UsesStockRoute action normally requires, so this must fail closed here rather than let
		// ConfigureEndpoint silently drop the action later.
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.MarkedReportComponent",
				"/unused",
				"report.policy",
				HasHtmxRoute: false,
				StockRoutes: ["/reports/{ReportId:int}"],
				HasDisableHtmxDirectRouting: true));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.MarkedReportComponent.PutReport",
			usesStockRoute: true);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		Assert.Contains(fixture.Types[0].FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("DisableHtmxDirectRouting", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
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

		// This defensive membership guard is unrelated to the manifest's name (an empty manifest
		// rejects any routed type regardless of where it lives), so only the stable fragment and
		// the type name are pinned, not the "project-root" wording the manifest no longer carries.
		Assert.Contains("outside the", exception.Message, StringComparison.Ordinal);
		Assert.Contains("manifest", exception.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("project-root", exception.Message, StringComparison.Ordinal);
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

	[Fact]
	public async Task Bridge_maps_route_representation_filters()
	{
		var fixture = DynamicComponentAssembly.Create(
			new("PackageConsumer.AValidComponent", "/valid/{Id:int}", "valid.policy"),
			new(
				"PackageConsumer.ZFilteredComponent",
				"/invalid/{Id:int}",
				"invalid.policy",
				HasAdditionalRouteFilter: true));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest);

		var endpoint = Assert.Single(
			GetGeneratedEndpoints(app),
			endpoint => endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type == fixture.Types[1]);
		var route = endpoint.Metadata.GetRequiredMetadata<HtmxRouteAttribute>();
		Assert.Equal("/orders", route.CurrentUrl);
		Assert.Equal("section#result", route.Target);
		Assert.Equal(new[] { "section#result", "div#fallback" }, route.Targets);
		Assert.Same(route, endpoint.Metadata.GetRequiredMetadata<EndpointMetadata>().HxRoute);
	}

	[Fact]
	public async Task Bridge_rejects_an_invalid_current_url_before_mapping()
	{
		await AssertInvalidRouteDeclarationAsync(
			new ComponentDefinition(
				"PackageConsumer.ZInvalidCurrentUrlComponent",
				"/invalid-current-url/{Id:int}",
				"invalid.policy",
				HasAdditionalRouteFilter: true,
				CurrentUrl: "ftp://example.test/orders"),
			"CurrentUrl");
	}

	[Fact]
	public async Task Bridge_rejects_an_invalid_target_before_mapping()
	{
		await AssertInvalidRouteDeclarationAsync(
			new ComponentDefinition(
				"PackageConsumer.ZInvalidTargetComponent",
				"/invalid-target/{Id:int}",
				"invalid.policy",
				HasAdditionalRouteFilter: true,
				Target: "section#"),
			"Target");
	}

	[Fact]
	public async Task Bridge_rejects_an_invalid_targets_value_before_mapping()
	{
		await AssertInvalidRouteDeclarationAsync(
			new ComponentDefinition(
				"PackageConsumer.ZInvalidTargetsComponent",
				"/invalid-targets/{Id:int}",
				"invalid.policy",
				HasAdditionalRouteFilter: true,
				Targets: ["section", " "]),
			"Targets");
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_metadata_construction_throws()
	{
		var fixture = DynamicComponentAssembly.Create(
			new("PackageConsumer.AValidComponent", "/valid/{Id:int}", "valid.policy"),
			new(
				"PackageConsumer.ZInvalidComponent",
				"/invalid/{Id:int}",
				"invalid.policy",
				HasThrowingMetadata: true));
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
	public async Task Bridge_maps_nothing_when_a_generated_action_is_outside_the_manifest()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			typeof(global::Htmxor.TestApp.App),
			HttpMethods.Put,
			"PackageConsumer.App.PutReport",
			usesStockRoute: true);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		// This defensive membership guard is unrelated to the manifest's name, so only the stable
		// fragment is pinned, not the "project-root" wording the manifest no longer carries.
		Assert.Contains("does not belong to the", exception.Message, StringComparison.Ordinal);
		Assert.Contains("manifest", exception.Message, StringComparison.Ordinal);
		Assert.DoesNotContain("project-root", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_does_not_widen_an_explicit_get_only_route_for_a_generated_stock_action()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.HtmxReportComponent",
				"/reports/{ReportId:int}",
				"report.policy"),
			new ComponentDefinition(
				"PackageConsumer.StockReportComponent",
				"/unused",
				"stock.policy",
				HasHtmxRoute: false,
				StockRoutes: ["/stock-reports/{ReportId:int}"]));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[1],
			HttpMethods.Put,
			"PackageConsumer.StockReportComponent.PutReport",
			usesStockRoute: true);

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
	public async Task Bridge_widens_an_omitted_methods_htmx_route_only_for_its_generated_action()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.ReportComponent",
				"/reports/{ReportId:int}",
				"report.policy",
				ExplicitMethods: false));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Patch,
			"PackageConsumer.ReportComponent.PatchReport",
			usesStockRoute: false);

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest,
			[generatedAction]);

		var endpoint = Assert.Single(GetGeneratedEndpoints(app));
		Assert.Equal(
			[HttpMethods.Get, HttpMethods.Patch],
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
		var action = Assert.Single(endpoint.Metadata.GetOrderedMetadata<HtmxorComponentActionDescriptor>());
		Assert.Equal(HttpMethods.Patch, action.HttpMethod);
		Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation);
	}

	[Fact]
	public async Task Bridge_widens_an_omitted_methods_htmx_route_for_query_without_antiforgery()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.ReportComponent",
				"/reports/{ReportId:int}",
				"report.policy",
				ExplicitMethods: false));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Query,
			"PackageConsumer.ReportComponent.QueryReport",
			usesStockRoute: false);

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest,
			[generatedAction]);

		var endpoint = Assert.Single(GetGeneratedEndpoints(app));
		Assert.Equal(
			[HttpMethods.Get, HttpMethods.Query],
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
		var action = Assert.Single(endpoint.Metadata.GetOrderedMetadata<HtmxorComponentActionDescriptor>());
		Assert.Equal(HttpMethods.Query, action.HttpMethod);
		Assert.Null(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>());
	}

	[Fact]
	public async Task Bridge_binds_an_action_allowed_by_explicit_htmx_route_methods()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.ReportComponent",
				"/reports/{ReportId:int}",
				"report.policy",
				Methods: [HttpMethods.Get, HttpMethods.Patch]));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Patch,
			"PackageConsumer.ReportComponent.PatchReport",
			usesStockRoute: false);

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest,
			[generatedAction]);

		var endpoint = Assert.Single(GetGeneratedEndpoints(app));
		Assert.Equal(
			[HttpMethods.Get, HttpMethods.Patch],
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
		var action = Assert.Single(endpoint.Metadata.GetOrderedMetadata<HtmxorComponentActionDescriptor>());
		Assert.Equal(HttpMethods.Patch, action.HttpMethod);
		Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation);
	}

	[Fact]
	public async Task Bridge_binds_query_allowed_by_explicit_htmx_route_methods_without_antiforgery()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.ReportComponent",
				"/reports/{ReportId:int}",
				"report.policy",
				Methods: [HttpMethods.Get, HttpMethods.Query]));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Query,
			"PackageConsumer.ReportComponent.QueryReport",
			usesStockRoute: false);

		componentBuilder.AddHtmxorAttributedComponentEndpoints(
			group,
			fixture.Assembly,
			fixture.Manifest,
			[generatedAction]);

		var endpoint = Assert.Single(GetGeneratedEndpoints(app));
		Assert.Equal(
			[HttpMethods.Get, HttpMethods.Query],
			endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods);
		Assert.Null(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>());
	}

	[Fact]
	public void Build_keeps_omitted_methods_get_only_when_public_defaults_are_mutated()
	{
		foreach (var injectedMethod in new[] { HttpMethods.Post, "TRACE" })
		{
			var publicDefaults = HtmxRouteAttribute.DefaultHttpMethods;
			var originalDefaults = publicDefaults.ToArray();
			try
			{
				publicDefaults[0] = injectedMethod;
				var fixture = DynamicComponentAssembly.Create(
					new ComponentDefinition(
						"PackageConsumer.ReportComponent",
						"/reports/{ReportId:int}",
						"report.policy",
						ExplicitMethods: false));

				var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(fixture.Assembly, fixture.Manifest));

				AssertDescriptor(
					descriptor,
					fixture.Types[0],
					"/reports/{ReportId:int}",
					"report.policy");
				Assert.Equal(new[] { HttpMethods.Get }, new HtmxRouteAttribute("/control").Methods);
			}
			finally
			{
				Array.Copy(originalDefaults, publicDefaults, originalDefaults.Length);
			}
		}
	}

	[Fact]
	public async Task Unsafe_generated_route_preserves_effective_antiforgery_opt_out()
	{
		await using var app = CreateApplication(out var group, out _, out _);
		var descriptor = new HtmxorComponentRouteDescriptor(
			typeof(global::Htmxor.TestApp.App),
			"/unsafe",
			[
				new TestAntiforgeryMetadata(true),
				new TestAntiforgeryMetadata(false),
			],
			[HttpMethods.Get, HttpMethods.Delete]);

		group.MapHtmxorComponentEndpoint(descriptor, []);

		var endpoint = Assert.Single(GetGeneratedEndpoints(app));
		Assert.False(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation);
	}

	[Fact]
	public async Task Bridge_fails_closed_before_mapping_when_explicit_methods_conflict_with_a_binding()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.ReportComponent.PutReport",
			usesStockRoute: false);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		Assert.Contains("explicit HtmxRoute.Methods is authoritative", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_a_generated_action_component_has_two_stock_routes()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition(
				"PackageConsumer.HtmxReportComponent",
				"/reports/{ReportId:int}",
				"report.policy"),
			new ComponentDefinition(
				"PackageConsumer.StockReportComponent",
				"/unused",
				"stock.policy",
				HasHtmxRoute: false,
				StockRoutes:
				[
					"/stock-reports/{ReportId:int}",
					"/alternate-reports/{ReportId:int}",
				]));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[1],
			HttpMethods.Put,
			"PackageConsumer.StockReportComponent.PutReport",
			usesStockRoute: true);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		Assert.Contains("requires exactly one compiled stock route", exception.Message, StringComparison.Ordinal);
		Assert.Contains("PackageConsumer.StockReportComponent", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_a_generated_action_uses_an_unsupported_method()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var generatedAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Trace,
			"PackageConsumer.ReportComponent.TraceReport",
			usesStockRoute: false);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[generatedAction]));

		Assert.Contains("uses unsupported method", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	[Fact]
	public async Task Bridge_maps_nothing_when_a_component_has_two_generated_actions_for_one_method()
	{
		var fixture = DynamicComponentAssembly.Create(
			new ComponentDefinition("PackageConsumer.ReportComponent", "/reports/{ReportId:int}", "report.policy"));
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);
		var firstAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.ReportComponent.PutReport",
			usesStockRoute: false);
		var secondAction = new HtmxorGeneratedComponentAction(
			fixture.Types[0],
			HttpMethods.Put,
			"PackageConsumer.ReportComponent.PutReportAgain",
			usesStockRoute: false);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest,
				[firstAction, secondAction]));

		Assert.Contains("declares more than one generated PUT action", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	private static void AssertDescriptor(
		HtmxorComponentRouteDescriptor descriptor,
		Type componentType,
		string route,
		string policy)
	{
		Assert.Same(componentType, descriptor.ComponentType);
		Assert.Equal(route, descriptor.NormalizedRoute);
		Assert.Equal([HttpMethods.Get], descriptor.HttpMethods);
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
		var routeMetadata = endpoint.Metadata.GetRequiredMetadata<HtmxRouteAttribute>();
		Assert.Same(routeMetadata, endpoint.Metadata.GetRequiredMetadata<EndpointMetadata>().HxRoute);
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

	private static WebApplication CreateRequestApplication(out RouteGroupBuilder group)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddRazorComponents().AddHtmxor();
		var app = builder.Build();
		group = app.MapGroup("/catalog");
		group.MapRazorComponents<global::Htmxor.TestApp.App>();
		return app;
	}

	private static async Task AssertInvalidRouteDeclarationAsync(
		ComponentDefinition definition,
		string expectedArgument)
	{
		var fixture = DynamicComponentAssembly.Create(definition);
		await using var app = CreateApplication(out var group, out var componentBuilder, out _);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			componentBuilder.AddHtmxorAttributedComponentEndpoints(
				group,
				fixture.Assembly,
				fixture.Manifest));

		Assert.Contains($"HtmxRoute named argument '{expectedArgument}'", exception.Message, StringComparison.Ordinal);
		Assert.Empty(GetGeneratedEndpoints(app));
	}

	private static HttpRequestMessage CreateDirectRequest(
		string path,
		string currentUrl,
		string target)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.TryAddWithoutValidation(Htmxor.Http.HtmxRequestHeaderNames.HtmxRequest, "true");
		request.Headers.TryAddWithoutValidation(Htmxor.Http.HtmxRequestHeaderNames.RequestType, "partial");
		request.Headers.TryAddWithoutValidation(Htmxor.Http.HtmxRequestHeaderNames.CurrentUrl, currentUrl);
		request.Headers.TryAddWithoutValidation(Htmxor.Http.HtmxRequestHeaderNames.Target, target);
		return request;
	}

	private static RouteEndpoint[] GetGeneratedEndpoints(WebApplication app)
		=> ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(static dataSource => dataSource.Endpoints)
			.OfType<RouteEndpoint>()
			.Where(static endpoint => endpoint.Metadata.GetMetadata<HtmxorDirectEndpointMetadata>() is not null)
			.ToArray();

	private sealed record GroupMetadata(string Value);

	private sealed record TestAntiforgeryMetadata(bool RequiresValidation) : IAntiforgeryMetadata;

	private sealed class RouteProbeComponent : ComponentBase
	{
		protected override void BuildRenderTree(
			Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
			=> builder.AddMarkupContent(0, "<div id=\"result\">route-probe</div>");
	}

	private sealed record ComponentDefinition(
		string TypeName,
		string Route,
		string Policy,
		bool UseNamedPolicy = false,
		bool HasAdditionalRouteFilter = false,
		bool HasThrowingMetadata = false,
		bool HasDisableHtmxDirectRouting = false,
		bool HasHtmxRoute = true,
		IReadOnlyList<string>? StockRoutes = null,
		bool ExplicitMethods = true,
		IReadOnlyList<string>? Methods = null,
		string? CurrentUrl = null,
		string? Target = null,
		IReadOnlyList<string>? Targets = null,
		Action<TypeBuilder>? Authorization = null,
		Action<TypeBuilder>? BaseAuthorization = null);

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
			var baseType = typeof(ComponentBase);
			if (definition.BaseAuthorization is not null)
			{
				var baseBuilder = module.DefineType(
					definition.TypeName + "Base",
					TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Class,
					typeof(ComponentBase));
				baseBuilder.DefineDefaultConstructor(MethodAttributes.Family);
				definition.BaseAuthorization(baseBuilder);
				baseType = baseBuilder.CreateType()!;
			}

			var type = module.DefineType(
				definition.TypeName,
				TypeAttributes.Public | TypeAttributes.Class,
				baseType);
			type.DefineDefaultConstructor(MethodAttributes.Public);
			if (definition.HasHtmxRoute)
			{
				AddRoute(type, definition);
			}
			AddStockRoutes(type, definition.StockRoutes);
			if (definition.Authorization is not null)
			{
				definition.Authorization(type);
			}
			else
			{
				AddAuthorization(type, definition);
			}

			if (definition.HasThrowingMetadata)
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(ThrowingMetadataAttribute).GetConstructor(Type.EmptyTypes)!,
					[]));
			}

			if (definition.HasDisableHtmxDirectRouting)
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(DisableHtmxDirectRoutingAttribute).GetConstructor(Type.EmptyTypes)!,
					[]));
			}

			return type.CreateType()!;
		}

		private static void AddStockRoutes(
			TypeBuilder type,
			IReadOnlyList<string>? stockRoutes)
		{
			foreach (var stockRoute in stockRoutes ?? [])
			{
				type.SetCustomAttribute(new CustomAttributeBuilder(
					typeof(RouteAttribute).GetConstructor([typeof(string)])!,
					[stockRoute]));
			}
		}

		private static void AddRoute(TypeBuilder type, ComponentDefinition definition)
		{
			var properties = new List<PropertyInfo>();
			var values = new List<object>();
			if (definition.ExplicitMethods)
			{
				properties.Add(typeof(HtmxRouteAttribute).GetProperty(nameof(HtmxRouteAttribute.Methods))!);
				values.Add((definition.Methods ?? [HttpMethods.Get]).ToArray());
			}
			if (definition.HasAdditionalRouteFilter)
			{
				properties.Add(typeof(HtmxRouteAttribute).GetProperty(nameof(HtmxRouteAttribute.CurrentUrl))!);
				values.Add(definition.CurrentUrl ?? "/orders");
				properties.Add(typeof(HtmxRouteAttribute).GetProperty(nameof(HtmxRouteAttribute.Target))!);
				values.Add(definition.Target ?? "section#result");
				properties.Add(typeof(HtmxRouteAttribute).GetProperty(nameof(HtmxRouteAttribute.Targets))!);
				values.Add((definition.Targets ?? ["section#result", "div#fallback"]).ToArray());
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

	/// <summary>
	/// An authorization attribute that is stock metadata read by the stock authorization
	/// middleware, not an <see cref="IAuthorizationHandler"/> or requirement. It deliberately does
	/// not derive from <see cref="AuthorizeAttribute"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class CustomAuthorizeDataAttribute : Attribute, IAuthorizeData
	{
		public string? Policy { get; set; }

		public string? Roles { get; set; }

		public string? AuthenticationSchemes { get; set; }
	}
}
