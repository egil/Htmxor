using Microsoft.AspNetCore.Http;

namespace Htmxor;

public sealed class HtmxRouteAttributeTests
{
	[Fact]
	public void Equality_and_hash_set_treat_route_fields_and_element_tags_with_their_declared_case_rules()
	{
		var first = new HtmxRouteAttribute("/Items")
		{
			Methods = [HttpMethods.Get],
			CurrentUrl = "/source",
			Target = "div#result",
			Targets = ["section", "span#secondary"],
		};
		var second = new HtmxRouteAttribute("/items")
		{
			Methods = ["get"],
			CurrentUrl = "/source",
			Target = "DIV#result",
			Targets = ["SECTION", "SPAN#secondary"],
		};

		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Single(new HashSet<HtmxRouteAttribute> { first, second });
	}

	[Fact]
	public void Equality_and_hash_set_keep_case_distinct_current_url_path_and_query_separate()
	{
		var lower = new HtmxRouteAttribute("/items") { CurrentUrl = "/source?mode=read" };
		var upper = new HtmxRouteAttribute("/items") { CurrentUrl = "/Source?mode=READ" };

		Assert.NotEqual(lower, upper);
		Assert.Equal(2, new HashSet<HtmxRouteAttribute> { lower, upper }.Count);
	}

	[Fact]
	public void Equality_and_hash_set_use_runtime_current_url_equivalence_for_absolute_urls()
	{
		var defaultPort = new HtmxRouteAttribute("/items")
		{
			CurrentUrl = "HTTPS://LOCALHOST/foo#first",
		};
		var explicitPort = new HtmxRouteAttribute("/items")
		{
			CurrentUrl = "https://localhost:443/foo#second",
		};

		Assert.Equal(defaultPort, explicitPort);
		Assert.Equal(defaultPort.GetHashCode(), explicitPort.GetHashCode());
		Assert.Single(new HashSet<HtmxRouteAttribute> { defaultPort, explicitPort });
	}

	[Fact]
	public void Equality_and_hash_set_ignore_fragments_for_relative_current_url_declarations()
	{
		var firstFragment = new HtmxRouteAttribute("/items")
		{
			CurrentUrl = "/source#first",
		};
		var secondFragment = new HtmxRouteAttribute("/items")
		{
			CurrentUrl = "/source#second",
		};

		Assert.Equal(firstFragment, secondFragment);
		Assert.Equal(firstFragment.GetHashCode(), secondFragment.GetHashCode());
		Assert.Single(new HashSet<HtmxRouteAttribute> { firstFragment, secondFragment });
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
			CurrentUrl = "/source",
			Target = "div#result",
		};

		Assert.False(absent.Equals(present));
		Assert.False(present.Equals(absent));
		Assert.Equal(2, new HashSet<HtmxRouteAttribute> { absent, present }.Count);
	}

	[Fact]
	public void Null_targets_are_normalized_to_empty()
	{
		var route = new HtmxRouteAttribute("/items")
		{
			Targets = null!,
		};

		Assert.Empty(route.Targets);
	}
}
