// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using global::Microsoft.AspNetCore.Http.HttpResults;
using global::Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using static Htmxor.LinkerFlags;

namespace Htmxor.Endpoints.Results;

/// <summary>
/// An <see cref="IResult"/> that renders a Htmxor Razor Component.
/// </summary>
public class HtmxorComponentResult<[DynamicallyAccessedMembers(Component)] TComponent>
	: HtmxorComponentResult where TComponent : IComponent
{
	/// <summary>
	/// Constructs an instance of <see cref="HtmxorComponentResult"/>.
	/// </summary>
	public HtmxorComponentResult() : base(typeof(TComponent))
	{
	}

	/// <summary>
	/// Constructs an instance of <see cref="HtmxorComponentResult"/>.
	/// </summary>
	/// <param name="parameters">Parameters for the component.</param>
	public HtmxorComponentResult(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] object parameters) : base(typeof(TComponent), parameters)
	{
	}

	/// <summary>
	/// Constructs an instance of <see cref="HtmxorComponentResult"/>.
	/// </summary>
	/// <param name="parameters">Parameters for the component.</param>
	public HtmxorComponentResult(IReadOnlyDictionary<string, object?> parameters) : base(typeof(TComponent), parameters)
	{
	}
}
