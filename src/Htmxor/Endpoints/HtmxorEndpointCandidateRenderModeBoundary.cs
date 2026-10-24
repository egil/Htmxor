// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/ComponentMarker.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/ServerComponentSerializer.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/WebAssemblyComponentSerializer.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/WebAssemblyComponentSerializationSettings.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/ServerComponentSerializationSettings.cs | reimplements
// Htmxor upstream dependency: src/Shared/Components/ComponentParameter.cs | mirrors
// Htmxor upstream dependency: src/Shared/Components/ServerComponent.cs | mirrors
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.13, commit 4099f83977f4d29604a9aeb971fb0a7bc1d5f699,
// synchronized 2026-09-06. Exact source inventory: docs/engineering/candidate-form-adapter.md.
// The marker payload serializer options (#275, synchronized 2026-10-04) reimplement
// WebAssemblyComponentSerializationSettings.cs and ServerComponentSerializationSettings.cs, whose naming and null
// handling are identical at v10.0.13 (4099f83977f4d29604a9aeb971fb0a7bc1d5f699) and v11.0.0-rc.2.26504.105
// (820230c386db5b0106c5430b3e5aee92a7826bf5). NET11 adds a server source-generated resolver and freezes
// WebAssembly options at creation; this adapter freezes its private options on first serialization:
// https://github.com/dotnet/dotnet/blob/v10.0.13/src/aspnetcore/src/Shared/Components/WebAssemblyComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/aspnetcore/src/Shared/Components/WebAssemblyComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/v11.0.0-rc.2.26504.105/src/aspnetcore/src/Shared/Components/WebAssemblyComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Shared/Components/WebAssemblyComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/v10.0.13/src/aspnetcore/src/Shared/Components/ServerComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/aspnetcore/src/Shared/Components/ServerComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/v11.0.0-rc.2.26504.105/src/aspnetcore/src/Shared/Components/ServerComponentSerializationSettings.cs
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Shared/Components/ServerComponentSerializationSettings.cs

// CacheBehavior.Rerender follows SSRRenderModeBoundary.cs at v11.0.0-rc.2.26504.105,
// commit 820230c386db5b0106c5430b3e5aee92a7826bf5, synchronized 2026-10-24 for #237.
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs

// Shipped VMR baselines re-reviewed 2026-10-15 for #329; source deltas: docs/engineering/patched-framework-baselines.md.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;

namespace Htmxor.Endpoints;

#if NET11_0_OR_GREATER
[CacheBehavior(CacheBehavior.Rerender)]
#endif
internal sealed class HtmxorEndpointCandidateRenderModeBoundary(
	HttpContext context,
	HtmxorEndpointCandidateFormServices formServices,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
	IComponentRenderMode renderMode) : IComponent
{
	private RenderHandle renderHandle;
	private IReadOnlyDictionary<string, object?>? parameters;
	private readonly bool prerender = GetPrerender(renderMode);

	public IComponentRenderMode RenderMode { get; } = ValidateConfiguration(context, formServices, componentType, renderMode)
		? renderMode
		: throw new InvalidOperationException("Render mode configuration validation unexpectedly returned false.");

	public void Attach(RenderHandle renderHandle) => this.renderHandle = renderHandle;

	public Task SetParametersAsync(ParameterView parameters)
	{
		this.parameters = parameters.ToDictionary();
		if (prerender)
		{
			renderHandle.Render(Render);
		}
		return Task.CompletedTask;
	}

	public HtmxorEndpointCandidateMarker CreateMarker(
		HttpContext context,
		int locationSequence,
		object? key,
		int invocationSequence,
		Guid invocationId)
	{
		var marker = new HtmxorEndpointCandidateMarker
		{
			Type = RenderMode switch
			{
				Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode => "server",
				Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode => "webassembly",
				Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode => "auto",
				_ => throw new InvalidOperationException($"Unsupported render mode '{RenderMode.GetType().FullName}'."),
			},
			PrerenderId = prerender ? Guid.NewGuid().ToString("N") : null,
			Key = new HtmxorEndpointCandidateMarkerKey(
				ComputeTypeNameHash(componentType) + ":" + locationSequence.ToString(CultureInfo.InvariantCulture),
				FormatComponentKey(key)),
		};
		var (definitions, values) = HtmxorEndpointCandidateParameter.From(parameters!);
		if (RenderMode is Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode or Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode)
		{
			var payload = new HtmxorEndpointCandidateServerComponent(
				invocationSequence, marker.Key, componentType.Assembly.GetName().Name!, componentType.FullName!, definitions, values, invocationId);
			var protector = context.RequestServices.GetRequiredService<IDataProtectionProvider>()
				.CreateProtector("Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1")
				.ToTimeLimitedDataProtector();
			marker.Sequence = invocationSequence;
			marker.Descriptor = Convert.ToBase64String(protector.Protect(
				JsonSerializer.SerializeToUtf8Bytes(payload, ServerComponentSerializationOptions), TimeSpan.FromMinutes(5)));
		}

		if (RenderMode is Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode or Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode)
		{
			marker.Assembly = componentType.Assembly.GetName().Name!;
			marker.TypeName = componentType.FullName!;
			marker.ParameterDefinitions = Convert.ToBase64String(
				JsonSerializer.SerializeToUtf8Bytes(definitions, WebAssemblyComponentSerializationOptions));
			marker.ParameterValues = Convert.ToBase64String(
				JsonSerializer.SerializeToUtf8Bytes(values, WebAssemblyComponentSerializationOptions));
		}
		return marker;
	}

	// Each instance reimplements its own separately watched stock settings class. They are identical today, but
	// .NET 11's ServerComponentSerializationSettings already adds a source-generated resolver, so drift in one must
	// be adoptable without touching the other. Matching them makes the client read the same payload stock writes.
	// PropertyNameCaseInsensitive affects only reads, which Htmxor never does; it keeps each copy faithful.
	private static readonly JsonSerializerOptions WebAssemblyComponentSerializationOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private static readonly JsonSerializerOptions ServerComponentSerializationOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private static bool GetPrerender(IComponentRenderMode renderMode)
		=> renderMode switch
		{
			Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode mode => mode.Prerender,
			Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode mode => mode.Prerender,
			Microsoft.AspNetCore.Components.Web.InteractiveAutoRenderMode mode => mode.Prerender,
			_ => throw new ArgumentException($"Server-side rendering does not support the render mode '{renderMode}'.", nameof(renderMode)),
		};

	private static bool ValidateConfiguration(HttpContext context, HtmxorEndpointCandidateFormServices formServices, Type componentType, IComponentRenderMode renderMode)
	{
		formServices.AssertRenderModeIsConfigured(context, componentType, renderMode);
		return true;
	}

	private static string FormatComponentKey(object? key)
		=> key switch
		{
			string value => value,
			IFormattable value => value.ToString(null, CultureInfo.InvariantCulture),
			_ => string.Empty,
		};

	private static string ComputeTypeNameHash(Type type)
		=> Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(type.FullName!)));

	private void Render(RenderTreeBuilder builder)
	{
		builder.OpenComponent(0, componentType);
		foreach (var (name, value) in parameters!)
		{
			builder.AddComponentParameter(1, name, value);
		}

		builder.CloseComponent();
	}
}

internal sealed class HtmxorEndpointCandidateMarker
{
	public string? Type { get; init; }
	public string? PrerenderId { get; init; }
	public HtmxorEndpointCandidateMarkerKey? Key { get; init; }
	public int? Sequence { get; set; }
	public string? Descriptor { get; set; }
	public string? Assembly { get; set; }
	public string? TypeName { get; set; }
	public string? ParameterDefinitions { get; set; }
	public string? ParameterValues { get; set; }
}

internal sealed record HtmxorEndpointCandidateMarkerKey(string LocationHash, string? FormattedComponentKey);

internal sealed record HtmxorEndpointCandidateParameter(string Name, string? TypeName, string? Assembly)
{
	public static (IList<HtmxorEndpointCandidateParameter>, IList<object?>) From(IReadOnlyDictionary<string, object?> values)
		=> (values.Select(pair => new HtmxorEndpointCandidateParameter(
			pair.Key,
			pair.Value?.GetType().FullName,
			pair.Value?.GetType().Assembly.GetName().Name)).ToList(), values.Values.ToList());
}

internal sealed record HtmxorEndpointCandidateServerComponent(
	int Sequence,
	HtmxorEndpointCandidateMarkerKey? Key,
	string AssemblyName,
	string TypeName,
	IList<HtmxorEndpointCandidateParameter> ParameterDefinitions,
	IList<object?> ParameterValues,
	Guid InvocationId);
