namespace Htmxor.Explicit.Anywhere;

public partial class Issue180Behind
{
	[Microsoft.AspNetCore.Components.Inject] public Issue180Probe Probe { get; set; } = default!;
	[Microsoft.AspNetCore.Components.Parameter] public int Id { get; set; }
	public override Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { Probe.Bind(); return base.SetParametersAsync(parameters); }
	private readonly string owner = Guid.NewGuid().ToString("N");
	private string result = "GET";
	private int callbacks;
	protected override void OnInitialized() => Probe.Initialize();
	private void Invoke(string method) { callbacks++; result = method; Probe.Invoke(owner); }
	[Htmxor.HtmxAction("pOsT")] private void Post() => Invoke("POST");
	[Htmxor.HtmxAction("put")] private void Put(Htmxor.HtmxEventArgs args) => Invoke(args.Request.Method);
	[Htmxor.HtmxAction("PATCH")] private async Task Patch() { await Task.Yield(); Invoke("PATCH"); }
	[Htmxor.HtmxAction("DELETE")][Htmxor.HtmxAction("QUERY")] private async Task Delete(Htmxor.HtmxEventArgs args) { await Task.Yield(); Invoke(args.Request.Method); }
}
