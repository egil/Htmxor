using Bunit;
using Htmxor.Components;

namespace Htmxor.Configuration;

public class HtmxHeadOutletTest : TestContext
{
	[Fact]
	public void Head_outlet_emits_no_Htmxor_owned_htmx_runtime_or_configuration()
	{
		var cut = RenderComponent<HtmxHeadOutlet>();

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
}
