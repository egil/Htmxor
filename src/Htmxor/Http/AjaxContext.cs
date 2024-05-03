using System.Text.Json.Serialization;

namespace Htmxor.Http;

/// <summary>
/// Represents a location event with various properties.
/// </summary>
public class AjaxContext
{
	/// <summary>
	/// Gets or sets the source element of the request.
	/// </summary>
	[JsonPropertyName("source")]
	public string? Source { get; set; }

	/// <summary>
	/// Gets or sets an event that "triggered" the request.
	/// </summary>
	[JsonPropertyName("event")]
	public string? Event { get; set; }

	/// <summary>
	/// Gets or sets a callback that will handle the response HTML.
	/// </summary>
	[JsonPropertyName("handler")]
	public string? Handler { get; set; }

	/// <summary>
	/// Gets or sets the target to swap the response into.
	/// </summary>
	[JsonPropertyName("target")]
	public string? Target { get; set; }

	/// <summary>
	/// Gets or sets how the response will be swapped in relative to the target.
	/// </summary>
	[JsonPropertyName("swap")]
	public SwapStyle? Swap { get; set; }

	/// <summary>
	/// Gets or sets values to submit with the request.
	/// </summary>
	[JsonPropertyName("values")]
	public string? Values { get; set; }

	/// <summary>
	/// Gets or sets headers to submit with the request.
	/// </summary>
	[JsonPropertyName("headers")]
	public string? Headers { get; set; }

	/// <summary>
	/// Gets or sets allows you to select the content you want swapped from a response.
	/// </summary>
	[JsonPropertyName("select")]
	public string? Select { get; set; }
}

