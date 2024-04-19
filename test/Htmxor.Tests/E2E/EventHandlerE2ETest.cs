using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Htmxor.Blazewright;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.E2E;

public class EventHandlerE2ETest : PageTest
{
    private readonly ITestOutputHelper outputHelper;

    public EventHandlerE2ETest(ITestOutputHelper outputHelper, PlaywrightFixture fixture) : base(fixture)
    {
        this.outputHelper = outputHelper;
    }

    [Fact]
    public async Task Invoke_event_handler_methods()
    {
        var page = await Context.NewPageAsync();
        await page.GotoAsync("/custom-event-handler");
        
        await page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).First.ClickAsync();
        await Expect(page.Locator("body")).ToContainTextAsync("OnGet");

        await page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).Nth(1).ClickAsync();
        await Expect(page.Locator("body")).ToContainTextAsync("OnGet");

        await page.GetByRole(AriaRole.Button, new() { Name = "GET", Exact = true }).Nth(2).ClickAsync();
        await Expect(page.Locator("body")).ToContainTextAsync("OnGet");

        await page.GetByRole(AriaRole.Button, new() { Name = "POST" }).ClickAsync();
        await Expect(page.Locator("body")).ToContainTextAsync("OnPost");

        await page.GetByRole(AriaRole.Button, new() { Name = "PUT" }).ClickAsync();
        await Expect(page.Locator("body")).ToContainTextAsync("OnPut");

        await page.GetByRole(AriaRole.Button, new() { Name = "DELETE" }).ClickAsync();
        await Expect(page.Locator("body")).ToContainTextAsync("OnDelete");
    }
}