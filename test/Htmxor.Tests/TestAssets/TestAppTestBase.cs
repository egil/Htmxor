namespace Htmxor.TestAssets;

public class TestAppTestBase : IClassFixture<TestAppFixture>
{
    public IAlbaHost Host { get; private set; }

    protected TestAppTestBase(TestAppFixture fixture)
    {
        Host = fixture.Host;
    }

    public static string FullPageContent(string bodyInnerHtml, string? title = null)
    {
        title = title is not null ? $"<title>{title}</title>" : "";
        return $$$"""
            <!DOCTYPE html>
            <html lang="en"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <base href="/">
                <link rel="stylesheet" href="app.css">
                <link rel="stylesheet" href="Htmxor.TestApp.styles.css">
                <meta name="htmx-config" content="{&quot;selfRequestsOnly&quot;:true,&quot;antiforgery&quot;:{&quot;formFieldName&quot;:&quot;__RequestVerificationToken&quot;,&quot;headerName&quot;:&quot;RequestVerificationToken&quot;,&quot;cookieName&quot;:&quot;HX-XSRF-TOKEN&quot;}}">
                <script defer src="_content/Htmxor/htmx/htmx.min.js"></script>
                <script defer src="_content/Htmxor/htmx/event-header.js"></script>
                <script defer src="_content/Htmxor/htmxor.js"></script>
                {{{title}}}
            </head>
            <body>
                {{{bodyInnerHtml}}}
            </body>
            </html>
            """;
    }
}
