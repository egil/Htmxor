using Microsoft.AspNetCore.Routing;
using Htmxor;

namespace Htmxor.AspNetCore10.Issue362.Routing;

[HtmxRoute(
	"/issue-362/htmx-reports/{ReportId:int}",
	Methods = ["GET", "PATCH"],
	CurrentUrl = "/orders",
	Target = "section#patch-result",
	Targets = ["section#patch-result", "div#fallback"])]
[Host("reports.internal.example")]
public partial class Issue97ReportComponent
{
	private void PatchReport(HtmxEventArgs _)
	{
		RequestProbe.RecordCallback();
		callbackObservation =
			$"patch|{ReportId}|{Source}|{HttpContext.User.Identity?.Name}|{RequestProbe.Value}|{lifecycleReady.ToString().ToLowerInvariant()}";
	}
}
