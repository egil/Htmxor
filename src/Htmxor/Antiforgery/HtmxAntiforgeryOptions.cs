using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace Htmxor.Antiforgery;

public class HtmxAntiforgeryOptions
{
	public bool IncludeAntiForgery { get; set; }
    public string FormFieldName { get; set; } 
	public string? HeaderName { get; set; }
    public string CookieName { get; set; }
}