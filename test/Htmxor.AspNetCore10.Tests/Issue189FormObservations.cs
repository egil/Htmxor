using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed class Issue189FormModel
{
	[Required]
	public string Name { get; set; } = string.Empty;

	[Range(1, 120)]
	public int Age { get; set; }

	public List<string> Tags { get; set; } = [];
}

internal sealed record Issue189ComponentEvent(string Label, Guid Instance, string Phase, string? Value);

internal sealed class Issue189Observation
{
	public ConcurrentQueue<string> Operations { get; } = new();

	public ConcurrentQueue<Issue189ComponentEvent> Components { get; } = new();

	public ConcurrentQueue<string> GeneratedTokens { get; } = new();
}

internal sealed class Issue189Journal
{
	private readonly ConcurrentDictionary<string, Issue189Observation> requests = new();

	public Issue189Observation For(string requestId) => requests.GetOrAdd(requestId, _ => new());
}

internal sealed class Issue189OverlapGate
{
	private readonly TaskCompletionSource bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly ConcurrentDictionary<string, byte> arrivals = new();

	public int RequestCount => arrivals.Count;

	public async Task ArriveAsync(string requestId)
	{
		if (arrivals.TryAdd(requestId, 0) && arrivals.Count == 2)
		{
			bothArrived.TrySetResult();
		}

		await bothArrived.Task.WaitAsync(TimeSpan.FromSeconds(20));
	}
}

internal sealed class Issue189RequestProbe(
	IHttpContextAccessor accessor,
	Issue189Journal journal,
	Issue189OverlapGate gate)
{
	public string RequestId => accessor.HttpContext!.Request.Headers["X-Issue-189-Request"].ToString();

	public Issue189Observation Observation => journal.For(RequestId);

	public void Record(string label, Guid instance, string phase, string? value)
		=> Observation.Components.Enqueue(new(label, instance, phase, value));

	public Task WaitForOverlapAsync()
		=> accessor.HttpContext!.Request.Headers.ContainsKey("X-Issue-189-Overlap")
			? gate.ArriveAsync(RequestId)
			: Task.CompletedTask;
}

internal sealed class Issue189ObservedMapper(IFormValueMapper inner, Issue189RequestProbe probe) : IFormValueMapper
{
	public bool CanMap(Type valueType, string scopeName, string? formName)
		=> inner.CanMap(valueType, scopeName, formName);

	public void Map(FormValueMappingContext context)
	{
		probe.Observation.Operations.Enqueue("map");
		inner.Map(context);
	}
}

internal sealed class Issue189ObservedAntiforgery(IAntiforgery inner, Issue189Journal journal) : IAntiforgery
{
	public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
	{
		Record(httpContext, $"store:started={httpContext.Response.HasStarted}");
		return RememberToken(httpContext, inner.GetAndStoreTokens(httpContext));
	}

	public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
	{
		Record(httpContext, $"tokens:started={httpContext.Response.HasStarted}");
		return RememberToken(httpContext, inner.GetTokens(httpContext));
	}

	public async Task<bool> IsRequestValidAsync(HttpContext httpContext)
	{
		var valid = await inner.IsRequestValidAsync(httpContext);
		Record(httpContext, $"invoker-validation:{valid}");
		return valid;
	}

	public async Task ValidateRequestAsync(HttpContext httpContext)
	{
		try
		{
			await inner.ValidateRequestAsync(httpContext);
			Record(httpContext, "middleware-validation:True");
		}
		catch (AntiforgeryValidationException)
		{
			Record(httpContext, "middleware-validation:False");
			throw;
		}
	}

	public void SetCookieTokenAndHeader(HttpContext httpContext)
	{
		Record(httpContext, $"cookie:started={httpContext.Response.HasStarted}");
		inner.SetCookieTokenAndHeader(httpContext);
	}

	private AntiforgeryTokenSet RememberToken(HttpContext context, AntiforgeryTokenSet tokens)
	{
		if (tokens.RequestToken is { } token)
		{
			journal.For(context.Request.Headers["X-Issue-189-Request"].ToString()).GeneratedTokens.Enqueue(token);
		}

		return tokens;
	}

	private void Record(HttpContext context, string operation)
		=> journal.For(context.Request.Headers["X-Issue-189-Request"].ToString()).Operations.Enqueue(operation);
}
