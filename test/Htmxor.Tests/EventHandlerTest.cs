using System.Net;

namespace Htmxor;

public class EventHandlerTest : TestAppTestBase
{
	public EventHandlerTest(TestAppFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task Get_method_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Get.Url($"/EventHandlers");
			s.WithHxHeaders(eventHandlerId: "C817B229");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnGet</div>");
		});

	[Fact]
	public async Task Get_lambda_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Get.Url($"/EventHandlers");
			s.WithHxHeaders(eventHandlerId: "2300F6BC");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnGetInline</div>");
		});

	[Fact]
	public async Task Post_method_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Post.Url($"/EventHandlers");
			s.WithHxHeaders(eventHandlerId: "1D4E98D3");
			s.WithAntiforgeryTokensFrom(Host);
			s.WithRequestHeader("Content-Type", "application/x-www-form-urlencoded");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnPost</div>");
		});

	[Fact]
	public async Task Put_method_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Put.Url($"/EventHandlers");
			s.WithHxHeaders(eventHandlerId: "04C1C73A");
			s.WithAntiforgeryTokensFrom(Host);
			s.WithRequestHeader("Content-Type", "application/x-www-form-urlencoded");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnPut</div>");
		});

	[Fact]
	public async Task Patch_method_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Patch.Url($"/EventHandlers");
			s.WithHxHeaders(eventHandlerId: "19B81537");
			s.WithAntiforgeryTokensFrom(Host);
			s.WithRequestHeader("Content-Type", "application/x-www-form-urlencoded");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnPatch</div>");
		});

	[Fact]
	public async Task Delete_method_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Patch.Url($"/EventHandlers");
			s.WithHxHeaders(eventHandlerId: "8D1ACB70");
			s.WithAntiforgeryTokensFrom(Host);
			s.WithRequestHeader("Content-Type", "application/x-www-form-urlencoded");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnDelete</div>");
		});

	[Fact]
	public async Task Submit_form_htmxor_event_handler()
		=> await Host.Scenario(s =>
		{
			s.Post.Url($"/EventHandlers");
			s.WithFormData(("_handler", "classic-form"));
			s.WithAntiforgeryTokensFrom(Host);
			s.WithRequestHeader("Content-Type", "application/x-www-form-urlencoded");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo("#handler", @"<div id=""handler"">OnSubmit</div>");
		});
}

