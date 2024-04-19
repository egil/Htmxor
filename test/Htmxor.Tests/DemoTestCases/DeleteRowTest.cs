using System.Net;
using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.DemoTestCases;

public class DeleteRowTest : TestAppTestBase
{
    public DeleteRowTest(TestAppFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Hx_delete_returns_204_without_content()
    {
        DataStore.Store(new User
        {
            Id = 1000,
            Active = true,
            Email = "foo@bar.baz",
            Name = "Foo Bar",
        });
        await Host.Scenario(s =>
        {
            s.Delete.Url($"/delete-row-1/{1000}");
            s.WithAntiforgeryTokensFrom(Host);
            s.WithHxHeaders();

            s.StatusCodeShouldBe(HttpStatusCode.NoContent);
            s.ContentShouldBe("");
        });
    }
}
