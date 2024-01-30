using Microsoft.AspNetCore.Http;

namespace Htmxor.Http.Mock;

/// <summary>
/// Mock implementation of the <see cref="HttpRequest"/> class for testing purposes.
/// </summary>
internal class MockHttpRequest : HttpRequest
{
	/// <inheritdoc/>
	public override IHeaderDictionary Headers { get; } = new MockHeaderDictionary();

	/// <inheritdoc/>
	public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default)
	{
		// This method is not implemented in the mock. You may need to implement it based on your testing requirements.
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override HttpContext HttpContext { get; }

	/// <inheritdoc/>
	public override string Method { get; set; }

	/// <inheritdoc/>
	public override string Scheme { get; set; }

	/// <inheritdoc/>
	public override bool IsHttps { get; set; }

	/// <inheritdoc/>
	public override HostString Host { get; set; }

	/// <inheritdoc/>
	public override PathString PathBase { get; set; }

	/// <inheritdoc/>
	public override PathString Path { get; set; }

	/// <inheritdoc/>
	public override QueryString QueryString { get; set; }

	/// <inheritdoc/>
	public override IQueryCollection Query { get; set; }

	/// <inheritdoc/>
	public override string Protocol { get; set; }

	/// <inheritdoc/>
	public override IRequestCookieCollection Cookies { get; set; }

	/// <inheritdoc/>
	public override long? ContentLength { get; set; }

	/// <inheritdoc/>
	public override string? ContentType { get; set; }

	/// <inheritdoc/>
	public override Stream Body { get; set; }

	/// <inheritdoc/>
	public override bool HasFormContentType { get; }

	/// <inheritdoc/>
	public override IFormCollection Form { get; set; }
}
