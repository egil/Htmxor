namespace Htmxor;

// HtmxRoute accepts every template stock @page accepts. Stock's route analyzer (ASP0017) reports grammar errors in
// the [StringSyntax("Route")] template, and the runtime catalog validates it with stock routing. This guard covers
// the one rule stock applies outside both: Razor rejects an @page template without a leading '/' (RZ9988), but
// Razor never inspects HtmxRoute, and the stock route parser accepts a relative template.
internal static class HtmxorRouteTemplateContract
{
	public const string MissingLeadingSlashReason =
		"The @page directive must specify a route template. The route template must be enclosed in quotes and begin " +
		"with the '/' character.";

	public static bool HasLeadingSlash(string template)
		=> template.Length > 0 && template[0] == '/';
}
