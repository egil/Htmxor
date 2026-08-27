var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddLegacyHtmx();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery()
   .UseHtmxAntiforgery();

app.MapRazorComponents<Htmxor.TestApp.App>()
   .AddLegacyHtmxorComponentEndpoints(app);

app.Run();
