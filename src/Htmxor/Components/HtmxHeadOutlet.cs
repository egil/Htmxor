using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

/// <summary>
/// Adds Htmxor's browser adapter to the document head.
/// The application supplies and configures its own htmx runtime.
/// </summary>
public class HtmxHeadOutlet : IComponent
{
	/// <inheritdoc/>
	public void Attach(RenderHandle renderHandle)
	{
		renderHandle.Render(builder => builder.AddMarkupContent(
			0,
			@"<script defer src=""_content/Htmxor/htmxor.js""></script>"));
	}

	/// <inheritdoc/>
	public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}
