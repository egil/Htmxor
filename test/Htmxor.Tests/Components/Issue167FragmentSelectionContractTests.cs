using Htmxor.Http;

namespace Htmxor.Components;

public sealed class Issue167FragmentSelectionContractTests
{
	[Fact]
	public void HtmxFragment_exposes_a_writable_named_selection_parameter()
	{
		var name = typeof(HtmxFragment).GetProperty("Name");

		Assert.NotNull(name);
		Assert.Equal(typeof(string), name.PropertyType);
		Assert.True(name.CanWrite);
	}

	[Fact]
	public void HtmxFragment_retires_request_and_render_flag_selection_parameters()
	{
		Assert.Null(typeof(HtmxFragment).GetProperty("Match"));
		Assert.Null(typeof(HtmxFragment).GetProperty("RenderDuringStandardRequest"));
	}

	[Fact]
	public void HtmxResponse_exposes_explicit_whole_single_and_ordered_selection()
	{
		var responseType = typeof(HtmxResponse);

		var whole = responseType.GetMethod("SelectWholeComponent", Type.EmptyTypes);
		var single = responseType.GetMethod("SelectFragment", [typeof(string)]);
		var many = responseType.GetMethod("SelectFragments", [typeof(string[])]);

		Assert.NotNull(whole);
		Assert.NotNull(single);
		Assert.NotNull(many);
		Assert.Equal(responseType, whole.ReturnType);
		Assert.Equal(responseType, single.ReturnType);
		Assert.Equal(responseType, many.ReturnType);
	}
}
