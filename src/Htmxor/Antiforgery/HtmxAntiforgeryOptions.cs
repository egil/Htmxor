using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace Htmxor.Antiforgery;

public class HtmxAntiforgeryOptions(IOptions<AntiforgeryOptions> antiforgeryOptions)
{
    public string FormFieldName { get; } = antiforgeryOptions.Value.FormFieldName;

    public string? HeaderName { get; } = antiforgeryOptions.Value.HeaderName;

    public string CookieName { get; } = "HX-XSRF-TOKEN";
}