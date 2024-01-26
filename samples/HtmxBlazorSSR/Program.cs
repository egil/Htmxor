using HtmxBlazorSSR;
using HtmxBlazorSSR.Components;
using HtmxBlazorSSR.Components.FlashMessages;
using Htmxor;
using Htmxor.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();
builder.Services.AddScoped<Archiver>();
builder.Services.AddScoped<DiskStorage>();
builder.Services.AddScoped<ContactsRepository>();
builder.Services.AddFlashMessages();

builder.AddHtmxor()
    .WithDefaultConfiguration(config =>
    {
        config.SelfRequestsOnly = true;
    })
    .WithNamedConfiguration("articles", config =>
    {
        config.SelfRequestsOnly = true;
        config.GlobalViewTransitions = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseMiddleware<HtmxAntiforgeryMiddleware>();

app.MapGet("/contacts/count", async (ContactsRepository repo) =>
{
    var contacts = await repo.All();
    return Results.Content(
        content: $"({contacts.Count()} total Contacts)",
        contentType: "text/html");
});

app.MapRazorComponents<App>();

app.Run();
