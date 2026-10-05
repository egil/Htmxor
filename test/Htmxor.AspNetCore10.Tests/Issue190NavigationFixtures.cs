namespace Htmxor.AspNetCore10;

// Extracted from Issue190NavigationParityTests.cs (unchanged otherwise) so #266's own paired
// redirect-representation cases can reuse the same external-navigation contract and lifecycle
// journal on net11.0 too, without widening Issue190NavigationParityTests.cs's much larger
// Issue187ParityHost dependency (session, authentication, forms, TempData) to that target. See
// the csproj's net11.0 Compile/RazorComponent Include for the explicit allow-list this file and
// Issue190ExternalNavigationPage.razor/Issue190NavigationPage.razor join.
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
