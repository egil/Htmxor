using System.Net;
using System.Text.RegularExpressions;
using Htmxor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Htmxor.Explicit.Anywhere;

public sealed class Issue180DuplicateOwnerTests : IAsyncLifetime
{
    private WebApplication app = default!;
    private HttpClient client = default!;
    private readonly Issue180DuplicateProbe probe = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Issue180DuplicateApp).Assembly.GetName().Name });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton(probe);
        builder.Services.AddAuthentication("issue180").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Issue180AuthenticationHandler>("issue180", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddRazorComponents().AddHtmxor();
        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapRazorComponents<Issue180DuplicateApp>().AddHtmxorEndpoints();
        await app.StartAsync();
        client = app.GetTestClient();
    }

    [Fact]
    public async Task Inline_same_type_before_router_cannot_own_the_routed_action()
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, "/issue-180/duplicate/42?q=target");
        get.Headers.Add("X-Issue180-User", "Ada");
        using var credentials = await client.SendAsync(get);
        var getBody = await credentials.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);
        Assert.Contains("data-role=\"inline\"", getBody, StringComparison.Ordinal);
        Assert.Contains("data-role=\"routed\"", getBody, StringComparison.Ordinal);
        var getOwner = Regex.Match(getBody, "data-role=\"routed\" data-owner=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(getOwner);
        var token = Regex.Match(getBody, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var cookie = Assert.Single(credentials.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0];

        probe.Reset();
        using var post = new HttpRequestMessage(HttpMethod.Post, "/issue-180/duplicate/42?q=target") { Content = new FormUrlEncodedContent([]) };
        post.Headers.Add("X-Issue180-User", "Ada");
        post.Headers.Add("Cookie", cookie);
        post.Headers.Add("RequestVerificationToken", WebUtility.HtmlDecode(token));
        using var response = await client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, probe.Initializations.Count);
        Assert.True(probe.Initializations[0].Inline, "The inline instance must render before the routed instance.");
        var inline = Assert.Single(probe.Initializations, item => item.Inline);
        var routed = Assert.Single(probe.Initializations, item => !item.Inline);
        Assert.Equal(7, inline.Id);
        Assert.Equal(42, routed.Id);
        Assert.Equal("target", routed.Query);
        Assert.NotEqual(inline.Owner, routed.Owner);
        Assert.NotEqual(getOwner, routed.Owner);
        var callback = Assert.Single(probe.Callbacks);
        Assert.False(callback.Inline, $"The inline owner {inline.Owner} (Id {inline.Id}) received the callback; routed owner {routed.Owner} has Id {routed.Id}. Actual callback owner {callback.Owner}, Id {callback.Id}, query {callback.Query}.");
        Assert.Equal(routed.Owner, callback.Owner);
        Assert.Equal(42, callback.Id);
        Assert.Equal("target", callback.Query);
        Assert.Contains($"data-role=\"inline\" data-owner=\"{inline.Owner}\" data-id=\"7\" data-result=\"GET\"", body, StringComparison.Ordinal);
        Assert.Contains($"data-role=\"routed\" data-owner=\"{routed.Owner}\" data-id=\"42\" data-result=\"POST\"", body, StringComparison.Ordinal);
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        if (app is not null) await app.DisposeAsync();
    }
}

public sealed class Issue180DuplicateProbe
{
    public sealed record Observation(bool Inline, string Owner, int Id, string? Query);
    public List<Observation> Initializations { get; } = [];
    public List<Observation> Callbacks { get; } = [];
    public void Initialize(bool inline, string owner, int id, string? query) => Initializations.Add(new(inline, owner, id, query));
    public void Invoke(bool inline, string owner, int id, string? query) => Callbacks.Add(new(inline, owner, id, query));
    public void Reset() { Initializations.Clear(); Callbacks.Clear(); }
}

public sealed class Issue180AsyncFallbackTests : IAsyncLifetime
{
    private WebApplication app = default!;
    private HttpClient client = default!;
    private readonly Issue180BoundaryProbe probe = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Issue180AsyncFallbackApp).Assembly.GetName().Name });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton(probe);
        builder.Services.AddAuthentication("issue180").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Issue180AuthenticationHandler>("issue180", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddRazorComponents().AddHtmxor();
        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapRazorComponents<Issue180AsyncFallbackApp>().AddHtmxorEndpoints();
        await app.StartAsync();
        client = app.GetTestClient();
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("async")]
    public async Task Ordinary_callback_waits_for_async_error_fallback(string mode)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, "/issue-180/boundary/healthy");
        get.Headers.Add("X-Issue180-User", "Ada");
        using var credentials = await client.SendAsync(get);
        var getBody = await credentials.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, credentials.StatusCode);
        var token = Regex.Match(getBody, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var cookie = Assert.Single(credentials.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0];
        var getOwner = Regex.Match(getBody, "data-owner=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(getOwner);

        probe.Reset();
        probe.HoldFallback();
        using var post = new HttpRequestMessage(HttpMethod.Post, "/issue-180/boundary/" + mode) { Content = new FormUrlEncodedContent([]) };
        post.Headers.Add("X-Issue180-User", "Ada");
        post.Headers.Add("Cookie", cookie);
        post.Headers.Add("RequestVerificationToken", WebUtility.HtmlDecode(token));
        var responseTask = client.SendAsync(post);
        try
        {
            await probe.FallbackEntered.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(responseTask.IsCompleted, "The ordinary response completed while its fallback child was still gated.");
        }
        finally { probe.ReleaseFallback(); }
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () => response = await responseTask);
        Assert.Equal(1, probe.Initializations);
        Assert.NotEqual(getOwner, probe.InitializedOwner);
        Assert.Equal(1, probe.Callbacks);
        Assert.Equal(probe.InitializedOwner, probe.CallbackOwner);
        Assert.Null(failure);
        Assert.NotNull(response);
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.Contains("data-boundary=\"fallback\">completed", StringComparison.Ordinal), $"The completed fallback child was missing from the ordinary response: {body}");
        }
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        if (app is not null) await app.DisposeAsync();
    }
}
