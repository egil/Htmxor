using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10;

// General-purpose persisted-state decoder for paired stock/candidate hosted parity cases: unprotects
// Blazor-Server-Component-State with the host's own data-protection key, parses both the Server and
// WebAssembly markers as JSON, and rewrites each marker's content as a sorted, decoded "key=value" list so
// two separately keyed hosts compare by content instead of by opaque, per-host-random bytes. It is shared
// rather than private to one test class so every parity case that compares decoded persisted state agrees on
// what "decoded" means.
internal static class Issue272PersistedState
{
	public static string Decode(string body, IDataProtectionProvider protection)
	{
		// A render-mode boundary marker's prerenderId is random per render, and a Server marker's descriptor
		// is data-protected with the host's own ephemeral key, so neither can match across two separately
		// keyed hosts. Everything else on the marker (type, key, and the plain WebAssembly component fields)
		// stays compared verbatim.
		var normalized = Regex.Replace(body, "\"(prerenderId|descriptor)\":\"[^\"]+\"", "\"$1\":\"<dynamic>\"");
		return Regex.Replace(normalized, "<!--Blazor-(Server|WebAssembly)-Component-State:(.*?)-->", match =>
		{
			var bytes = Convert.FromBase64String(match.Groups[2].Value);
			if (match.Groups[1].Value == "Server")
			{
				bytes = protection.CreateProtector("Microsoft.AspNetCore.Components.Server.State").Unprotect(bytes);
			}

			var state = JsonSerializer.Deserialize<SortedDictionary<string, byte[]>>(bytes)!;
			var decoded = string.Join(";", state.Select(item => $"{item.Key}={NormalizeAntiforgeryToken(Encoding.UTF8.GetString(item.Value))}"));
			return $"<!--Blazor-{match.Groups[1].Value}-Component-State:{decoded}-->";
		});
	}

	// The framework's own AntiforgeryStateProvider registers an OnPersisting callback on every interactive
	// endpoint (not something a fixture component persists itself), carrying a token value that is random per
	// request and keyed per host.
	private static string NormalizeAntiforgeryToken(string value)
		=> Regex.Replace(value, "\"value\":\"[^\"]+\"(?=,\"formFieldName\":\"__RequestVerificationToken\")", "\"value\":\"<antiforgery-token>\"");

	// Stock's own oracle for a persisted entry's placement: it is filed under exactly the store(s) the caller
	// expects, and absent from any store it does not expect, so neither a dropped entry nor a fixture that
	// ignored the selected render mode can hide behind a bare "is it in the decoded body somewhere" check.
	// Parameterized by expected stores rather than by a mode token, so a caller's own mode vocabulary (#272's
	// "wasm", #269's "webassembly") never needs to agree with this decoder's.
	public static void AssertEntryInStores(string decodedBody, string entry, bool expectServer, bool expectWebAssembly)
	{
		var serverStore = ExtractStore(decodedBody, "Server");
		var webAssemblyStore = ExtractStore(decodedBody, "WebAssembly");
		if (expectServer)
		{
			Assert.Contains(entry, serverStore, StringComparison.Ordinal);
		}
		else
		{
			Assert.DoesNotContain(entry, serverStore, StringComparison.Ordinal);
		}

		if (expectWebAssembly)
		{
			Assert.Contains(entry, webAssemblyStore, StringComparison.Ordinal);
		}
		else
		{
			Assert.DoesNotContain(entry, webAssemblyStore, StringComparison.Ordinal);
		}
	}

	private static string ExtractStore(string decodedBody, string store)
	{
		var match = Regex.Match(decodedBody, $"<!--Blazor-{store}-Component-State:(.*?)-->");
		return match.Success ? match.Groups[1].Value : string.Empty;
	}
}
