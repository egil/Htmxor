namespace HtmxBlazorSSR.Components.FlashMessages;

public static class FlashMessagesServicesExtensions
{
    public static void AddFlashMessages(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<FlashMessageQueue>();
    }
}
