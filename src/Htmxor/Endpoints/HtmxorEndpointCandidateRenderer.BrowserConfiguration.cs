#if NET11_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v11.0.0-rc.1.26425.128 at
// c3325eeb6b47bc6383c127d4f4827dc9642a2b6e, synchronized 2026-09-13.
// Exact source inventory: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | reimplements

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
