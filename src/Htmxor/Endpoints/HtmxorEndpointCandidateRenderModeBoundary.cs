// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs | reimplements
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Endpoints;

internal sealed class HtmxorEndpointCandidateRenderModeBoundary(
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
	IComponentRenderMode renderMode) : IComponent
{
	private RenderHandle renderHandle;
	private IReadOnlyDictionary<string, object?>? parameters;

	public IComponentRenderMode RenderMode { get; } = renderMode;

	public void Attach(RenderHandle renderHandle) => this.renderHandle = renderHandle;

	public Task SetParametersAsync(ParameterView parameters)
	{
		this.parameters = parameters.ToDictionary();
		renderHandle.Render(Render);
		return Task.CompletedTask;
	}

	public HtmxorEndpointCandidateMarker CreateMarker(HttpContext context, int sequence, object? key)
	{
		var marker = new HtmxorEndpointCandidateMarker
		{
			Type = "server",
			PrerenderId = Guid.NewGuid().ToString("N"),
			Key = new HtmxorEndpointCandidateMarkerKey(
				ComputeTypeNameHash(componentType) + ":" + sequence.ToString(CultureInfo.InvariantCulture),
				key?.ToString() ?? string.Empty),
		};
		var (definitions, values) = HtmxorEndpointCandidateParameter.From(parameters!);
		var payload = new HtmxorEndpointCandidateServerComponent(
			sequence,
			marker.Key,
			componentType.Assembly.GetName().Name!,
			componentType.FullName!,
			definitions,
			values,
			Guid.NewGuid());
		var protector = context.RequestServices.GetRequiredService<IDataProtectionProvider>()
			.CreateProtector("Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1")
			.ToTimeLimitedDataProtector();
		marker.Sequence = sequence;
		marker.Descriptor = Convert.ToBase64String(protector.Protect(JsonSerializer.SerializeToUtf8Bytes(payload), TimeSpan.FromMinutes(5)));
		return marker;
	}

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
