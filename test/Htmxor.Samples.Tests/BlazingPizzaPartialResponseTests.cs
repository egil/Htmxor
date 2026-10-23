using System.Net;

namespace Htmxor.Samples.Tests;

public sealed class BlazingPizzaPartialResponseTests
{
	[Fact]
	public async Task Partial_get_home_returns_only_the_empty_order_sidebar()
	{
		var directory = Path.Combine(Path.GetTempPath(), "htmxor-samples-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			await using var host = await SampleKestrelHost.StartAsync(
				"samples/BlazingPizza/BlazingPizza.csproj",
				new Dictionary<string, string>
				{
					["ConnectionStrings__DefaultConnection"] = $"Data Source={Path.Combine(directory, "pizza.db")}",
				});
			using var request = new HttpRequestMessage(HttpMethod.Get, "/");
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
			using var response = await host.Client.SendAsync(request);
			var body = await response.Content.ReadAsStringAsync();

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			PartialResponseAssertions.IsSingleFragment(body, "form", "Choose a pizza", "pizza-card", id: "sidebar");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}
