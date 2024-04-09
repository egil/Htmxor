using Htmxor.Http;

namespace Htmxor;

public class RequestRoutingTest : TestAppTestBase
{
    public RequestRoutingTest(TestAppFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task NormalRequest()
    {
        await Host.Scenario(s =>
        {
            s.Get.Url("/normal-and-hx");
            s.ContentShouldBeHtml("""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <base href="/">
                    <link rel="stylesheet" href="app.css">
                    <link rel="stylesheet" href="Htmxor.TestApp.styles.css">
                    <script defer src="/js/htmx.min.js"></script>
                    <script defer src="_content/Htmxor/htmxor.js"></script>
                    <meta name="htmx-config" content="{&quot;selfRequestsOnly&quot;:true,&quot;antiforgery&quot;:{&quot;formFieldName&quot;:&quot;__RequestVerificationToken&quot;,&quot;headerName&quot;:&quot;RequestVerificationToken&quot;,&quot;cookieName&quot;:&quot;HX-XSRF-TOKEN&quot;}}">
                    <title>Home</title>
                </head>
                <body>
                    <h1>Hello, world!</h1>
                </body>
                </html>
                """);
        });
    }

    [Fact]
    public async Task HxRequest()
    {
        await Host.Scenario(s =>
        {
            s.Get.Url("/normal-and-hx");
            s.WithHxHeaders();
            s.ContentShouldBeHtml("""
                <h1>Hello, world!</h1>
                """);
        });
    }
}
