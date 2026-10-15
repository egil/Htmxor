#if NET11_0_OR_GREATER
using System.Net;

namespace Htmxor.AspNetCore10;

public sealed class Issue210CredentialCorsParityTests
{
    [Theory]
    [InlineData("cross-site", "trusted", "https://trusted.example", HttpStatusCode.BadRequest, 0)]
    [InlineData("same-site", "trusted", "https://trusted.example", HttpStatusCode.BadRequest, 0)]
    [InlineData("cross-site", "trusted-credentials", "https://trusted.example", HttpStatusCode.OK, 1)]
    [InlineData("same-site", "trusted-credentials", "https://trusted.example", HttpStatusCode.OK, 1)]
    [InlineData("cross-site", "wildcard", "https://trusted.example", HttpStatusCode.BadRequest, 0)]
    [InlineData("cross-site", "trusted-credentials", "https://untrusted.example", HttpStatusCode.BadRequest, 0)]
    public async Task Ordinary_form_preserves_stocks_credential_aware_origin_decision(
        string fetchSite, string cors, string origin, HttpStatusCode expected, int callbacks)
    {
        await using var stock = await Issue210SecurityHost.StartAsync(tokens: false, cors: cors, htmxor: false, groupPrefix: "");
        await using var candidate = await Issue210SecurityHost.StartAsync(tokens: false, cors: cors, groupPrefix: "");
        await AssertDecisionAsync(stock, fetchSite, origin, expected, callbacks);
        await AssertDecisionAsync(candidate, fetchSite, origin, expected, callbacks);
    }

    private static async Task AssertDecisionAsync(Issue210SecurityHost host, string fetchSite, string origin,
        HttpStatusCode expected, int callbacks)
    {
        using var request = Issue210SecurityHost.Request("POST", "form", form: true, direct: false);
        request.RequestUri = new Uri("/issue-210/form", UriKind.Relative);
        request.Headers.Add("Sec-Fetch-Site", fetchSite);
        request.Headers.Add("Origin", origin);
        using var response = await host.Client.SendAsync(request);
        Assert.True(expected == response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(callbacks, host.Probe.Callbacks);
        if (callbacks == 0)
        {
            host.Probe.AssertUntouched();
        }
        else
        {
            Assert.Contains("data-saved=\"True\">submitted", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
    }
}
#endif
