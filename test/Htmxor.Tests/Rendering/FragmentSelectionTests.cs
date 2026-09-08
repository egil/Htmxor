using Htmxor.Components;
using Htmxor.Endpoints;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Htmxor.Rendering;

public sealed class FragmentSelectionTests
{
	[Theory]
	[InlineData(false, "single", "<main><b>first</b><i>second</i><aside>sibling</aside></main>")]
	[InlineData(true, "default", "<main><b>first</b><i>second</i><aside>sibling</aside></main>")]
	[InlineData(true, "whole", "<main><b>first</b><i>second</i><aside>sibling</aside></main>")]
	[InlineData(true, "single", "<b>first</b>")]
	[InlineData(true, "multiple", "<i>second</i><b>first</b>")]
	public async Task Completed_render_emits_application_selection_in_caller_order(bool direct, string selection, string expected)
	{
		var http = new DefaultHttpContext();
		if (direct)
		{
			http.Request.Headers["HX-Request"] = "true";
			http.Request.Headers["HX-Request-Type"] = "partial";
		}
		var context = http.GetHtmxContext();
		await using var services = new ServiceCollection().AddSingleton(context).BuildServiceProvider();
		await using var renderer = new HtmxorEndpointCandidateRenderer(services, NullLoggerFactory.Instance);
		await renderer.Dispatcher.InvokeAsync(async () =>
		{
			var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(SelectionRoot.Selection)] = selection });
			var rendered = await renderer.RenderEndpointComponentAsync(typeof(SelectionRoot), parameters);
			using var writer = new StringWriter();
			renderer.WriteResponseHtml(rendered, context, writer);
			writer.ToString().Should().Be(expected);
		});
	}

	[Fact]
	public void Selection_is_read_only_request_local_and_replaced_by_whole_selection()
	{
		var response = new DefaultHttpContext().GetHtmxContext().Response;
		string[] names = ["Second", "First"];
		response.SelectFragments(names);
		names[0] = "Changed";
		response.SelectedFragmentNames.Should().Equal("Second", "First");
		((IList<string>)response.SelectedFragmentNames).IsReadOnly.Should().BeTrue();
		new DefaultHttpContext().GetHtmxContext().Response.SelectedFragmentNames.Should().BeEmpty();
		response.SelectWholeComponent().SelectedFragmentNames.Should().BeEmpty();
	}

	private sealed class SelectionRoot : ComponentBase
	{
		[Inject] public HtmxContext Context { get; set; } = default!;
		[Parameter] public string Selection { get; set; } = "default";

		protected override void OnParametersSet()
		{
			switch (Selection)
			{
				case "single": Context.Response.SelectFragment("First"); break;
				case "multiple": Context.Response.SelectFragments("Second", "First"); break;
				case "whole": Context.Response.SelectFragment("First").SelectWholeComponent(); break;
			}
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "main");
			builder.OpenComponent<HtmxFragment>(1);
			builder.AddComponentParameter(2, nameof(HtmxFragment.Name), "First");
			builder.AddComponentParameter(3, nameof(HtmxFragment.ChildContent), (RenderFragment)(content => content.AddMarkupContent(0, "<b>first</b>")));
			builder.CloseComponent();
			builder.OpenComponent<HtmxFragment>(4);
			builder.AddComponentParameter(5, nameof(HtmxFragment.Name), "Second");
			builder.AddComponentParameter(6, nameof(HtmxFragment.ChildContent), (RenderFragment)(content => content.AddMarkupContent(0, "<i>second</i>")));
			builder.CloseComponent();
			builder.AddMarkupContent(7, "<aside>sibling</aside>");
			builder.CloseElement();
		}
	}
}
