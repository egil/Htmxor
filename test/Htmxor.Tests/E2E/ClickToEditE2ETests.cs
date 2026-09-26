using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples;
using Htmxor.TestAssets.Blazewright;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

public class ClickToEditE2ETests : PageTest
{
	private readonly ITestOutputHelper outputHelper;

	public ClickToEditE2ETests(ITestOutputHelper outputHelper, PlaywrightFixture fixture) : base(fixture)
	{
		this.outputHelper = outputHelper;
	}

	[Fact]
	public Task Click_to_edit() =>
		RunWithRetryAsync(outputHelper, async page =>
		{
			// Fresh per attempt (https://github.com/egil/Htmxor/issues/252): a retry re-stores its
			// own contact rather than depending on a prior attempt's still being there.
			var contact = new Contact
			{
				Id = Guid.NewGuid(),
				FirstName = "Joe",
				LastName = "Blow",
				Email = "joe@blow.com",
			};
			DataStore.Store(contact);

			page.Request += (_, request) =>
				outputHelper.WriteLine($">> {request.ResourceType} {request.Method} {request.Url} {request.Headers.Aggregate("", (r, x) => $"{r}, {x.Key}:{x.Value}")}");

			await page.GotoAsync($"/click-to-edit-1/contact/{contact.Id}");
			await page.GetByRole(AriaRole.Button, new() { Name = "Click To Edit" }).ClickAsync();
			await page.Locator("input[name=\"Contact\\.FirstName\"]").FillAsync("Foo");
			await page.Locator("input[name=\"Contact\\.LastName\"]").FillAsync("Bar");
			await page.Locator("input[name=\"Contact\\.Email\"]").FillAsync("foo@bar.com");
			await page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();
			await Expect(page.Locator("body")).ToContainTextAsync("First Name: Foo Last Name: Bar Email: foo@bar.com Click To Edit");
		});
}
