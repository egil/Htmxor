#if NET11_0_OR_GREATER
using System.Net;
using Htmxor;
using Htmxor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Htmxor.AspNetCore10;

public sealed class Issue219CacheRepresentationTests
{
	[Fact]
	public async Task An_htmx_request_does_not_reuse_an_entry_stored_for_the_ordinary_representation()
	{
		await using var app = await Issue219CacheSafetyTests.StartAsync<Issue219BranchingPage>(htmxor: true);
		using var client = app.GetTestClient();

		// The ordinary request renders no fragment, so nothing abandons and the entry is stored.
		using var ordinary = await client.GetAsync("/issue-219/branching");
		Assert.Equal(HttpStatusCode.OK, ordinary.StatusCode);

		// The htmx request must miss that entry and construct the fragment it selects. Sharing it is what this
		// case guards against, not what it expects.
		using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-219/branching");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var selected = await client.SendAsync(request);

		// One URL, two representations. Sharing an entry served the ordinary body to the htmx request and then
		// failed it, because the named fragment the cached markup was stored around is never reconstructed.
		Assert.Equal(HttpStatusCode.OK, selected.StatusCode);
		Assert.Contains("data-branch", await selected.Content.ReadAsStringAsync(), StringComparison.Ordinal);
	}
}

[Route("/issue-219/branching")]
public sealed class Issue219BranchingPage : ComponentBase
{
	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

	protected override void OnInitialized()
	{
		if (HttpContext.GetHtmxContext().Request.IsHtmxRequest)
		{
			HttpContext.GetHtmxContext().Response.SelectFragment("branch");
		}
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var htmx = HttpContext.GetHtmxContext().Request.IsHtmxRequest;
		builder.OpenComponent<CacheView>(0);
		builder.AddAttribute(1, nameof(CacheView.CacheKey), "issue-219-branching");
		builder.AddAttribute(2, nameof(CacheView.ChildContent), (RenderFragment)(cached =>
		{
			if (!htmx)
			{
				cached.OpenElement(0, "p");
				cached.AddContent(1, "ordinary");
				cached.CloseElement();
				return;
			}

			cached.OpenComponent<HtmxFragment>(2);
			cached.AddAttribute(3, nameof(HtmxFragment.Name), "branch");
			cached.AddAttribute(4, nameof(HtmxFragment.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenElement(0, "p");
				inner.AddAttribute(1, "data-branch", "true");
				inner.CloseElement();
			}));
			cached.CloseComponent();
		}));
		builder.CloseComponent();
	}
}
#endif
