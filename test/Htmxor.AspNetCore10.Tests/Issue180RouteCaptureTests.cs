using System.Net;
using System.Text.RegularExpressions;
using Htmxor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Htmxor.Explicit.Anywhere;

public sealed class Issue180RouteCaptureTests
{
    private const string LayoutPath = "/issue-180/layout/42?q=target";
    private static readonly string PageType = typeof(Issue180LayoutPage).FullName!;

    [Fact]
    public async Task Standard_layout_keeps_original_route_observations_and_dispatches_to_body_page()
    {
        await using var stock = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: true);
        var stockGet = await GetCredentialsAsync(stock.Client, LayoutPath);
        var enhancedGet = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", enhancedGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"route-layout\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"route-layout\"", enhancedGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"layout-inline\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"layout-inline\"", enhancedGet.Body, StringComparison.Ordinal);
        Assert.Equal(2, stock.Probe.Initializations.Count);
        Assert.Equal(2, enhanced.Probe.Initializations.Count);

        enhanced.Probe.Reset();
        using var post = ActionRequest(LayoutPath, enhancedGet, authenticated: true, direct: false);
        using var response = await enhanced.Client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"route-layout\"", body, StringComparison.Ordinal);
        Assert.Equal(2, enhanced.Probe.Initializations.Count);
        Assert.Equal("layout-inline", enhanced.Probe.Initializations[0].Placement);
        var inline = Assert.Single(enhanced.Probe.Initializations, item => item.Placement == "layout-inline");
        var routed = Assert.Single(enhanced.Probe.Initializations, item => item.Placement == "routed");
        Assert.Equal(42, routed.Id);
        Assert.Equal("target", routed.Query);
        Assert.NotEqual(inline.Owner, routed.Owner);
        var callback = Assert.Single(enhanced.Probe.Callbacks);
        Assert.True(callback.Placement == "routed", $"Expected routed owner {routed.Owner}; callback targeted {callback.Placement} owner {callback.Owner}, Id {callback.Id}, query {callback.Query}.");
        Assert.Equal(routed.Owner, callback.Owner);
        Assert.Contains($"data-role=\"layout-inline\" data-owner=\"{inline.Owner}\" data-id=\"42\" data-query=\"target\" data-result=\"GET\"", body, StringComparison.Ordinal);
        Assert.Contains($"data-role=\"routed\" data-owner=\"{routed.Owner}\" data-id=\"42\" data-query=\"target\" data-result=\"POST\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Declined_authorization_never_dispatches_an_action()
    {
        await using var stock = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: true);
        var credentials = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        using var stockGet = await stock.Client.GetAsync(LayoutPath);
        using var enhancedGet = await enhanced.Client.GetAsync(LayoutPath);
        Assert.Equal(stockGet.StatusCode, enhancedGet.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, enhancedGet.StatusCode);

        enhanced.Probe.Reset();
        using var post = ActionRequest(LayoutPath, credentials, authenticated: false, direct: false);
        using var response = await enhanced.Client.SendAsync(post);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(enhanced.Probe.Callbacks);
    }

    [Fact]
    public async Task Delayed_component_authorization_dispatches_once_to_the_routed_page_after_release()
    {
        const string path = LayoutPath;
        var gate = new Issue180AuthorizationGate();
        await using var stock = await RouteHost<Issue180DelayedAuthorizationApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180DelayedAuthorizationApp>.StartAsync(enhanced: true, gate);
        var stockGet = await GetCredentialsAsync(stock.Client, path);
        var credentials = await GetCredentialsAsync(enhanced.Client, path);
        Assert.Contains($"data-found-type=\"{PageType}\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains($"data-found-type=\"{PageType}\"", credentials.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"routed\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"routed\"", credentials.Body, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        gate.Hold();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: false);
        var responseTask = enhanced.Client.SendAsync(post);
        try
        {
            await gate.Entered.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(responseTask.IsCompleted, "The response completed while component authorization was held.");
            Assert.Empty(enhanced.Probe.Callbacks);
        }
        finally { gate.Release(); }
        using var response = await responseTask;
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var routed = Assert.Single(enhanced.Probe.Initializations, item => item.Placement == "routed");
        var callback = Assert.Single(enhanced.Probe.Callbacks);
        Assert.Equal(routed.Owner, callback.Owner);
        Assert.Equal(42, callback.Id);
        Assert.Equal("target", callback.Query);
        Assert.Contains($"data-role=\"routed\" data-owner=\"{routed.Owner}\" data-id=\"42\" data-query=\"target\" data-result=\"POST\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Default_layout_with_nested_layout_keeps_stock_markers_and_routed_owner()
    {
        const string path = "/issue-180/default/42";
        await using var stock = await RouteHost<Issue180DefaultLayoutApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180DefaultLayoutApp>.StartAsync(enhanced: true);
        var stockGet = await GetCredentialsAsync(stock.Client, path);
        var credentials = await GetCredentialsAsync(enhanced.Client, path);
        Assert.Contains("data-layout=\"outer\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"inner\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"outer\"", credentials.Body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"inner\"", credentials.Body, StringComparison.Ordinal);
        Assert.Contains("data-found-id=\"42\"", credentials.Body, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: false);
        using var response = await enhanced.Client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var routed = Assert.Single(enhanced.Probe.Initializations);
        var callback = Assert.Single(enhanced.Probe.Callbacks);
        Assert.Equal(routed.Owner, callback.Owner);
        Assert.Contains("data-layout=\"outer\"", body, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"inner\"", body, StringComparison.Ordinal);
        Assert.Contains($"data-owner=\"{routed.Owner}\" data-id=\"42\" data-result=\"POST\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Configured_component_activator_preserves_original_routed_instance()
    {
        const string path = "/issue-180/default/42";
        var activation = new Issue180ActivationProbe();
        await using var enhanced = await RouteHost<Issue180DefaultLayoutApp>.StartAsync(enhanced: true, activation: activation);
        var credentials = await GetCredentialsAsync(enhanced.Client, path);
        Assert.Contains("data-layout=\"inner\"", credentials.Body, StringComparison.Ordinal);

        activation.Reset();
        enhanced.Probe.Reset();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: false);
        using var response = await enhanced.Client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(activation.Created.OfType<Router>());
        Assert.Single(activation.Created.OfType<RouteView>());
        Assert.Contains(activation.Created, component => component is LayoutView);
        var page = Assert.Single(activation.Created.OfType<Issue180DefaultLayoutPage>());
        var initialized = Assert.Single(enhanced.Probe.Initializations);
        var callback = Assert.Single(enhanced.Probe.Callbacks);
        Assert.Equal(page.InstanceId, initialized.Owner);
        Assert.Equal(page.InstanceId, callback.Owner);
        Assert.Contains($"data-owner=\"{page.InstanceId}\" data-id=\"42\" data-result=\"POST\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Selected_route_without_a_rendered_page_fails_without_inline_callback()
    {
        await using var stock = await RouteHost<Issue180NoPageApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180NoPageApp>.StartAsync(enhanced: true);
        var stockGet = await GetCredentialsAsync(stock.Client, LayoutPath);
        var credentials = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found=\"without-route-view\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found=\"without-route-view\"", credentials.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("data-role=\"routed\"", credentials.Body, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(LayoutPath, credentials, authenticated: true, direct: false);
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () => response = await enhanced.Client.SendAsync(post));
        Assert.Equal(0, enhanced.Probe.Initializations.Count(item => item.Placement == "routed"));
        Assert.Empty(enhanced.Probe.Callbacks);
        AssertClearFailure(failure, response, "Missing routed page");
    }

    [Fact]
    public async Task Two_route_views_fail_clearly_without_arbitrary_callback()
    {
        await using var stock = await RouteHost<Issue180AmbiguousApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180AmbiguousApp>.StartAsync(enhanced: true);
        var stockGet = await GetCredentialsAsync(stock.Client, LayoutPath);
        var credentials = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        Assert.Equal(2, Regex.Matches(stockGet.Body, "data-role=\"routed\"").Count);
        Assert.Equal(2, Regex.Matches(credentials.Body, "data-role=\"routed\"").Count);
        Assert.Contains($"data-found-type=\"{PageType}\"", credentials.Body, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(LayoutPath, credentials, authenticated: true, direct: false);
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () => response = await enhanced.Client.SendAsync(post));
        Assert.Equal(2, enhanced.Probe.Initializations.Count(item => item.Placement == "routed"));
        Assert.Empty(enhanced.Probe.Callbacks);
        AssertClearFailure(failure, response, "Two routed pages");
    }

    [Fact]
    public async Task Declined_route_view_with_nested_fake_page_keeps_not_authorized_content_without_callback()
    {
        await using var stock = await RouteHost<Issue180CascadeDeniedApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180CascadeDeniedApp>.StartAsync(enhanced: true);
        var stockGet = await GetCredentialsAsync(stock.Client, LayoutPath);
        var credentials = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", credentials.Body, StringComparison.Ordinal);
        Assert.Contains("data-auth=\"declined\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-auth=\"declined\"", credentials.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"routed\"", stockGet.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"routed\"", credentials.Body, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(LayoutPath, credentials, authenticated: true, direct: false);
        using var response = await enhanced.Client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-auth=\"declined\"", body, StringComparison.Ordinal);
        Assert.Empty(enhanced.Probe.Callbacks);
    }

    [Fact]
    public async Task Authorized_route_view_whose_layout_omits_body_fails_without_callback()
    {
        const string path = LayoutPath + "&noBody=true";
        await using var stock = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: true);
        var credentials = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        var stockBody = await GetAuthenticatedBodyAsync(stock.Client, path);
        var enhancedBody = await GetAuthenticatedBodyAsync(enhanced.Client, path);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", stockBody, StringComparison.Ordinal);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", enhancedBody, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"route-layout\"", stockBody, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"route-layout\"", enhancedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("data-auth=\"declined\"", enhancedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("data-role=\"routed\"", stockBody, StringComparison.Ordinal);
        Assert.DoesNotContain("data-role=\"routed\"", enhancedBody, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: false);
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () => response = await enhanced.Client.SendAsync(post));
        Assert.Equal(0, enhanced.Probe.Initializations.Count(item => item.Placement == "routed"));
        Assert.Empty(enhanced.Probe.Callbacks);
        AssertClearFailure(failure, response, "Authorized layout omitted the routed page");
    }

    [Fact]
    public async Task Declined_route_view_whose_layout_omits_body_keeps_denial_without_callback()
    {
        const string path = LayoutPath + "&noBody=true";
        await using var stock = await RouteHost<Issue180CascadeDeniedApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180CascadeDeniedApp>.StartAsync(enhanced: true);
        var credentials = await GetCredentialsAsync(enhanced.Client, LayoutPath);
        var stockBody = await GetAuthenticatedBodyAsync(stock.Client, path);
        var enhancedBody = await GetAuthenticatedBodyAsync(enhanced.Client, path);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", stockBody, StringComparison.Ordinal);
        Assert.Contains($"data-found-type=\"{PageType}\" data-found-id=\"42\"", enhancedBody, StringComparison.Ordinal);
        Assert.Contains("data-auth=\"declined\"", stockBody, StringComparison.Ordinal);
        Assert.Contains("data-auth=\"declined\"", enhancedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("data-role=\"routed\"", stockBody, StringComparison.Ordinal);
        Assert.DoesNotContain("data-role=\"routed\"", enhancedBody, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: false);
        using var response = await enhanced.Client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-auth=\"declined\"", body, StringComparison.Ordinal);
        Assert.Equal(0, enhanced.Probe.Initializations.Count(item => item.Placement == "routed"));
        Assert.Empty(enhanced.Probe.Callbacks);
    }

    [Fact]
    public async Task One_route_view_whose_layout_renders_body_twice_fails_without_arbitrary_callback()
    {
        const string path = LayoutPath + "&doubleBody=true";
        await using var stock = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: false);
        await using var enhanced = await RouteHost<Issue180LayoutApp>.StartAsync(enhanced: true);
        var stockGet = await GetCredentialsAsync(stock.Client, path);
        var credentials = await GetCredentialsAsync(enhanced.Client, path);
        Assert.Equal(2, Regex.Matches(stockGet.Body, "data-role=\"routed\"").Count);
        Assert.Equal(2, Regex.Matches(credentials.Body, "data-role=\"routed\"").Count);
        Assert.Single(Regex.Matches(credentials.Body, "data-found-type="));
        Assert.Single(Regex.Matches(credentials.Body, "data-layout=\"route-layout\""));

        enhanced.Probe.Reset();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: false);
        HttpResponseMessage? response = null;
        var failure = await Record.ExceptionAsync(async () => response = await enhanced.Client.SendAsync(post));
        Assert.Equal(2, enhanced.Probe.Initializations.Count(item => item.Placement == "routed"));
        Assert.Empty(enhanced.Probe.Callbacks);
        AssertClearFailure(failure, response, "Repeated route-view body");
    }

    [Fact]
    public async Task Direct_route_dispatches_to_its_selected_page_not_nested_copy()
    {
        await using var enhanced = await RouteHost<Issue180DuplicateApp>.StartAsync(enhanced: true);
        const string path = "/issue-180/duplicate/42?q=target";
        var credentials = await GetCredentialsAsync(enhanced.Client, path, direct: true);
        Assert.Contains("data-role=\"routed\"", credentials.Body, StringComparison.Ordinal);
        Assert.Contains("data-role=\"nested-equal\"", credentials.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("data-role=\"before-seven\"", credentials.Body, StringComparison.Ordinal);

        enhanced.Probe.Reset();
        using var post = ActionRequest(path, credentials, authenticated: true, direct: true);
        using var response = await enhanced.Client.SendAsync(post);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, enhanced.Probe.Initializations.Count);
        var routed = Assert.Single(enhanced.Probe.Initializations, item => item.Placement == "routed");
        var nested = Assert.Single(enhanced.Probe.Initializations, item => item.Placement == "nested-equal");
        Assert.NotEqual(routed.Owner, nested.Owner);
        var callback = Assert.Single(enhanced.Probe.Callbacks);
        Assert.Equal("routed", callback.Placement);
        Assert.Equal(routed.Owner, callback.Owner);
        Assert.Equal(42, callback.Id);
        Assert.Equal("target", callback.Query);
        Assert.Contains($"data-role=\"routed\" data-owner=\"{routed.Owner}\" data-id=\"42\" data-result=\"POST\"", body, StringComparison.Ordinal);
        Assert.Contains($"data-role=\"nested-equal\" data-owner=\"{nested.Owner}\" data-id=\"42\" data-result=\"GET\"", body, StringComparison.Ordinal);
    }

    private static async Task<Credentials> GetCredentialsAsync(HttpClient client, string path, bool direct = false)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, path);
        get.Headers.Add("X-Issue180-User", "Ada");
        if (direct) AddDirectHeaders(get);
        using var response = await client.SendAsync(get);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = Regex.Match(body, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0];
        return new(body, cookie, WebUtility.HtmlDecode(token));
    }

    private static async Task<string> GetAuthenticatedBodyAsync(HttpClient client, string path)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, path);
        get.Headers.Add("X-Issue180-User", "Ada");
        using var response = await client.SendAsync(get);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static HttpRequestMessage ActionRequest(string path, Credentials credentials, bool authenticated, bool direct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent([]) };
        if (authenticated) request.Headers.Add("X-Issue180-User", "Ada");
        request.Headers.Add("Cookie", credentials.Cookie);
        request.Headers.Add("RequestVerificationToken", credentials.Token);
        if (direct) AddDirectHeaders(request);
        return request;
    }

    private static void AddDirectHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("HX-Request", "true");
        request.Headers.Add("HX-Request-Type", "partial");
    }

    private sealed record Credentials(string Body, string Cookie, string Token);

    private static void AssertClearFailure(Exception? failure, HttpResponseMessage? response, string scenario)
    {
        var status = (int?)response?.StatusCode;
        response?.Dispose();
        Assert.True(failure is InvalidOperationException || status >= 500,
            $"{scenario} must fail clearly without a write; failure={failure?.GetType().Name}, HTTP={status}.");
    }

    private sealed class RouteHost<TRoot> : IAsyncDisposable where TRoot : IComponent
    {
        private readonly WebApplication app;
        public HttpClient Client { get; }
        public Issue180DuplicateProbe Probe { get; }

        private RouteHost(WebApplication app, Issue180DuplicateProbe probe)
        {
            this.app = app;
            Probe = probe;
            Client = app.GetTestClient();
        }

        public static async Task<RouteHost<TRoot>> StartAsync(bool enhanced,
            Issue180AuthorizationGate? gate = null, Issue180ActivationProbe? activation = null)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(TRoot).Assembly.GetName().Name });
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            var probe = new Issue180DuplicateProbe();
            builder.Services.AddSingleton(probe);
            builder.Services.AddAuthentication("issue180").AddScheme<AuthenticationSchemeOptions, Issue180AuthenticationHandler>("issue180", _ => { });
            builder.Services.AddAuthorization();
            if (gate is not null)
            {
                builder.Services.AddSingleton(gate);
                builder.Services.AddScoped<IAuthorizationService>(services =>
                    new Issue180GatedAuthorizationService(
                        ActivatorUtilities.CreateInstance<DefaultAuthorizationService>(services), gate));
            }
            if (activation is not null)
            {
                builder.Services.AddSingleton<IComponentActivator>(services =>
                    new Issue180RecordingActivator(services, activation));
            }
            builder.Services.AddCascadingAuthenticationState();
            if (enhanced) builder.Services.AddRazorComponents().AddHtmxor();
            else builder.Services.AddRazorComponents();
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            if (enhanced) app.MapRazorComponents<TRoot>().AddHtmxorEndpoints();
            else app.MapRazorComponents<TRoot>();
            await app.StartAsync();
            return new(app, probe);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }
}
