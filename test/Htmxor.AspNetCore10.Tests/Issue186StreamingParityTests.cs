using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed class Issue186StreamingParityTests
{
	[Fact]
	public async Task AddHtmxor_waits_for_a_non_streaming_sibling_before_starting_a_mixed_streaming_response()
	{
		await using var stock = await Issue186MixedStreamingHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue186MixedStreamingHost.CreateAsync(useHtmxor: true);

		var stockResponseTask = stock.Client.GetAsync(Issue186MixedStreamingHost.Path, HttpCompletionOption.ResponseHeadersRead);
		var htmxorResponseTask = htmxor.Client.GetAsync(Issue186MixedStreamingHost.Path, HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(stock.Probe.InitialRendersReached, htmxor.Probe.InitialRendersReached);

		await Assert.ThrowsAsync<TimeoutException>(async () => await htmxorResponseTask.WaitAsync(TimeSpan.FromMilliseconds(100)));
		stock.Probe.CompleteNonStreaming();
		htmxor.Probe.CompleteNonStreaming();

		using var stockResponse = await stockResponseTask;
		using var htmxorResponse = await htmxorResponseTask;
		stock.Probe.CompleteStreaming();
		htmxor.Probe.CompleteStreaming();
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var htmxorSnapshot = await Issue187ResponseSnapshot.CreateAsync(htmxorResponse);

		Assert.Contains("non-streaming-completed", htmxorSnapshot.Body, StringComparison.Ordinal);
		Assert.Equal(stockSnapshot.StatusCode, htmxorSnapshot.StatusCode);
		Assert.Equal(NormalizeHeaders(stockSnapshot.Headers), NormalizeHeaders(htmxorSnapshot.Headers));
		Assert.Equal(stockSnapshot.Body, htmxorSnapshot.Body);
	}

	[Fact]
	public async Task AddHtmxor_preserves_a_completed_mixed_response_when_the_streaming_child_finishes_first()
	{
		await using var stock = await Issue186MixedStreamingHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue186MixedStreamingHost.CreateAsync(useHtmxor: true);

		var stockResponseTask = stock.Client.GetAsync(Issue186MixedStreamingHost.Path);
		var htmxorResponseTask = htmxor.Client.GetAsync(Issue186MixedStreamingHost.Path);
		await Task.WhenAll(stock.Probe.InitialRendersReached, htmxor.Probe.InitialRendersReached);
		stock.Probe.CompleteStreaming();
		htmxor.Probe.CompleteStreaming();
		stock.Probe.CompleteNonStreaming();
		htmxor.Probe.CompleteNonStreaming();

		using var stockResponse = await stockResponseTask;
		using var htmxorResponse = await htmxorResponseTask;
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var htmxorSnapshot = await Issue187ResponseSnapshot.CreateAsync(htmxorResponse);

		Assert.DoesNotContain("<blazor-ssr>", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Equal(stockSnapshot.StatusCode, htmxorSnapshot.StatusCode);
		Assert.Equal(NormalizeHeaders(stockSnapshot.Headers), NormalizeHeaders(htmxorSnapshot.Headers));
		Assert.Equal(stockSnapshot.Body, htmxorSnapshot.Body);
	}

	[Fact]
	public async Task AddHtmxor_preserves_streamed_navigation_response_shape()
	{
		await using var stock = await Issue186StreamingHost.CreateAsync(useHtmxor: false, navigateAfterUpdate: true);
		await using var htmxor = await Issue186StreamingHost.CreateAsync(useHtmxor: true, navigateAfterUpdate: true);

		var stockRequest = stock.Client.GetAsync(Issue186StreamingHost.Path);
		var htmxorRequest = htmxor.Client.GetAsync(Issue186StreamingHost.Path);
		await Task.WhenAll(stock.Probe.InitialRenderReached, htmxor.Probe.InitialRenderReached);
		stock.Probe.Complete();
		htmxor.Probe.Complete();

		using var stockResponse = await stockRequest;
		using var htmxorResponse = await htmxorRequest;
		var stockBody = await stockResponse.Content.ReadAsStringAsync();
		var htmxorBody = await htmxorResponse.Content.ReadAsStringAsync();

		Assert.Contains("<template type=\"redirection\"", stockBody, StringComparison.Ordinal);
		Assert.Contains("<template type=\"redirection\"", htmxorBody, StringComparison.Ordinal);
		Assert.Equal(stockResponse.StatusCode, htmxorResponse.StatusCode);
	}

	[Fact]
	public async Task AddHtmxor_flushes_each_static_ssr_update_before_completion()
	{
		await using var htmxor = await Issue186StreamingHost.CreateAsync(useHtmxor: true);
		using var request = new HttpRequestMessage(HttpMethod.Get, Issue186StreamingHost.Path);
		var responseTask = htmxor.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await htmxor.Probe.InitialRenderReached;
		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(1));
		await using var body = await response.Content.ReadAsStreamAsync();

		Assert.Contains("initial", await ReadUntilAsync(body, "initial"), StringComparison.Ordinal);
		htmxor.Probe.ReleaseUpdate();
		Assert.Contains("updated", await ReadUntilAsync(body, "updated"), StringComparison.Ordinal);
		htmxor.Probe.Complete();
	}

	[Fact]
	public async Task AddHtmxor_preserves_static_ssr_streaming_updates()
	{
		await using var stock = await Issue186StreamingHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue186StreamingHost.CreateAsync(useHtmxor: true);

		var stockRequest = stock.Client.GetAsync(Issue186StreamingHost.Path);
		var htmxorRequest = htmxor.Client.GetAsync(Issue186StreamingHost.Path);
		await Task.WhenAll(stock.Probe.InitialRenderReached, htmxor.Probe.InitialRenderReached);
		stock.Probe.Complete();
		htmxor.Probe.Complete();

		using var stockResponse = await stockRequest;
		using var htmxorResponse = await htmxorRequest;
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var htmxorSnapshot = await Issue187ResponseSnapshot.CreateAsync(htmxorResponse);

		Assert.Contains("<blazor-ssr>", stockSnapshot.Body, StringComparison.Ordinal);
		Assert.Equal(stockSnapshot.StatusCode, htmxorSnapshot.StatusCode);
		Assert.Equal(NormalizeHeaders(stockSnapshot.Headers), NormalizeHeaders(htmxorSnapshot.Headers));
		Assert.Equal(stockSnapshot.Body, htmxorSnapshot.Body);
	}

	private static IReadOnlyDictionary<string, string> NormalizeHeaders(IReadOnlyDictionary<string, string> headers)
		=> headers.ToDictionary(
			header => header.Key,
			header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
				? "<dynamic-antiforgery-cookie>"
				: header.Value,
			StringComparer.OrdinalIgnoreCase);

	private static async Task<string> ReadUntilAsync(Stream body, string marker)
	{
		var buffer = new byte[1024];
		var output = new MemoryStream();
		while (true)
		{
			var count = await body.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(1));
			Assert.NotEqual(0, count);
			await output.WriteAsync(buffer.AsMemory(0, count));
			var text = System.Text.Encoding.UTF8.GetString(output.ToArray());
			if (text.Contains(marker, StringComparison.Ordinal))
			{
				return text;
			}
		}
	}
}

internal sealed class Issue186StreamingHost(Issue187ParityHost host, Issue186StreamingProbe probe) : IAsyncDisposable
{
	public const string Path = "/issue-186/streaming";

	public HttpClient Client => host.Client;

	public Issue186StreamingProbe Probe { get; } = probe;

	public static async Task<Issue186StreamingHost> CreateAsync(bool useHtmxor, bool navigateAfterUpdate = false)
	{
		var host = await Issue187ParityHost.CreateAsync<Issue186StreamingApp>(
			useHtmxor,
			options: new()
			{
				ConfigureServices = services => services.AddSingleton(new Issue186StreamingProbe(navigateAfterUpdate)),
			});
		return new(host, host.App.Services.GetRequiredService<Issue186StreamingProbe>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();
}

internal sealed class Issue186MixedStreamingHost(Issue187ParityHost host, Issue186MixedStreamingProbe probe) : IAsyncDisposable
{
	public const string Path = "/issue-186/mixed-streaming";

	public HttpClient Client => host.Client;

	public Issue186MixedStreamingProbe Probe { get; } = probe;

	public static async Task<Issue186MixedStreamingHost> CreateAsync(bool useHtmxor)
	{
		var host = await Issue187ParityHost.CreateAsync<Issue186StreamingApp>(
			useHtmxor,
			options: new()
			{
				ConfigureServices = services => services.AddSingleton<Issue186MixedStreamingProbe>(),
			});
		return new(host, host.App.Services.GetRequiredService<Issue186MixedStreamingProbe>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();
}

internal sealed class Issue186StreamingProbe
{
	private readonly TaskCompletionSource initialRenderReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource update = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public Issue186StreamingProbe(bool navigateAfterUpdate = false)
	{
		NavigateAfterUpdate = navigateAfterUpdate;
	}

	public Task InitialRenderReached => initialRenderReached.Task;

	public bool NavigateAfterUpdate { get; }

	public void MarkInitialRenderReached() => initialRenderReached.TrySetResult();

	public Task WaitForUpdateAsync() => update.Task;

	public void ReleaseUpdate() => update.TrySetResult();

	public Task WaitForCompletionAsync() => completion.Task;

	public void Complete()
	{
		ReleaseUpdate();
		completion.TrySetResult();
	}
}

internal sealed class Issue186MixedStreamingProbe
{
	private readonly TaskCompletionSource streamingInitialRenderReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource nonStreamingInitialRenderReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource streamingCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource nonStreamingCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public Task InitialRendersReached => Task.WhenAll(streamingInitialRenderReached.Task, nonStreamingInitialRenderReached.Task);

	public void MarkStreamingInitialRenderReached() => streamingInitialRenderReached.TrySetResult();

	public void MarkNonStreamingInitialRenderReached() => nonStreamingInitialRenderReached.TrySetResult();

	public Task WaitForStreamingCompletionAsync() => streamingCompletion.Task;

	public Task WaitForNonStreamingCompletionAsync() => nonStreamingCompletion.Task;

	public void CompleteStreaming() => streamingCompletion.TrySetResult();

	public void CompleteNonStreaming() => nonStreamingCompletion.TrySetResult();
}

public sealed class Issue186StreamingReexecutionParityTests
{
	[Fact]
	public async Task AddHtmxor_waits_for_a_streaming_status_reexecution_before_the_response_starts()
	{
		await using var stock = await Issue186StreamingStatusHost.CreateAsync(useHtmxor: false);
		await using var htmxor = await Issue186StreamingStatusHost.CreateAsync(useHtmxor: true);

		var stockResponseTask = stock.Client.GetAsync(Issue186StreamingStatusHost.OriginPath, HttpCompletionOption.ResponseHeadersRead);
		var htmxorResponseTask = htmxor.Client.GetAsync(Issue186StreamingStatusHost.OriginPath, HttpCompletionOption.ResponseHeadersRead);
		await Task.WhenAll(stock.Probe.InitialRenderReached, htmxor.Probe.InitialRenderReached);
		await Assert.ThrowsAsync<TimeoutException>(async () => await htmxorResponseTask.WaitAsync(TimeSpan.FromMilliseconds(100)));
		stock.Probe.Complete();
		htmxor.Probe.Complete();

		using var stockResponse = await stockResponseTask;
		using var htmxorResponse = await htmxorResponseTask;
		var stockSnapshot = await Issue187ResponseSnapshot.CreateAsync(stockResponse);
		var htmxorSnapshot = await Issue187ResponseSnapshot.CreateAsync(htmxorResponse);

		Assert.Equal(stockSnapshot.StatusCode, htmxorSnapshot.StatusCode);
		Assert.Equal(stockSnapshot.Headers, htmxorSnapshot.Headers);
		Assert.Equal(stockSnapshot.Body, htmxorSnapshot.Body);
	}
}

internal sealed class Issue186StreamingStatusHost(Issue187ParityHost host, Issue186DeferredStreamingProbe probe) : IAsyncDisposable
{
	public const string OriginPath = "/issue-186/streaming-status-origin";

	public HttpClient Client => host.Client;

	public Issue186DeferredStreamingProbe Probe { get; } = probe;

	public static async Task<Issue186StreamingStatusHost> CreateAsync(bool useHtmxor)
	{
		var host = await Issue187ParityHost.CreateAsync<Issue186StreamingApp>(
			useHtmxor,
			options: new()
			{
				ConfigureServices = services => services.AddSingleton<Issue186DeferredStreamingProbe>(),
				BeforeSession = app =>
				{
					app.UseStatusCodePagesWithReExecute("/issue-186/streaming-status");
					app.Use(static (context, next) =>
					{
						if (context.Request.Path == OriginPath)
						{
							context.Response.StatusCode = StatusCodes.Status404NotFound;
							return Task.CompletedTask;
						}
						return next();
					});
					app.UseRouting();
				},
			});
		return new(host, host.App.Services.GetRequiredService<Issue186DeferredStreamingProbe>());
	}

	public ValueTask DisposeAsync() => host.DisposeAsync();
}

internal sealed class Issue186DeferredStreamingProbe
{
	private readonly TaskCompletionSource initialRenderReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public Task InitialRenderReached => initialRenderReached.Task;

	public void MarkInitialRenderReached() => initialRenderReached.TrySetResult();

	public Task WaitForCompletionAsync() => completion.Task;

	public void Complete() => completion.TrySetResult();
}
