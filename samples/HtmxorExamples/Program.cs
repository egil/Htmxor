using Htmxor;
using HtmxorExamples.Components;
using HtmxorExamples.Components.Pages;
using HtmxorExamples.Components.Pages.Examples.OutOfBandOutlets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ToastService>();
builder.Services.AddRazorComponents().AddHtmx(options =>
{
	// Enabled to support out of band updates
	options.UseTemplateFragments = true;

	// Enabled to show use of trigger specification cache
	options.TriggerSpecsCache = [
		Trigger.Revealed(), // Used in InfiniteScroll demo
		Trigger.OnEvent("newContact").From("body"), // Used in TriggeringEvents demo
		Trigger.OnEvent("keyup").Changed().Delay(TimeSpan.FromMilliseconds(500))
			.Or()
			.OnEvent("mouseenter").Once(),  //  Unused, demonstrates complex trigger
		Trigger.Every(TimeSpan.FromSeconds(30)) // Unused, demonstrates use of Every
			.Or()
			.OnEvent("newContact").From("closest (form input)"),
	];
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
app.UseHtmxAntiforgery();
app.MapRazorComponents<App>()
   .AddHtmxorComponentEndpoints(app);

// Demonstrating mapping of a page/component
app.MapGet("/mapped/home", () => new HtmxorComponentResult<Home>());

app.Run();
