using System.ComponentModel.DataAnnotations;
namespace Htmxor.AspNetCore10.Issue362.Issue179;

public sealed class Issue179Probe
{
	public int Binding { get; set; }
	public int Lifecycle { get; set; }
	public int Callback { get; set; }
	public string? CallbackOwner { get; set; }
	public void Reset() { Binding = 0; Lifecycle = 0; Callback = 0; CallbackOwner = null; }
}
public sealed class Issue179Model
{
	[Required] public string Name { get; set; } = "";
}
