using BlazorSSR;
using BlazorSSR.Components;
using BlazorSSR.Components.Contacts;
using BlazorSSR.Components.FlashMessages;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();
builder.Services.AddScoped<DiskStorage>();
builder.Services.AddScoped<ContactsRepository>();
builder.Services.AddScoped<Contact.Validator>();

builder.Services.AddFlashMessages();

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

app.MapRazorComponents<App>();

app.Run();
