namespace Htmxor;

public class RequestRoutingTest : TestAppTestBase
{
	public RequestRoutingTest(TestAppFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task NormalRequest()
	{
		await Host.Scenario(s =>
		{
			s.Get.Url("/normal-and-hx");
			s.ContentShouldBeHtml(FullPageContent("<h1>Hello, world!</h1>", "Home"));
		});
	}

	[Fact]
	public async Task HxRequest()
	{
		await Host.Scenario(s =>
		{
			s.Get.Url("/normal-and-hx");
			s.WithHxHeaders();
			s.ContentShouldBeHtml("""
                <h1>Hello, world!</h1>
                """);
		});
	}
}
