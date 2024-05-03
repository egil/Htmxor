using System.Net;
using Htmxor.Http;

namespace Htmxor;

public class EventHandlerTest : TestAppTestBase
{
	public EventHandlerTest(TestAppFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task Get_method()
	{
		await Host.Scenario(s =>
		{
			s.Get.Url($"/custom-event-handler");
			s.WithHxHeaders(target: "post-request");
			s.WithRequestHeader(HtmxRequestHeaderNames.EventHandlerId, "DF4B2CEB");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldBeHtml("""
                <div id="post-request">
                    OnGet
                </div>
                """);
		});
	}
}

