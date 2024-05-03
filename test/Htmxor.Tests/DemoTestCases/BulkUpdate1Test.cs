using System.Net;
using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples.BulkUpdate1;

namespace Htmxor.DemoTestCases;

public class BulkUpdate1Test : TestAppTestBase
{
	public BulkUpdate1Test(TestAppFixture fixture) : base(fixture)
	{
	}

	[Fact]
	public async Task Hx_post_with_partial_return()
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
			s.WithHxHeaders(target: "toast", trigger: "checked-contacts", currentURL: $"{Host.Server.BaseAddress}bulk-update-1");

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldBeHtml($"""
                <span id="toast" aria-live="polite">Activated 2 and deactivated 0 users.</span>
                """);
		});
	}
}
