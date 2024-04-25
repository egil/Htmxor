using BlazingPizza.Components;
using BlazingPizza.Server;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddHtmx();

builder.Services.AddDbContext<PizzaStoreContext>(options => options.UseSqlite("Data Source=data/pizza.db"));
builder.Services.AddScoped<OrdersClient>();
builder.Services.AddScoped<PizzaClient>();
builder.Services.AddScoped<OrderState>();

var app = builder.Build();

// Initialize the database
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PizzaStoreContext>();
    if (db.Database.EnsureCreated())
    {
        SeedData.Initialize(db);
    }
}

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

app.MapPizzaApi();

app.MapRazorComponents<App>().AddHtmxorComponentEndpoints(app);

app.Run();
