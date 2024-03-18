namespace Htmxor.TestAssets;

public class TestAppTestBase : IClassFixture<TestAppFixture>
{
    public IAlbaHost Host { get; private set; }

    protected TestAppTestBase(TestAppFixture fixture)
    {
        Host = fixture.Host;
    }

    public static string FullPageContent(string bodyInnerHtml, string? title = null) => $"""
        <!DOCTYPE html>
        <html lang="en"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <base href="/">
            <link rel="stylesheet" href="app.css">
            <link rel="stylesheet" href="Htmxor.TestApp.styles.css">
            <script defer src="/js/htmx.min.js"></script>
            <script defer src="_content/Htmxor/htmxor.js"></script>
            {(title is not null ? $"<title>{title}</title>" : "")}
        </head>
        <body>
            {bodyInnerHtml}
        </body>
        </html>
        """;
}