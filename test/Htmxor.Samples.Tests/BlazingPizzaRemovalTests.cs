using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Htmxor.Samples.Tests;

public sealed class BlazingPizzaRemovalTests
{
	[Fact]
	public async Task Removal_sidebar_exposes_working_remove_buttons_for_remaining_pizzas()
	{
		await WithHostAsync(async (host, initialSidebar) =>
		{
			using var initialRequest = RemovalRequest(initialSidebar, [12, 16, 14], 1);
			using var initialResponse = await host.Client.SendAsync(initialRequest);
			Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
			using var initialDocument = new HtmlParser().ParseDocument(await initialResponse.Content.ReadAsStringAsync());
			var sidebar = initialDocument.QuerySelector("#sidebar")!;
			Assert.Collection(sidebar.QuerySelectorAll(".cart-item"),
				item => Assert.Contains("12\"", item.TextContent, StringComparison.Ordinal),
				item => Assert.Contains("14\"", item.TextContent, StringComparison.Ordinal));
			var buttons = sidebar.QuerySelectorAll("button[hx-put]");
			Assert.Equal(2, buttons.Length);
			Assert.Collection(buttons,
				button => AssertRemovalButton(button, "0"),
				button => AssertRemovalButton(button, "1"));

			using var request = CreateRequest(HttpMethod.Put, buttons[1].GetAttribute("hx-put")!, sidebar);
			request.Content = new FormUrlEncodedContent(FormFields(sidebar).Append(new("RemovePizzaIndex", "1")));
			using var response = await host.Client.SendAsync(request);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			using var document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync());
			Assert.Contains("12\"", Assert.Single(document.QuerySelectorAll(".cart-item")).TextContent, StringComparison.Ordinal);
		});
	}

	[Theory]
	[InlineData(12, 16, 1, "12\"")]
	[InlineData(12, 16, 0, "16\"")]
	[InlineData(12, 12, 0, "12\"")]
	[InlineData(12, 0, 0, "Choose a pizza")]
	public async Task Put_remove_returns_sidebar_with_only_the_unselected_pizza_or_empty_order(
		int firstSize, int secondSize, int removeIndex, string expectedContent)
	{
		int[] sizes = secondSize == 0 ? [firstSize] : [firstSize, secondSize];
		await WithHostAsync(async (host, sidebar) =>
		{
			using var request = RemovalRequest(sidebar, sizes, removeIndex);
			using var response = await host.Client.SendAsync(request);
			var body = await response.Content.ReadAsStringAsync();

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			PartialResponseAssertions.IsSingleFragment(body, "form", expectedContent, "pizza-card", id: "sidebar");
			using var document = new HtmlParser().ParseDocument(body);
			Assert.Equal(sizes.Length - 1, document.QuerySelectorAll(".cart-item").Length);
			Assert.Equal(sizes.Length - 1, document.QuerySelectorAll("input[name$='.SpecialId']").Length);
			if (firstSize != secondSize)
			{
				Assert.DoesNotContain($"{sizes[removeIndex]}\"", document.Body!.TextContent, StringComparison.Ordinal);
			}
		});
	}

	private static HttpRequestMessage RemovalRequest(IElement sidebar, int[] sizes, int removeIndex)
	{
		var fields = FormFields(sidebar).ToList();
		for (var index = 0; index < sizes.Length; index++)
		{
			fields.Add(new($"OrderForm.Pizzas[{index}].SpecialId", "1"));
			fields.Add(new($"OrderForm.Pizzas[{index}].Size", sizes[index].ToString(CultureInfo.InvariantCulture)));
		}

		fields.Add(new("RemovePizzaIndex", removeIndex.ToString(CultureInfo.InvariantCulture)));
		var request = CreateRequest(HttpMethod.Put, "/", sidebar);
		request.Content = new FormUrlEncodedContent(fields);
		return request;
	}

	private static void AssertRemovalButton(IElement button, string index)
	{
		Assert.Equal("/", button.GetAttribute("hx-put"));
		Assert.Equal("#sidebar", button.GetAttribute("hx-target"));
		Assert.Equal("RemovePizzaIndex", button.GetAttribute("name"));
		Assert.Equal(index, button.GetAttribute("value"));
	}

	private static IEnumerable<KeyValuePair<string, string>> FormFields(IElement sidebar) =>
		sidebar.QuerySelectorAll("input[name]").Select(input =>
			new KeyValuePair<string, string>(input.GetAttribute("name")!, input.GetAttribute("value") ?? ""));

	private static HttpRequestMessage CreateRequest(HttpMethod method, string url, IElement sidebar)
	{
		var request = new HttpRequestMessage(method, url);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("HX-Target", "#sidebar");
		request.Headers.Add("RequestVerificationToken", sidebar.QuerySelector("input[name='__RequestVerificationToken']")!.GetAttribute("value"));
		return request;
	}

	private static async Task WithHostAsync(Func<SampleKestrelHost, IElement, Task> verify)
	{
		var directory = Path.Combine(AppContext.BaseDirectory, "pizza-removal", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			await using var host = await SampleKestrelHost.StartAsync("samples/BlazingPizza/BlazingPizza.csproj",
				new Dictionary<string, string> { ["ConnectionStrings__DefaultConnection"] = $"Data Source={Path.Combine(directory, "pizza.db")}" });
			using var page = await host.Client.GetAsync("/");
			Assert.Equal(HttpStatusCode.OK, page.StatusCode);
			var cookie = Assert.Single(page.Headers.GetValues("Set-Cookie").Where(value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))).Split(';')[0];
			host.Client.DefaultRequestHeaders.Add("Cookie", cookie);
			using var initial = new HtmlParser().ParseDocument(await page.Content.ReadAsStringAsync());
			var sidebar = initial.QuerySelector("#sidebar")!;
			await verify(host, sidebar);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}
