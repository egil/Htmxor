// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.11, commit a5383385245bdacc20ec19f30e46090a8154d8da,
// synchronized 2026-09-05. Exact sources and license: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/FormMapping/HttpContextFormDataProvider.cs | private-accesses
// Htmxor upstream dependency: src/Components/Endpoints/src/Forms/EndpointAntiforgeryStateProvider.cs | private-accesses
// Htmxor upstream dependency: src/Components/Endpoints/src/Builder/ConfiguredRenderModesMetadata.cs | private-accesses
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements

using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Htmxor.Endpoints;

internal sealed class HtmxorEndpointCandidateFormServices
{
	private const string EndpointNamespace = "Microsoft.AspNetCore.Components.Endpoints.";
	private static readonly Assembly EndpointAssembly = typeof(IRazorComponentEndpointInvoker).Assembly;
	private readonly Type formProviderType;
	private readonly Type antiforgeryProviderType;
	private readonly Type renderModesMetadataType;
	private readonly MethodInfo setFormData;
	private readonly MethodInfo setRequestContext;
	private readonly MethodInfo disableTokenGeneration;
	private readonly MethodInfo getConfiguredRenderModes;

	private HtmxorEndpointCandidateFormServices()
	{
		formProviderType = RequireType("HttpContextFormDataProvider");
		antiforgeryProviderType = RequireType("Forms.EndpointAntiforgeryStateProvider");
		renderModesMetadataType = RequireType("ConfiguredRenderModesMetadata");
		setFormData = RequireMethod(formProviderType, "SetFormData", BindingFlags.Public, typeof(void),
			typeof(string), typeof(IReadOnlyDictionary<string, StringValues>), typeof(IFormFileCollection));
		setRequestContext = RequireMethod(antiforgeryProviderType, "SetRequestContext", BindingFlags.NonPublic,
			typeof(void), typeof(HttpContext));
		disableTokenGeneration = RequireMethod(antiforgeryProviderType, "DisableTokenGeneration", BindingFlags.NonPublic,
			typeof(void));
		getConfiguredRenderModes = RequireMethod(renderModesMetadataType, "get_ConfiguredRenderModes", BindingFlags.Public,
			typeof(IComponentRenderMode[]));
		if (renderModesMetadataType.GetProperty("ConfiguredRenderModes", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)?.GetMethod != getConfiguredRenderModes ||
			!setRequestContext.IsAssembly || !disableTokenGeneration.IsAssembly ||
			!typeof(AntiforgeryStateProvider).IsAssignableFrom(antiforgeryProviderType))
		{
			throw IncompatibleFramework("ConfiguredRenderModes property getter, internal antiforgery methods, or AntiforgeryStateProvider base type");
		}
	}

	// Registration validates shapes before any request can enter this candidate. Only metadata is cached.
	internal static HtmxorEndpointCandidateFormServices Create() => new();

	internal void Initialize(HttpContext context, string? handler, IFormCollection? form)
	{
		if (handler is not null && form is not null)
		{
			Invoke(setFormData, context.RequestServices.GetRequiredService(formProviderType),
				[handler, new FormEntries(form), form.Files]);
		}

		if (context.RequestServices.GetService<AntiforgeryStateProvider>() is { } provider &&
			antiforgeryProviderType.IsInstanceOfType(provider))
		{
			Invoke(setRequestContext, provider, [context]);
		}
	}

	internal void DisableTokenGenerationForCompletedResponse(HttpContext context, Endpoint endpoint)
	{
		// GetMetadata<T> selects the last assignable entry. Absent metadata does not mean an empty list.
		var metadata = endpoint.Metadata.LastOrDefault(renderModesMetadataType.IsInstanceOfType);
		if (metadata is not null && ((IComponentRenderMode[])Invoke(getConfiguredRenderModes, metadata, null)!).Length == 0)
		{
			var provider = context.RequestServices.GetRequiredService<AntiforgeryStateProvider>();
			if (antiforgeryProviderType.IsInstanceOfType(provider))
			{
				Invoke(disableTokenGeneration, provider, null);
			}
		}
	}

	private static Type RequireType(string name)
		=> EndpointAssembly.GetType(EndpointNamespace + name, throwOnError: false)
			?? throw IncompatibleFramework(EndpointNamespace + name);

	private static MethodInfo RequireMethod(
		Type type, string name, BindingFlags visibility, Type returnType, params Type[] parameterTypes)
	{
		var method = type.GetMethod(name, visibility | BindingFlags.Instance | BindingFlags.DeclaredOnly,
			binder: null, parameterTypes, modifiers: null);
		if (method is null || method.ReturnType != returnType || method.IsGenericMethod ||
			!method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes))
		{
			throw IncompatibleFramework($"{type.FullName}.{name}({string.Join(", ", parameterTypes.Select(type => type.FullName))}) -> {returnType.FullName}");
		}

		return method;
	}

	private static InvalidOperationException IncompatibleFramework(string dependency)
		=> new($"The inactive Htmxor form-service adapter is incompatible with installed '{EndpointAssembly.FullName}': expected {dependency}. Baseline: ASP.NET Core v10.0.11, commit a5383385245bdacc20ec19f30e46090a8154d8da. Review the upstream dependency and renew paired parity evidence before using this candidate.");

	private static object? Invoke(MethodInfo method, object target, object?[]? arguments)
		=> method.Invoke(target, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);

	private sealed class FormEntries(IFormCollection form) : IReadOnlyDictionary<string, StringValues>
	{
		public StringValues this[string key] => form[key];
		public IEnumerable<string> Keys => form.Keys;
		public IEnumerable<StringValues> Values => form.Keys.Select(key => form[key]);
		public int Count => form.Count;
		public bool ContainsKey(string key) => form.ContainsKey(key);
		public bool TryGetValue(string key, out StringValues value) => form.TryGetValue(key, out value);
		public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => form.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => form.GetEnumerator();
	}
}
