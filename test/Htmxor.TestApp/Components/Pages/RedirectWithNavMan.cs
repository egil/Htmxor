using Microsoft.AspNetCore.Components;

namespace Htmxor.TestApp.Components.Pages;

[Route("/redirect-with-navman")]
public class RedirectWithNavMan : ComponentBase
{
	[Inject]
	public required NavigationManager NavMan { get; set; }

	protected override void OnInitialized()
	{
		NavMan.NavigateTo("/");
	}
}
