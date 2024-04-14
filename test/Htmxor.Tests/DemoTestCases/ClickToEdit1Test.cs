using System.Net;
using Htmxor.TestApp;
using Htmxor.TestApp.Components.Pages.Examples;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.DemoTestCases;

public class ClickToEdit1Test : TestAppTestBase
{
    public ClickToEdit1Test(TestAppFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task None_hx_get_view()
    {
        var contact = new Contact
        {
            Id = DataStore.GetNextId<Contact>(),
            FirstName = "Joe",
            LastName = "Blow",
            Email = "joe@blow.com",
        };
        DataStore.Store(contact);
        await Host.Scenario(s =>
        {
            s.Get.Url($"/click-to-edit-1/contact/{contact.Id}");

            s.StatusCodeShouldBe(HttpStatusCode.OK);
            s.ContentShouldBeHtml(FullPageContent($"""
                <div hx-target="this" hx-swap="outerHTML">
                    <div><label>First Name</label>: {contact.FirstName}</div>
                    <div><label>Last Name</label>: {contact.LastName}</div>
                    <div><label>Email</label>: {contact.Email}</div>
                    <button hx-get="/click-to-edit-1/contact/{contact.Id}/edit" class="btn btn-primary">
                        Click To Edit
                    </button>
                </div>
                """));
        });
    }

    [Fact]
    public async Task None_hx_get_edit()
    {
        var contact = new Contact
        {
            Id = DataStore.GetNextId<Contact>(),
            FirstName = "Joe",
            LastName = "Blow",
            Email = "joe@blow.com",
        };
        DataStore.Store(contact);
        await Host.Scenario(s =>
        {
            s.Get.Url($"/click-to-edit-1/contact/{contact.Id}/edit");

            s.StatusCodeShouldBe(HttpStatusCode.OK);
            s.ContentShouldBeHtml(FullPageContent($"""
                <form hx-put="/click-to-edit-1/contact/{contact.Id}" hx-target="this" hx-swap="outerHTML">
                  <div>
                    <label>First Name</label>
                    <input type="text" name="Contact.FirstName" value="{contact.FirstName}">
                  </div>
                  <div class="form-group">
                    <label>Last Name</label>
                    <input type="text" name="Contact.LastName" value="{contact.LastName}">
                  </div>
                  <div class="form-group">
                    <label>Email Address</label>
                    <input type="email" name="Contact.Email" value="{contact.Email}">
                  </div>
                  <button class="btn">Submit</button>
                  <button class="btn" hx-get="/click-to-edit-1/contact/{contact.Id}">Cancel</button>
                </form>
                """));
        });
    }

    [Fact]
    public async Task Hx_get_view()
    {
        var contact = new Contact
        {
            Id = DataStore.GetNextId<Contact>(),
            FirstName = "Joe",
            LastName = "Blow",
            Email = "joe@blow.com",
        };
        DataStore.Store(contact);
        await Host.Scenario(s =>
        {
            s.Get.Url($"/click-to-edit-1/contact/{contact.Id}");
            s.WithHxHeaders();
            s.StatusCodeShouldBe(HttpStatusCode.OK);

            s.ContentShouldBeHtml($"""
                <div hx-target="this" hx-swap="outerHTML">
                    <div><label>First Name</label>: {contact.FirstName}</div>
                    <div><label>Last Name</label>: {contact.LastName}</div>
                    <div><label>Email</label>: {contact.Email}</div>
                    <button hx-get="/click-to-edit-1/contact/{contact.Id}/edit" class="btn btn-primary">
                        Click To Edit
                    </button>
                </div>
                """);
        });
    }

    [Fact]
    public async Task Hx_get_edit()
    {
        var contact = new Contact
        {
            Id = DataStore.GetNextId<Contact>(),
            FirstName = "Joe",
            LastName = "Blow",
            Email = "joe@blow.com",
        };
        DataStore.Store(contact);
        await Host.Scenario(s =>
        {
            s.Get.Url($"/click-to-edit-1/contact/{contact.Id}/edit");
            s.WithHxHeaders();

            s.StatusCodeShouldBe(HttpStatusCode.OK);
            s.ContentShouldBeHtml($"""
                <form hx-put="/click-to-edit-1/contact/{contact.Id}" hx-target="this" hx-swap="outerHTML">
                  <div>
                    <label>First Name</label>
                    <input type="text" name="Contact.FirstName" value="{contact.FirstName}">
                  </div>
                  <div class="form-group">
                    <label>Last Name</label>
                    <input type="text" name="Contact.LastName" value="{contact.LastName}">
                  </div>
                  <div class="form-group">
                    <label>Email Address</label>
                    <input type="email" name="Contact.Email" value="{contact.Email}">
                  </div>
                  <button class="btn">Submit</button>
                  <button class="btn" hx-get="/click-to-edit-1/contact/{contact.Id}">Cancel</button>
                </form>
                """);
        });
    }

    [Fact]
    public async Task Hx_put_view()
    {
        var contact = new Contact
        {
            Id = DataStore.GetNextId<Contact>(),
            FirstName = "Joe",
            LastName = "Blow",
            Email = "joe@blow.com",
        };
        DataStore.Store(contact);

        await Host.Scenario(s =>
        {
            s.Put.FormData(new()
                {
                    { "Contact.FirstName", "Foo" },
                    { "Contact.LastName", "Bar" },
                    { "Contact.Email", "foo@bar.com" },
                })
                .ToUrl($"/click-to-edit-1/contact/{contact.Id}");
            s.WithAntiforgeryTokensFrom(Host);
            s.WithHxHeaders();

            s.StatusCodeShouldBe(HttpStatusCode.OK);
            s.ContentShouldBeHtml($"""
                <div hx-target="this" hx-swap="outerHTML">
                    <div><label>First Name</label>: Foo</div>
                    <div><label>Last Name</label>: Bar</div>
                    <div><label>Email</label>: foo@bar.com</div>
                    <button hx-get="/click-to-edit-1/contact/{contact.Id}/edit" class="btn btn-primary">
                        Click To Edit
                    </button>
                </div>
                """);
        });
    }
}
