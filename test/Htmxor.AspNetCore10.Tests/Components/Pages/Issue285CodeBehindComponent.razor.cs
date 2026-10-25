using Htmxor;
using Microsoft.AspNetCore.Components;

namespace Htmxor.AspNetCore10.Components.Pages;

[HtmxRoute("/issue-362-285/code-behind/{Id:int}", Methods = ["GET", "PATCH"])]
public partial class Issue285CodeBehindComponent
{
	public static int PatchCount;

	[Parameter]
	public int Id { get; set; }

	private void Patch(HtmxEventArgs _) => PatchCount++;
}
