using Bunit;
using Htmxor.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Configuration;

public class HtmxHeadOutletTest : BunitContext
{
	[Fact]
	public void Head_outlet_emits_no_Htmxor_owned_htmx_runtime_or_configuration()
	{
		var cut = Render<HtmxHeadOutlet>();

		cut.FindAll("meta[name='htmx-config']").Should().BeEmpty();
		cut.FindAll("script[src*='/htmx/']").Should().BeEmpty();
		cut.FindAll("script[src='_content/Htmxor/htmxor.js']").Should().ContainSingle();
	}

	[Fact]
	public void Head_outlet_has_no_embedded_htmx_option()
	{
		typeof(HtmxHeadOutlet)
			.GetProperty("UseEmbeddedHtmx")
			.Should()
			.BeNull();
	}

	[Fact]
	public void Public_registration_exposes_no_Htmxor_owned_client_configuration()
	{
		var registrationMethods = typeof(HtmxorApplicationBuilderExtensions)
			.GetMethods()
			.Where(static method => method.IsPublic && method.IsStatic)
			.ToArray();

		var addHtmxor = registrationMethods.Should()
			.ContainSingle(static method => method.Name == nameof(HtmxorApplicationBuilderExtensions.AddHtmxor))
			.Which;
		addHtmxor.GetParameters().Should().ContainSingle();
		registrationMethods.Should().NotContain(static method => method.Name == "AddHtmx");
		typeof(HtmxHeadOutlet).Assembly.GetType("Htmxor.HtmxConfig").Should().BeNull();
	}

	[Fact]
	public void Public_registration_exposes_no_readable_token_cookie_middleware()
	{
		var assembly = typeof(HtmxHeadOutlet).Assembly;

		assembly.GetType("Microsoft.AspNetCore.Builder.HtmxorAntiforgeryApplicationBuilderExtensions")
			.Should()
			.BeNull();
		assembly.GetType("Htmxor.Antiforgery.HtmxorAntiforgeryMiddleware")
			.Should()
			.BeNull();
	}
}
