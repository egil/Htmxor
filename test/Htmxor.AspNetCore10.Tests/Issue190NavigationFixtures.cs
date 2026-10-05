namespace Htmxor.AspNetCore10;

// Shared by Issue190NavigationParityTests (net10.0 only) and #266's paired redirect-representation cases (both
// targets). These types live apart from Issue190NavigationParityTests.cs so net11.0 can compile them and the two
// Issue190 navigation pages without that file's Issue187ParityHost dependency (session, authentication, forms,
// TempData); the csproj's net11.0 Compile/RazorComponent allow-list names this file and both pages.
internal static class Issue190ExternalNavigationContract
{
	public const string Destination = "https://example.invalid/issue-190/external-destination?source=navigation";
	public const string Path = "/issue-190/navigate-external";
}

internal sealed class Issue190LifecycleJournal
{
	private readonly List<string> events = [];

	public IReadOnlyList<string> Events => events;

	public void Record(string value) => events.Add(value);
}
