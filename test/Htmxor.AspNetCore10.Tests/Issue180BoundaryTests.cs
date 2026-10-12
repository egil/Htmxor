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

public sealed class Issue180BoundaryTests : IAsyncLifetime
{
    private WebApplication app = default!;
    private HttpClient client = default!;
    private readonly Issue180BoundaryProbe probe = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Issue180BoundaryApp).Assembly.GetName().Name });
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
        app.MapRazorComponents<Issue180BoundaryApp>().AddHtmxorEndpoints();
        await app.StartAsync();
        client = app.GetTestClient();
    }

    [Theory]
    [InlineData("healthy", "healthy", 1)]
    [InlineData("sync", "fallback", 1)]
    [InlineData("async", "fallback", 1)]
    [InlineData("initialize", "fallback", 0)]
    [InlineData("render", "fallback", 0)]
    public async Task Ordinary_explicit_action_respects_enclosing_error_boundary(string mode, string expected, int callbacks)
    {
        var credentials = await CredentialsAsync();
        probe.Reset();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/issue-180/boundary/" + mode)
        {
            Content = new FormUrlEncodedContent([])
        };
        request.Headers.Add("X-Issue180-User", "Ada");
        request.Headers.Add("Cookie", credentials.Cookie);
        request.Headers.Add("RequestVerificationToken", credentials.Token);
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () => { response = await client.SendAsync(request); });

        Assert.Equal(1, probe.Initializations);
        Assert.NotEqual(credentials.Owner, probe.InitializedOwner);
        if (response is not null)
        {
            using (response)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("data-boundary=\"" + expected + "\"", body, StringComparison.Ordinal);
            }
        }
        Assert.Equal(callbacks, probe.Callbacks);
        if (callbacks == 1) Assert.Equal(probe.InitializedOwner, probe.CallbackOwner);
        Assert.Null(failure);
        Assert.NotNull(response);
    }

    private async Task<(string Cookie, string Token, string Owner)> CredentialsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/issue-180/boundary/healthy");
        request.Headers.Add("X-Issue180-User", "Ada");
        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0];
        var owner = Regex.Match(html, "data-owner=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(owner);
        return (cookie, WebUtility.HtmlDecode(token), owner);
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        if (app is not null) await app.DisposeAsync();
    }
}

public sealed class Issue180BoundaryProbe
{
    private TaskCompletionSource? fallbackEntered;
    private TaskCompletionSource? fallbackRelease;
    public int Initializations { get; private set; }
    public int Callbacks { get; private set; }
    public string? InitializedOwner { get; private set; }
    public string? CallbackOwner { get; private set; }
    public void Initialize(string owner) { Initializations++; InitializedOwner = owner; }
    public void Invoke(string owner) { Callbacks++; CallbackOwner = owner; }
    public void Reset() { Initializations = 0; Callbacks = 0; InitializedOwner = null; CallbackOwner = null; }
    public Task FallbackEntered => fallbackEntered?.Task ?? Task.CompletedTask;
    public void HoldFallback()
    {
        fallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fallbackRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    public async Task AwaitFallbackReleaseAsync()
    {
        fallbackEntered?.TrySetResult();
        if (fallbackRelease is not null) await fallbackRelease.Task;
    }
    public void ReleaseFallback() => fallbackRelease?.TrySetResult();
}
