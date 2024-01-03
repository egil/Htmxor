namespace HtmxBlazorSSR.Htmx;

public static class WebApplicationBuilderExtensions
{
    public static void AddHtmx(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<HtmxContext>(srv => srv.GetRequiredService<IHttpContextAccessor>().HttpContext!.GetHtmxContext());
    }
}
