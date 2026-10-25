using Microsoft.AspNetCore.Components;
namespace Htmxor.AspNetCore10.Issue362.Issue310.Generic;

public sealed class GateChild<T> : ComponentBase
{
	[Parameter] public EventCallback<Htmxor.HtmxEventArgs> onput { get; set; }
	[Parameter] public T Value { get; set; } = default!;
}
