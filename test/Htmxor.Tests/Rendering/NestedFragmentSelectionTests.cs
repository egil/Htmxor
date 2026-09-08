using Htmxor.Components;
using Htmxor.Endpoints;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Htmxor.Rendering;

public sealed class NestedFragmentSelectionTests
{
	private const string Child = "<b>child</b>";
	private const string Parent = "<section id=\"parent\"><i>before</i>" + Child + "<i>after</i></section>";
	private const string Whole = "<main>" + Parent + "<aside>sibling</aside><u>unnamed</u></main>";

	[Theory]
	[InlineData("Child", Child)]
	[InlineData("Parent", Parent)]
	[InlineData("Sibling,Child", "<aside>sibling</aside>" + Child)]
	[InlineData("Child,Sibling", Child + "<aside>sibling</aside>")]
	[InlineData("", Whole)]
	public Task Nested_selection_serializes_only_selected_boundaries_after_normal_render(string names, string expected) =>
		WithRenderer(async (renderer, context) =>
		{
			var rendered = await renderer.RenderEndpointComponentAsync(typeof(NestedRoot), ParameterView.Empty);
			rendered.ToHtmlString().Should().Be(Whole);
			if (names.Length > 0) context.Response.SelectFragments(names.Split(','));
			using var writer = new StringWriter();
			renderer.WriteResponseHtml(rendered, context, writer);
			writer.ToString().Should().Be(expected);
		});

	[Theory]
	[InlineData("Parent", "Child")]
	[InlineData("Child", "Parent")]
	[InlineData("Child", "Child")]
	[InlineData("Child", "Missing")]
	[InlineData("Child", "child")]
	public Task Invalid_sets_fail_before_writing_any_selected_html(string first, string second) =>
		WithRenderer(async (renderer, context) =>
		{
			var rendered = await renderer.RenderEndpointComponentAsync(typeof(NestedRoot), ParameterView.Empty);
			rendered.ToHtmlString().Should().Be(Whole);
			using var writer = new StringWriter();
			Action write = () =>
			{
				context.Response.SelectFragments(first, second);
				renderer.WriteResponseHtml(rendered, context, writer);
			};
			var failure = Record.Exception(write);
			failure.Should().NotBeNull("invalid selection must fail before output, but received {0}", writer.ToString());
			failure!.Message.Should().NotBeNullOrWhiteSpace();
			writer.ToString().Should().BeEmpty();
		});

	public static TheoryData<string> InvalidNames => new()
	{
		"", " ", "1First", "_First", "-First", "First.Second", "First Second", "Ångstrom", "Aé", new string('A', 65),
	};

	[Theory]
	[MemberData(nameof(InvalidNames))]
	public Task Malformed_selected_names_fail_even_when_a_declaration_matches(string name) =>
		AssertRejectedName(name, true);

	[Theory]
	[MemberData(nameof(InvalidNames))]
	public Task Malformed_declarations_fail_before_whole_output(string name) =>
		AssertRejectedName(name, false);

	[Theory]
	[InlineData(null)]
	[InlineData("Child")]
	[InlineData("Sibling")]
	public Task Duplicate_declarations_fail_even_when_not_selected(string? selection) =>
		AssertRejectedName("Child", selection is not null, selection);

	[Theory]
	[InlineData("child")]
	[InlineData("A")]
	[InlineData("zZ09-_")]
	[InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
	public Task Valid_identifier_boundaries_remain_selectable(string name) =>
		WithRenderer(async (renderer, context) =>
		{
			var rendered = await renderer.RenderEndpointComponentAsync(typeof(NestedRoot), ExtraName(name));
			context.Response.SelectFragment(name);
			using var writer = new StringWriter();
			renderer.WriteResponseHtml(rendered, context, writer);
			writer.ToString().Should().Be("<u>unnamed</u>");
		});

	private static Task AssertRejectedName(string name, bool select, string? selection = null) =>
		WithRenderer(async (renderer, context) =>
		{
			using var writer = new StringWriter();
			Func<Task> write = async () =>
			{
				if (select) context.Response.SelectFragment(selection ?? name);
				var rendered = await renderer.RenderEndpointComponentAsync(typeof(NestedRoot), ExtraName(name));
				renderer.WriteResponseHtml(rendered, context, writer);
			};
			(await write.Should().ThrowAsync<Exception>()).Which.Message.Should().NotBeNullOrWhiteSpace();
			writer.ToString().Should().BeEmpty();
		});

	private static ParameterView ExtraName(string name) => ParameterView.FromDictionary(
		new Dictionary<string, object?> { [nameof(NestedRoot.ExtraName)] = name });

	private static async Task WithRenderer(Func<HtmxorEndpointCandidateRenderer, HtmxContext, Task> test)
	{
		var http = new DefaultHttpContext();
		http.Request.Headers["HX-Request"] = "true";
		http.Request.Headers["HX-Request-Type"] = "partial";
		var context = http.GetHtmxContext();
		context.UsesCompletedFragmentSelection = true;
		await using var services = new ServiceCollection().AddSingleton(context).BuildServiceProvider();
		await using var renderer = new HtmxorEndpointCandidateRenderer(services, NullLoggerFactory.Instance);
		await renderer.Dispatcher.InvokeAsync(() => test(renderer, context));
	}

	private sealed class NestedRoot : ComponentBase
	{
		[Parameter] public string? ExtraName { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "main");
			builder.OpenComponent<HtmxFragment>(1);
			builder.AddComponentParameter(2, nameof(HtmxFragment.Name), "Parent");
			builder.AddComponentParameter(3, nameof(HtmxFragment.Element), "section");
			builder.AddComponentParameter(4, nameof(HtmxFragment.Id), "parent");
			builder.AddComponentParameter(5, nameof(HtmxFragment.ChildContent), (RenderFragment)(content =>
			{
				content.AddMarkupContent(0, "<i>before</i>");
				content.OpenComponent<NestedChild>(1);
				content.CloseComponent();
				content.AddMarkupContent(2, "<i>after</i>");
			}));
			builder.CloseComponent();
			builder.OpenComponent<HtmxFragment>(6);
			builder.AddComponentParameter(7, nameof(HtmxFragment.Name), "Sibling");
			builder.AddComponentParameter(8, nameof(HtmxFragment.ChildContent), (RenderFragment)(content => content.AddMarkupContent(0, "<aside>sibling</aside>")));
			builder.CloseComponent();
			builder.OpenComponent<HtmxFragment>(9);
			builder.AddComponentParameter(10, nameof(HtmxFragment.Name), ExtraName);
			builder.AddComponentParameter(11, nameof(HtmxFragment.ChildContent), (RenderFragment)(content => content.AddMarkupContent(0, "<u>unnamed</u>")));
			builder.CloseComponent();
			builder.CloseElement();
		}
	}

	private sealed class NestedChild : ComponentBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<HtmxFragment>(0);
			builder.AddComponentParameter(1, nameof(HtmxFragment.Name), "Child");
			builder.AddComponentParameter(2, nameof(HtmxFragment.ChildContent), (RenderFragment)(content => content.AddMarkupContent(0, Child)));
			builder.CloseComponent();
		}
	}
}
