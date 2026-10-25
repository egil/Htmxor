using Microsoft.AspNetCore.Components;
namespace Htmxor.AspNetCore10.Issue362.Issue310.Plain;

public sealed class GateChild : ComponentBase
{
	[Parameter] public EventCallback<Htmxor.HtmxEventArgs> onput { get; set; }

}
