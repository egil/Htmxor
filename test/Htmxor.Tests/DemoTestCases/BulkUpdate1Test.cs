using System.Net;
using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples.BulkUpdate1;
using Htmxor.TestAssets.Alba;

namespace Htmxor.DemoTestCases;

public class BulkUpdate1Test : TestAppTestBase
{
	public BulkUpdate1Test(TestAppFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task Hx_post_returns_the_updated_form()
	{
		var users = Enumerable.Range(1, 10)
			.Select(num => DataStore.Store(new ActivatableUser
			{
				Id = Guid.NewGuid(),
				Name = $"User {num}",
				Active = false,
				Email = $"user{num}@example.com",
			})).ToArray();

		await Host.Scenario(s =>
		{
			s.Post.Url("/bulk-update-1");
			s.WithFormData(("Active", users[1].Id.ToString()), ("Active", users[3].Id.ToString()));
			s.WithAntiforgeryTokensFrom(Host);
			s.WithHxHeaders(
				target: "form#checked-contacts",
				source: "form#checked-contacts",
				currentUrl: $"{Host.Server.BaseAddress}bulk-update-1");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveSingleRootElement("form#checked-contacts");
			s.ContentShouldHaveElementsEqualTo("#toast", $"""
                <span id="toast" aria-live="polite">Activated 2 and deactivated 0 users.</span>
                """);
		});
	}
}
