var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddHtmx();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery()
   .UseHtmxAntiforgery();

app.MapRazorComponents<Htmxor.TestApp.App>()
   .AddHtmxorComponentEndpoints(app);

app.Run();
