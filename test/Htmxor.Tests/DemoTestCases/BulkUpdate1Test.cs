using System.Net;
using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.DemoTestCases;

public class BulkUpdate1Test : TestAppTestBase
{
    public BulkUpdate1Test(TestAppFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Hx_post_with_partial_return()
    {
        await Host.Scenario(s =>
        {
            s.Post.Url("/bulk-update-1");
            s.WithFormData(("Active", "1"), ("Active", "3"));
            s.WithAntiforgeryTokensFrom(Host);
            s.WithHxHeaders(target: "toast", trigger: "checked-contacts", currentURL: $"{Host.Server.BaseAddress}bulk-update-1");

            s.StatusCodeShouldBe(HttpStatusCode.OK);
            s.ContentShouldBeHtml($"""
                <span id="toast" aria-live="polite">Activated 0 and deactivated 2 users.</span>
                """);
        });
    }
}
