using Microsoft.AspNetCore.Components;
#if NET11_0_OR_GREATER
using Microsoft.AspNetCore.Components.Web;
#endif
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

// The values #230's pages write before they navigate. TempData and Session parameters exist only on .NET 11, so
// the .NET 10 build of these pages carries nothing and exercises the redirect representation alone.
public abstract class Issue230Carrier : ComponentBase
{
	public const string Missing = "missing";

	[CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

#if NET11_0_OR_GREATER
	[SupplyParameterFromTempData(Name = "issue230-message")] public string? Message { get; set; }
	[SupplyParameterFromSession(Name = "issue230-value")] public string? Value { get; set; }
	[CascadingParameter] public ITempData? TempData { get; set; }

	// Only the supplied-parameter writes, so a surviving value proves supplier write-back rather than a direct
	// store write that would persist on its own.
	protected void Write(string value)
	{
		Message = value;
		Value = value;
	}

	// Establishes the Session cookie independently, so a later read isolates parameter write-back.
	protected void EstablishSession() => HttpContext.Session.SetString("issue230-established", "established");

	protected string ReadMessage() => TempData?.Get("issue230-message") as string ?? Missing;

	protected string ReadValue() => Value ?? Missing;
#else
	protected static void Write(string value) => _ = value;

	protected static void EstablishSession()
	{
	}

	protected static string ReadMessage() => Missing;

	protected static string ReadValue() => Missing;
#endif
}
