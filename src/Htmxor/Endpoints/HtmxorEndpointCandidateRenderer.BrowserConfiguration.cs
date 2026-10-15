#if NET11_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v11.0.0-rc.2.26504.105 at
// 820230c386db5b0106c5430b3e5aee92a7826bf5, synchronized 2026-09-13.
// Exact source inventory: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | reimplements

// Shipped VMR baselines re-reviewed 2026-10-15 for #329; source deltas: docs/engineering/patched-framework-baselines.md.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private static readonly Dictionary<string, string> browserEnvironmentVariables = GetWebAssemblyEnvironmentVariables();

	private void EmitBrowserConfigurationOnce(TextWriter output)
	{
		if (browserSettingsEmitted)
		{
			return;
		}

		browserSettingsEmitted = true;
		var options = BrowserOptions.GetBrowserOptions(httpContext);
		options.InteractiveWebAssembly.EnvironmentName ??= services.GetRequiredService<IHostEnvironment>().EnvironmentName;
		foreach (var (name, value) in browserEnvironmentVariables)
		{
			options.InteractiveWebAssembly.EnvironmentVariables.TryAdd(name, value);
		}

		var json = JsonSerializer.Serialize(options, HtmxorEndpointCandidateJson.Options);
		output.Write("<!--Blazor-Configuration:");
		output.Write(Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
		output.Write("-->");
	}
}
#endif
