using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10;

// General-purpose decoder for the per-component "<!--Blazor:{...}-->" render-mode-boundary marker that #275's
// parameterized parity cases compare. The outer marker shell (type, prerenderId, key, sequence, assembly,
// typeName, descriptor, parameterDefinitions, parameterValues) is a thin JSON object that both a stock host and
// an AddHtmxor candidate host already serialize with camelCase names (HtmxorEndpointCandidateJson.Options on the
// candidate side), so locating it and reading its own field values needs no naming-policy awareness. The
// payloads *inside* those field values -- the base64 parameterDefinitions/parameterValues text and the
// data-protected descriptor's own JSON -- are exactly what #275 compares, so decoding them stops at raw text
// instead of binding to a typed shape that would itself assume a naming policy. Shared rather than private to
// one test class so every parity case that decodes a marker's parameter payloads agrees on what "decoded"
// means, the same way Issue272PersistedState owns persisted-state decoding.
internal static class Issue275ComponentMarker
{
	// Finds the richer of a prerendered pair of "<!--Blazor:{...}-->" markers: the end marker carries only
	// prerenderId, so the start marker is identified by field count alone, whatever a naming defect renames its
	// fields to. Both hosts serialize the marker with System.Text.Json's default encoder, which escapes '>', so
	// the first "-->" always closes the marker even though its "key" field is a nested object.
	public static JsonDocument ExtractStartMarker(string body)
	{
		foreach (Match match in Regex.Matches(body, @"<!--Blazor:(\{.*?\})-->"))
		{
			var document = JsonDocument.Parse(match.Groups[1].Value);
			if (document.RootElement.EnumerateObject().Count() > 1)
			{
				return document;
			}

			document.Dispose();
		}

		throw new InvalidOperationException(
			"No '<!--Blazor:{...}-->' start marker carrying more than a prerenderId was found.");
	}

	public static string GetStringField(JsonDocument marker, string name)
		=> marker.RootElement.GetProperty(name).GetString()
			?? throw new InvalidOperationException($"Marker field '{name}' was unexpectedly null.");

	public static string DecodeBase64Utf8(string base64)
		=> Encoding.UTF8.GetString(Convert.FromBase64String(base64));

	// Unprotects the marker's data-protected descriptor with the caller's own host's data-protection provider
	// (purpose "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1", matching
	// ServerComponentSerializationSettings and the candidate's own protector) and returns its decoded JSON text
	// verbatim, with only invocationId blanked (ServerComponentInvocationSequence/Guid.NewGuid(), never shared
	// by two separately keyed hosts). Every other byte -- names, null handling, and field order -- is left
	// exactly as the host wrote it, so a naming or null-handling defect decides whether two hosts' descriptors
	// compare equal, the same way Issue272PersistedState.Decode blanks only its own per-host-random values.
	public static string DecodeDescriptor(string base64Descriptor, IDataProtectionProvider protection)
	{
		var bytes = protection
			.CreateProtector("Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1")
			.ToTimeLimitedDataProtector()
			.Unprotect(Convert.FromBase64String(base64Descriptor));
		return Regex.Replace(Encoding.UTF8.GetString(bytes), "\"invocationId\":\"[^\"]+\"", "\"invocationId\":\"<dynamic>\"");
	}
}
