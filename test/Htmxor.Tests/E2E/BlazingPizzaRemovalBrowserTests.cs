using Htmxor.Samples.Tests;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

public sealed class BlazingPizzaRemovalBrowserTests(ITestOutputHelper output)
{
	[Fact]
	[Trait("Category", "Browser")]
	public async Task Clicking_remove_preserves_the_unselected_pizza()
	{
		var directory = Path.Combine(AppContext.BaseDirectory, "pizza-removal-browser", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			await using var host = await SampleKestrelHost.StartAsync("samples/BlazingPizza/BlazingPizza.csproj",
				new Dictionary<string, string> { ["ConnectionStrings__DefaultConnection"] = $"Data Source={Path.Combine(directory, "pizza.db")}" });
			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
			output.WriteLine("Chromium {0}; .NET {1}; OS {2}", browser.Version, Environment.Version, System.Runtime.InteropServices.RuntimeInformation.OSDescription);
			await using var context = await browser.NewContextAsync();
			var page = await context.NewPageAsync();
			await page.GotoAsync(host.Address.ToString());
			await page.WaitForFunctionAsync("typeof htmx !== 'undefined'");
			await PopulateOrderAsync(context, page, host.Address);
			var pizzas = page.Locator("#sidebar .cart-item");
			await Assertions.Expect(pizzas).ToHaveCountAsync(2);
			await Assertions.Expect(pizzas).ToContainTextAsync(["12\"", "16\""]);

			var response = await page.RunAndWaitForResponseAsync(
				() => page.GetByRole(AriaRole.Button, new() { Name = "Remove pizza", Exact = true }).First.ClickAsync(),
				response => response.Request.Method == "PUT" && new Uri(response.Url).AbsolutePath == "/");
			var fields = QueryHelpers.ParseQuery(response.Request.PostData ?? "");
			output.WriteLine("Clicked removal: HTTP {0}; RemovePizzaIndex={1}; antiforgery form field present={2}",
				response.Status, fields.TryGetValue("RemovePizzaIndex", out var index) ? index.ToString() : "<missing>", fields.ContainsKey("__RequestVerificationToken"));
			Assert.Equal(200, response.Status);
			await Assertions.Expect(pizzas).ToHaveCountAsync(1);
			await Assertions.Expect(pizzas).ToContainTextAsync("16\"");
			await Assertions.Expect(pizzas).Not.ToContainTextAsync("12\"");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static async Task PopulateOrderAsync(IBrowserContext context, IPage page, Uri address)
	{
		// Seed through the existing order form HTTP boundary; the unrelated add-pizza POST is outside this regression.
		var token = await page.Locator("#sidebar input[name='__RequestVerificationToken']").InputValueAsync();
		var form = context.APIRequest.CreateFormData();
		form.Set("__RequestVerificationToken", token);
		form.Set("OrderForm.Pizzas[0].SpecialId", "1");
		form.Set("OrderForm.Pizzas[0].Size", "12");
		form.Set("OrderForm.Pizzas[1].SpecialId", "1");
		form.Set("OrderForm.Pizzas[1].Size", "16");
		form.Set("OrderForm.Pizzas[2].SpecialId", "1");
		form.Set("OrderForm.Pizzas[2].Size", "14");
		form.Set("RemovePizzaIndex", "2");
		await using var response = await context.APIRequest.PutAsync(address.ToString(), new()
		{
			Headers = new Dictionary<string, string> { ["HX-Request"] = "true", ["HX-Request-Type"] = "partial" },
			Form = form,
		});
		Assert.Equal(200, response.Status);
		await page.EvaluateAsync("html => { document.querySelector('#sidebar').outerHTML = html; htmx.process(document.querySelector('#sidebar')); }", await response.TextAsync());
	}
}
