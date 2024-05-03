// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Htmxor.TestAssets;

internal sealed class HttpContextBuilder
{
	private HttpRequestFeature? request;

	public HttpContextBuilder WithUrl([StringSyntax(StringSyntaxAttribute.Uri)] string url)
	{
		CreateDefaultRequestFeature();

		request.Path = url;
		return this;
	}

	public HttpContextBuilder WithMethod(HttpMethod method)
	{
		CreateDefaultRequestFeature();

		request.Method = method.Method;
		return this;
	}

	public HttpContextBuilder WithRequestHeader(params (string HeaderName, string? Value)[] headers)
	{
		CreateDefaultRequestFeature();

		foreach (var (key, value) in headers)
		{
			request.Headers[key] = value ?? "";
		}

		return this;
	}

	public HttpContext Build()
	{
		CreateDefaultRequestFeature();
		var context = new DefaultHttpContext();
		context.Features.Set<IHttpRequestFeature>(request);
		return context;
	}

	[MemberNotNull(nameof(request))]
	private void CreateDefaultRequestFeature()
	{
		request ??= new HttpRequestFeature()
		{
			PathBase = "https://localhost",
			Path = "",
			Method = HttpMethod.Get.Method,
			Headers = new HeaderDictionary(),
		};
	}
}