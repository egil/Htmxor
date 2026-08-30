using Microsoft.AspNetCore.Http;

namespace Htmxor;

public sealed class HtmxRouteAttributeTests
{
	[Fact]
	public void Equality_and_hash_set_treat_route_fields_and_element_tags_case_insensitively()
	{
		var first = new HtmxRouteAttribute("/Items")
		{
			Methods = [HttpMethods.Get],
			CurrentURL = "/Source",
			Target = "div#result",
			Targets = ["section", "span#secondary"],
		};
		var second = new HtmxRouteAttribute("/items")
		{
			Methods = ["get"],
			CurrentURL = "/source",
			Target = "DIV#result",
			Targets = ["SECTION", "SPAN#secondary"],
		};

		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Single(new HashSet<HtmxRouteAttribute> { first, second });
	}

	[Fact]
	public void Equality_and_hash_set_keep_case_distinct_element_ids_separate()
	{
		var lower = new HtmxRouteAttribute("/items") { Target = "div#result" };
		var upper = new HtmxRouteAttribute("/items") { Target = "DIV#Result" };

		Assert.NotEqual(lower, upper);
		Assert.Equal(2, new HashSet<HtmxRouteAttribute> { lower, upper }.Count);
	}

	[Fact]
	public void Equality_is_symmetric_when_optional_values_differ()
	{
		var absent = new HtmxRouteAttribute("/items");
		var present = new HtmxRouteAttribute("/items")
		{
			CurrentURL = "/source",
			Target = "div#result",
		};

		Assert.False(absent.Equals(present));
		Assert.False(present.Equals(absent));
		Assert.Equal(2, new HashSet<HtmxRouteAttribute> { absent, present }.Count);
	}
}
