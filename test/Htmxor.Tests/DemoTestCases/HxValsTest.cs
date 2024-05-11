using System.Net;
using Htmxor.TestAssets.Alba;

namespace Htmxor.DemoTestCases;

public class HxValsTest : TestAppTestBase
{
	public HxValsTest(TestAppFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task HxVals_correctly_escaped_by_renderer()
	{
		await Host.Scenario(s =>
		{
			s.Get.Url("/hx-vals-escaped");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldBeHtml(FullPageContent($$"""
                <button hx-post="/hx-vals-escaped"
                        hx-vals='{"myVal": "My Value"}'
                        type="button">
                    VALS
                </button>
                """));
		});
	}
}
