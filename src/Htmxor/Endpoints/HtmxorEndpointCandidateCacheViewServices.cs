#if NET11_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v11.0.0-rc.1.26425.128, commit c3325eeb6b47bc6383c127d4f4827dc9642a2b6e,
// synchronized 2026-09-15. Approved #219 dependencies and exact sources: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/CacheView/CacheView.cs | private-accesses
// Htmxor upstream dependency: src/Components/Endpoints/src/CacheView/CacheViewService.cs | private-accesses
// Htmxor upstream dependency: src/Components/Shared/src/ComponentKeyHelper.cs | mirrors

using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Endpoints;

// Stock drives CacheView from EndpointComponentState and EndpointHtmlRenderer, both of which the candidate
// replaces, so a cached subtree is never captured and never reused. The component itself still owns resolution,
// storage, keys and serialization; only the two coordination points stock performs are restored here.
internal sealed class HtmxorEndpointCandidateCacheViewServices
{
	private static readonly Assembly EndpointAssembly = typeof(IRazorComponentEndpointInvoker).Assembly;
	private readonly Type serviceType;
	private readonly PropertyInfo renderState;
	private readonly PropertyInfo isInStreamingContext;
	private readonly PropertyInfo treePositionKeyFactory;
	private readonly PropertyInfo isCacheHit;
	private readonly MethodInfo throwIfNested;
	private readonly MethodInfo tryBeginWrite;
	private readonly MethodInfo endCapture;

	private HtmxorEndpointCandidateCacheViewServices()
	{
		serviceType = RequireInternalClass("CacheViewService");
		var renderStateType = RequireInternalClass("CacheViewRenderState");

		renderState = RequireProperty(typeof(CacheView), "RenderState", renderStateType, read: true);
		isInStreamingContext = RequireProperty(typeof(CacheView), "IsInStreamingContext", typeof(bool), read: false);
		treePositionKeyFactory = RequireProperty(typeof(CacheView), "TreePositionKeyFactory", typeof(Func<string>), read: false);
		isCacheHit = RequireProperty(renderStateType, "IsCacheHit", typeof(bool), read: true);

		throwIfNested = RequireMethod(serviceType, "ThrowIfNestedInsideCapturingCacheView",
			BindingFlags.Public | BindingFlags.Static, typeof(void), typeof(TextWriter));
		tryBeginWrite = RequireMethod(serviceType, "TryBeginWrite",
			BindingFlags.Public | BindingFlags.Static, typeof(bool),
			renderStateType, typeof(CacheView), typeof(TextWriter), typeof(TextWriter).MakeByRefType());
		endCapture = RequireMethod(serviceType, "EndCapture",
			BindingFlags.Public | BindingFlags.Instance, typeof(void), renderStateType, typeof(bool));
	}

	internal static HtmxorEndpointCandidateCacheViewServices Create() => new();

	// Stock supplies both from EndpointComponentState as the component state is created, before the component
	// resolves its cache entry. The tree position always contributes to the key, even with an explicit CacheKey.
	internal void Initialize(CacheView cacheView, bool isStreaming, Func<string> treePositionKey)
	{
		isInStreamingContext.SetValue(cacheView, isStreaming);
		treePositionKeyFactory.SetValue(cacheView, treePositionKey);
	}

	// Mirrors stock EndpointHtmlRenderer.WriteComponentHtml's CacheView branch. Returns false when the caller
	// should write the component the ordinary way.
	internal bool TryWrite(IServiceProvider services, CacheView cacheView, TextWriter output, Action<TextWriter> write)
	{
		throwIfNested.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [output], null);
		var state = renderState.GetValue(cacheView);
		if (state is null)
		{
			return false;
		}

		if ((bool)isCacheHit.GetValue(state)!)
		{
			// The component's own render tree already holds the cached content.
			write(output);
			return true;
		}

		object?[] arguments = [state, cacheView, output, null];
		if (!(bool)tryBeginWrite.Invoke(null, BindingFlags.DoNotWrapExceptions, null, arguments, null)!)
		{
			return false;
		}

		var captured = false;
		try
		{
			write((TextWriter)arguments[3]!);
			captured = true;
		}
		finally
		{
			endCapture.Invoke(services.GetRequiredService(serviceType),
				BindingFlags.DoNotWrapExceptions, null, [state, captured], null);
		}

		return true;
	}

	// Mirrors stock EndpointComponentState.ComputeTreePositionKey with ComponentKeyHelper.FormatSerializableKey,
	// so an identical component tree produces an identical cache key under Htmxor and under stock.
	internal static string ComputeTreePositionKey(string ancestorTypeName, int sequence, object? componentKey)
	{
		var keyString = FormatSerializableKey(componentKey);
		return string.Concat(
			ancestorTypeName, ".",
			typeof(CacheView).FullName, "#",
			sequence.ToString(CultureInfo.InvariantCulture),
			keyString is not null ? "." : "",
			keyString);
	}

	private static string? FormatSerializableKey(object? key)
	{
		if (key is null)
		{
			return null;
		}

		var keyType = key.GetType();
		var serializable = Type.GetTypeCode(keyType) is not TypeCode.Object ||
			keyType == typeof(Guid) || keyType == typeof(DateTimeOffset) ||
			keyType == typeof(DateOnly) || keyType == typeof(TimeOnly);
		if (!serializable)
		{
			return null;
		}

		return key switch
		{
			IFormattable formattable => formattable.ToString("", CultureInfo.InvariantCulture),
			IConvertible convertible => convertible.ToString(CultureInfo.InvariantCulture),
			_ => default,
		};
	}

	private static Type RequireInternalClass(string name)
	{
		var type = EndpointAssembly.GetType($"Microsoft.AspNetCore.Components.Endpoints.{name}")
			?? throw IncompatibleFramework(name);
		return type.IsNotPublic && type.IsClass && !type.IsGenericType
			? type
			: throw IncompatibleFramework($"internal nongeneric {name} class");
	}

	private static PropertyInfo RequireProperty(Type declaringType, string name, Type propertyType, bool read)
	{
		var property = declaringType.GetProperty(name,
			BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		var accessor = read ? property?.GetMethod : property?.SetMethod;
		if (property is null || property.PropertyType != propertyType || accessor is null ||
			!(accessor.IsAssembly || accessor.IsPublic))
		{
			throw IncompatibleFramework($"{declaringType.Name}.{name} {(read ? "getter" : "setter")} of {propertyType.Name}");
		}

		return property;
	}

	private static MethodInfo RequireMethod(
		Type declaringType, string name, BindingFlags visibility, Type returnType, params Type[] parameterTypes)
	{
		var method = declaringType.GetMethod(name, visibility | BindingFlags.DeclaredOnly, null, parameterTypes, null);
		if (method is null || method.IsGenericMethod || method.ReturnType != returnType ||
			!method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes))
		{
			throw IncompatibleFramework(
				$"{declaringType.Name}.{name}({string.Join(", ", parameterTypes.Select(type => type.Name))}) -> {returnType.Name}");
		}

		return method;
	}

	private static InvalidOperationException IncompatibleFramework(string dependency)
		=> new($"The Htmxor CacheView adapter is incompatible with installed '{EndpointAssembly.FullName}': expected {dependency}. Baseline: ASP.NET Core v11.0.0-rc.1.26425.128, commit c3325eeb6b47bc6383c127d4f4827dc9642a2b6e. Review the upstream dependency and renew paired parity evidence before using this candidate.");
}
#endif
