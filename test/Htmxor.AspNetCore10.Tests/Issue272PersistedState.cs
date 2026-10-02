using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10;

// General-purpose persisted-state decoder for paired stock/candidate hosted parity cases: unprotects
// Blazor-Server-Component-State with the host's own data-protection key, parses both the Server and
// WebAssembly markers as JSON, and rewrites each marker's content as a sorted, decoded "key=value" list so
// two separately keyed hosts compare by content instead of by opaque, per-host-random bytes. This is
// deliberately a free-standing, parameterized type rather than private to one test class: #269's own
// test-contract branch carries a pre-#272 local copy of this exact shape (Issue269PersistedState); once it
// rebases onto this issue it reuses this type instead of keeping a second copy (see #272's "Fixture
// ownership").
internal static class Issue272PersistedState
{
	public static string Decode(string body, IDataProtectionProvider protection)
	{
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
	// request and keyed per host, exactly like Issue191PersistedStateParityTests' own NormalizePersistedValue.
	private static string NormalizeAntiforgeryToken(string value)
		=> Regex.Replace(value, "\"value\":\"[^\"]+\"(?=,\"formFieldName\":\"__RequestVerificationToken\")", "\"value\":\"<antiforgery-token>\"");
}
