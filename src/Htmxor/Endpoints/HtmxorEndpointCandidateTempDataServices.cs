#if NET11_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v11.0.0-rc.1.26425.128, commit c3325eeb6b47bc6383c127d4f4827dc9642a2b6e,
// synchronized 2026-09-15. Approved #213 dependencies and exact sources: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/TempData/TempDataCascadingValueSupplier.cs | private-accesses
// Htmxor upstream dependency: src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs | private-accesses
// Htmxor upstream dependency: src/Components/Endpoints/src/DependencyInjection/TempDataService.cs | private-accesses

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Endpoints;

// The stock ITempData cascade and write-back are reached through EndpointHtmlRenderer, which the candidate
// replaces. ITempData exposes dictionary, Get, Peek and Keep but no initialization or persistence operation,
// and enumerating the framework dictionary consumes retained keys instead of saving them. These three
// framework-owned members are therefore invoked directly so the stock state machine keeps deciding
// availability, consumption and retention.
internal sealed class HtmxorEndpointCandidateTempDataServices
{
	private static readonly Assembly EndpointAssembly = typeof(IRazorComponentEndpointInvoker).Assembly;
	private readonly Type supplierType;
	private readonly Type serviceType;
	private readonly MethodInfo setRequestContext;
	private readonly MethodInfo getOrCreateTempData;
	private readonly MethodInfo persist;

	private HtmxorEndpointCandidateTempDataServices()
	{
		supplierType = RequireInternalClass("TempDataCascadingValueSupplier");
		serviceType = RequireInternalClass("TempDataService");
		var providerExtensions = RequireInternalClass("TempDataProviderServiceCollectionExtensions");

		setRequestContext = RequireMethod(supplierType, "SetRequestContext",
			BindingFlags.NonPublic | BindingFlags.Instance, method => method.IsAssembly, typeof(void), typeof(HttpContext));
		getOrCreateTempData = RequireMethod(providerExtensions, "GetOrCreateTempData",
			BindingFlags.NonPublic | BindingFlags.Static, method => method.IsAssembly, typeof(ITempData), typeof(HttpContext));
		persist = RequireMethod(serviceType, "Persist",
			BindingFlags.Public | BindingFlags.Instance, method => method.IsPublic, typeof(void), typeof(HttpContext));
	}

	internal static HtmxorEndpointCandidateTempDataServices Create() => new();

	// The stock supplier reads [SupplyParameterFromTempData] through the request it was given. The candidate
	// supplies the same request after protection succeeded and before any component renders.
	internal void Initialize(HttpContext context)
	{
		if (context.RequestServices.GetService(supplierType) is { } supplier)
		{
			setRequestContext.Invoke(supplier, BindingFlags.DoNotWrapExceptions, null, [context], null);
		}
	}

	// Returns the framework-owned request-local dictionary backing the public ITempData cascade.
	internal ITempData GetOrCreate(HttpContext context)
		=> (ITempData)getOrCreateTempData.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [context], null)!;

	// Stock runs the provider write-back once every component finished, so values changed during async work
	// are captured. The framework decides consumption, retention and whether a started response can still save.
	internal void Persist(HttpContext context)
		=> persist.Invoke(context.RequestServices.GetRequiredService(serviceType),
			BindingFlags.DoNotWrapExceptions, null, [context], null);

	private static Type RequireInternalClass(string name)
	{
		var type = EndpointAssembly.GetType($"Microsoft.AspNetCore.Components.Endpoints.{name}")
			?? throw IncompatibleFramework(name);
		return type.IsNotPublic && type.IsClass && !type.IsGenericType
			? type
			: throw IncompatibleFramework($"internal nongeneric {name} class");
	}

	private static MethodInfo RequireMethod(Type declaringType, string name, BindingFlags visibility,
		Func<MethodInfo, bool> isExpectedAccessibility, Type returnType, params Type[] parameterTypes)
	{
		var method = declaringType.GetMethod(name, visibility | BindingFlags.DeclaredOnly, null, parameterTypes, null);
		if (method is null || !isExpectedAccessibility(method) || method.IsGenericMethod || method.ReturnType != returnType ||
			!method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes))
		{
			throw IncompatibleFramework(
				$"{declaringType.Name}.{name}({string.Join(", ", parameterTypes.Select(type => type.FullName))}) -> {returnType.FullName}");
		}

		return method;
	}

	private static InvalidOperationException IncompatibleFramework(string dependency)
		=> new($"The Htmxor TempData adapter is incompatible with installed '{EndpointAssembly.FullName}': expected {dependency}. Baseline: ASP.NET Core v11.0.0-rc.1.26425.128, commit c3325eeb6b47bc6383c127d4f4827dc9642a2b6e. Review the upstream dependency and renew paired parity evidence before using this candidate.");
}
#endif
