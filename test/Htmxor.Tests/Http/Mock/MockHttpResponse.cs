using Microsoft.AspNetCore.Http;

namespace Htmxor.Http.Mock;

/// <summary>
///     Mock implementation of the <see cref="HttpResponse" /> class for testing purposes.
/// </summary>
public class MockHttpResponse : HttpResponse
{
	/// <inheritdoc />
	public override IHeaderDictionary Headers { get; } = new MockHeaderDictionary();

	/// <inheritdoc />
	public override IResponseCookies Cookies { get; } = new MockResponseCookies();

	/// <inheritdoc />
	public override HttpContext HttpContext { get; } = default!;

	/// <inheritdoc />
	public override int StatusCode { get; set; }

	/// <inheritdoc />
	public override Stream Body { get; set; } = default!;

	/// <inheritdoc />
	public override long? ContentLength { get; set; }

	/// <inheritdoc />
	public override string? ContentType { get; set; }

	/// <inheritdoc />
	public override bool HasStarted { get; } = default!;

	/// <inheritdoc />
	public override void OnStarting(Func<object, Task> callback, object state)
	{
	}

	/// <inheritdoc />
	public override void OnCompleted(Func<object, Task> callback, object state)
	{
	}

	/// <inheritdoc />
	public override void Redirect(string location, bool permanent)
	{
	}
}