#if NET11_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v11.0.0-rc.2.26504.105, commit 820230c386db5b0106c5430b3e5aee92a7826bf5,
// synchronized 2026-09-13. Approved #212 dependencies and exact sources: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/SessionCascadingValueSupplier.cs | private-accesses

// Shipped VMR baselines re-reviewed 2026-10-15 for #329; source deltas: docs/engineering/patched-framework-baselines.md.

using System.Reflection;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Endpoints;

internal sealed class HtmxorEndpointCandidateSessionServices
{
	private static readonly Assembly EndpointAssembly = typeof(IRazorComponentEndpointInvoker).Assembly;
	private readonly Type supplierType;
	private readonly MethodInfo setRequestContext;
	private readonly MethodInfo persistAllValues;

	private HtmxorEndpointCandidateSessionServices()
	{
		supplierType = EndpointAssembly.GetType("Microsoft.AspNetCore.Components.Endpoints.SessionCascadingValueSupplier")
			?? throw IncompatibleFramework("SessionCascadingValueSupplier");
		if (!supplierType.IsNotPublic || !supplierType.IsClass || supplierType.IsGenericType)
		{
			throw IncompatibleFramework("internal nongeneric SessionCascadingValueSupplier class");
		}

		setRequestContext = RequireMethod("SetRequestContext", typeof(void), typeof(HttpContext));
		persistAllValues = RequireMethod("PersistAllValues", typeof(Task));
	}

	internal static HtmxorEndpointCandidateSessionServices Create() => new();

	internal void Initialize(HttpContext context)
	{
		if (context.RequestServices.GetService(supplierType) is { } supplier)
		{
			setRequestContext.Invoke(supplier, BindingFlags.DoNotWrapExceptions, null, [context], null);
		}
	}

	internal Task PersistAsync(HttpContext context)
		=> context.RequestServices.GetService(supplierType) is { } supplier
			? (Task)persistAllValues.Invoke(supplier, BindingFlags.DoNotWrapExceptions, null, null, null)!
			: Task.CompletedTask;

	private MethodInfo RequireMethod(string name, Type returnType, params Type[] parameterTypes)
	{
		var method = supplierType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
			null, parameterTypes, null);
		if (method is null || !method.IsAssembly || method.IsGenericMethod || method.ReturnType != returnType ||
			!method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes))
		{
			throw IncompatibleFramework($"SessionCascadingValueSupplier.{name}({string.Join(", ", parameterTypes.Select(type => type.FullName))}) -> {returnType.FullName}");
		}

		return method;
	}

	private static InvalidOperationException IncompatibleFramework(string dependency)
		=> new($"The Htmxor Session adapter is incompatible with installed '{EndpointAssembly.FullName}': expected {dependency}. Baseline: ASP.NET Core v11.0.0-rc.2.26504.105, commit 820230c386db5b0106c5430b3e5aee92a7826bf5. Review the upstream dependency and renew paired parity evidence before using this candidate.");
}
#endif
