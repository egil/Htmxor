using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue318AntiforgeryTests
{
	public static TheoryData<string, bool, bool> RejectedRequests => new()
	{
		{ "POST", false, false }, { "POST", true, false },
		{ "PUT", false, false }, { "PUT", true, false },
		{ "PATCH", false, false }, { "PATCH", true, false },
		{ "POST", false, true }, { "POST", true, true },
		{ "PUT", false, true }, { "PUT", true, true },
		{ "PATCH", false, true }, { "PATCH", true, true },
	};

	[Theory]
	[MemberData(nameof(RejectedRequests))]
	public async Task Missing_or_invalid_token_rejects_before_binding_lifecycle_or_callback(
		string method, bool invalidToken, bool multipart)
	{
		await AssertRejectedAsync(method, invalidToken, multipart, disableAfterMiddleware: false);
	}

	[Theory]
	[MemberData(nameof(RejectedRequests))]
	public async Task Failed_middleware_verdict_remains_authoritative_when_endpoint_validation_is_disabled(
		string method, bool invalidToken, bool multipart)
	{
		await AssertRejectedAsync(method, invalidToken, multipart, disableAfterMiddleware: true);
	}

	private static async Task AssertRejectedAsync(string method, bool invalidToken, bool multipart, bool disableAfterMiddleware)
	{
		await using var host = await Issue318Host.StartAsync(disableAfterMiddleware: disableAfterMiddleware);
		using var request = Issue318Host.Request(method, multipart);
		if (invalidToken)
		{
			await host.AddCredentialsAsync(request);
			request.Headers.Remove("RequestVerificationToken");
			request.Headers.Add("RequestVerificationToken", "corrupted");
		}

		using var response = await host.Client.SendAsync(request);

		Assert.False(host.Probe.MiddlewareValid);
		Assert.Equal(!disableAfterMiddleware, host.Probe.RequiresValidation);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		host.Probe.AssertNoApplicationEffects();
	}
}
