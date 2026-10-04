using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Htmxor.AspNetCore10;

// General-purpose decoder for the per-component "<!--Blazor:{...}-->" render-mode-boundary marker that #275's
// parameterized parity cases compare. The outer marker shell (type, prerenderId, key, sequence, assembly,
// typeName, descriptor, parameterDefinitions, parameterValues) is a thin JSON object that both a stock host and
// an AddHtmxor candidate host already serialize with camelCase names (HtmxorEndpointCandidateJson.Options on the
// candidate side), so locating it and reading its own field values needs no naming-policy awareness. The
// payloads *inside* those field values -- the base64 parameterDefinitions/parameterValues text and the
// data-protected descriptor's own JSON -- are exactly what #275 compares, so decoding them stops at raw text and
// raw field names instead of binding to a typed shape that would itself assume a naming policy. Shared rather
// than private to one test class so every parity case that decodes a marker's parameter payloads agrees on what
// "decoded" means, the same way Issue272PersistedState owns persisted-state decoding.
internal static class Issue275ComponentMarker
{
	// Finds the richer of a prerendered pair of "<!--Blazor:{...}-->" markers: the end marker stock and the
	// candidate both write carries exactly one field (prerenderId), so a start marker -- which always carries at
	// least "type" and "key" besides it -- is identified by field count alone, regardless of what a naming
	// defect might rename any individual field to.
	public static JsonDocument ExtractStartMarker(string body)
	{
		const string prefix = "<!--Blazor:";
		var searchFrom = 0;
		while (true)
		{
			var markerStart = body.IndexOf(prefix, searchFrom, StringComparison.Ordinal);
			if (markerStart < 0)
			{
				throw new InvalidOperationException(
					"No '<!--Blazor:{...}-->' start marker carrying more than a prerenderId was found.");
			}

			var jsonStart = markerStart + prefix.Length;
			if (body[jsonStart] != '{')
			{
				searchFrom = jsonStart;
				continue;
			}

			var json = ExtractBalancedObject(body, jsonStart);
			var document = JsonDocument.Parse(json);
			if (document.RootElement.EnumerateObject().Count() > 1)
			{
				return document;
			}

			document.Dispose();
			searchFrom = jsonStart + json.Length;
		}
	}

	public static string GetStringField(JsonDocument marker, string name)
		=> marker.RootElement.GetProperty(name).GetString()
			?? throw new InvalidOperationException($"Marker field '{name}' was unexpectedly null.");

	public static string DecodeBase64Utf8(string base64)
		=> Encoding.UTF8.GetString(Convert.FromBase64String(base64));

	// Unprotects the marker's data-protected descriptor with the caller's own host's data-protection provider
	// (purpose "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1", matching
	// ServerComponentSerializationSettings and the candidate's own protector) and returns every top-level field
	// by its own exact name and raw JSON text, so a naming or null-handling defect in the embedded payload --
	// not this helper's own assumption about what the names should be -- decides whether two hosts' fields
	// compare equal.
	public static IReadOnlyDictionary<string, string> DecodeDescriptorFields(string base64Descriptor, IDataProtectionProvider protection)
	{
		var protector = protection
			.CreateProtector("Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1")
			.ToTimeLimitedDataProtector();
		var bytes = protector.Unprotect(Convert.FromBase64String(base64Descriptor));
		using var document = JsonDocument.Parse(bytes);
		var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var property in document.RootElement.EnumerateObject())
		{
			fields[property.Name] = property.Value.GetRawText();
		}

		return fields;
	}

	// A hand-rolled balanced-brace scan rather than a regex: the marker's "key" field is itself a nested JSON
	// object, so a non-greedy "{.*?}" pattern would stop at that inner object's own closing brace. The scanner
	// below skips string literals (quotes and escapes tracked) so a brace inside a quoted value -- none of this
	// fixture's own field values contain one, but a future caller's might -- can never miscount depth.
	private static string ExtractBalancedObject(string body, int start)
	{
		var scanner = new BalancedObjectScanner();
		for (var index = start; index < body.Length; index++)
		{
			if (scanner.Consume(body[index]))
			{
				return body[start..(index + 1)];
			}
		}

		throw new InvalidOperationException("The '<!--Blazor:{...}-->' marker's JSON object was never closed.");
	}

	// Tracks just enough JSON state -- object-nesting depth, and whether a quoted string (with its own escape
	// sequences) is currently open -- to find a top-level object's matching closing brace.
	private struct BalancedObjectScanner
	{
		private int depth;
		private bool inString;
		private bool escaped;

		// Returns true the instant the character that closes the outermost object is consumed.
		public bool Consume(char current)
		{
			if (inString)
			{
				ConsumeStringCharacter(current);
				return false;
			}

			return ConsumeStructuralCharacter(current);
		}

		private bool ConsumeStructuralCharacter(char current)
		{
			switch (current)
			{
				case '"':
					inString = true;
					return false;
				case '{':
					depth++;
					return false;
				case '}':
					depth--;
					return depth == 0;
				default:
					return false;
			}
		}

		private void ConsumeStringCharacter(char current)
		{
			if (escaped)
			{
				escaped = false;
			}
			else if (current == '\\')
			{
				escaped = true;
			}
			else if (current == '"')
			{
				inString = false;
			}
		}
	}
}
