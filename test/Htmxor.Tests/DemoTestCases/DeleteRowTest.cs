using System.Net;
using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples;

namespace Htmxor.DemoTestCases;

public class DeleteRowTest : TestAppTestBase
{
    public DeleteRowTest(TestAppFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Hx_delete_returns_204_without_content()
    {
        var user = DataStore.Store(new User
        {
            Id = Guid.NewGuid(),
            Email = "foo@bar.baz",
            Name = "Foo Bar",
        });
        await Host.Scenario(s =>
        {
            s.Delete.Url($"/delete-row-1/{user.Id}");
            s.WithAntiforgeryTokensFrom(Host);
            s.WithHxHeaders();

            s.StatusCodeShouldBe(HttpStatusCode.OK);
            s.ContentShouldBe("");
        });
    }
}
