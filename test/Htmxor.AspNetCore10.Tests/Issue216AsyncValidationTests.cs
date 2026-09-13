#if NET11_0_OR_GREATER
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue216AsyncValidationTests
{
	[Theory]
	[InlineData(false, "Ada", "valid")]
	[InlineData(false, "taken", "invalid")]
	[InlineData(true, "Ada", "valid")]
	[InlineData(true, "taken", "invalid")]
	public async Task Named_submit_awaits_validation_and_dispatches_once_on_the_request_instance(
		bool direct, string name, string expected)
	{
		var stock = await SubmitAsync(false, false, name, expected);
		var candidate = await SubmitAsync(true, direct, name, expected);

		Assert.Equal(stock.Messages, candidate.Messages);
		Assert.Equal(stock.Trace, candidate.Trace);
	}

	private static async Task<(string Messages, string[] Trace)> SubmitAsync(
		bool htmxor, bool direct, string name, string expected)
	{
		var journal = new Issue216Journal();
		await using var host = await StartAsync(htmxor, journal);
		using var request = Post("submit", name, direct);
		await host.AddCredentialsAsync(request, "/issue-210/form");
		var observation = journal.For("submit");
		var pending = host.Client.SendAsync(request);
		try
		{
			await observation.Entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
			Assert.False(pending.IsCompleted);
			Assert.DoesNotContain(observation.Events, item => item.Phase is "valid" or "invalid");
		}
		finally
		{
			observation.Release.TrySetResult();
		}

		using var response = await pending.WaitAsync(TimeSpan.FromSeconds(20));
		await AssertResultAsync(response, observation, name, expected);
		var html = await response.Content.ReadAsStringAsync();
		var messages = Assert.Single(Regex.Matches(html,
			"<section data-validation-messages>(.*?)</section>", RegexOptions.Singleline | RegexOptions.CultureInvariant));
		return (messages.Groups[1].Value, observation.Events.Select(item => item.Phase).ToArray());
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Rejected_protection_does_not_bind_initialize_validate_or_submit(bool htmxor, bool direct)
	{
		var journal = new Issue216Journal();
		await using var host = await StartAsync(htmxor, journal);
		using var request = Post("rejected", "taken", direct);

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(journal.For("rejected").Events);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Concurrent_validation_keeps_messages_and_callbacks_request_local(bool direct)
	{
		var journal = new Issue216Journal();
		await using var host = await StartAsync(true, journal);
		using var valid = Post("valid", "Ada", direct);
		using var invalid = Post("invalid", "taken", direct);
		await host.AddCredentialsAsync(valid, "/issue-210/form");
		await host.AddCredentialsAsync(invalid, "/issue-210/form");
		var first = host.Client.SendAsync(valid);
		var second = host.Client.SendAsync(invalid);
		try
		{
			await Task.WhenAll(journal.For("valid").Entered.Task, journal.For("invalid").Entered.Task)
				.WaitAsync(TimeSpan.FromSeconds(20));
			journal.For("invalid").Release.TrySetResult();
			using var invalidResponse = await second.WaitAsync(TimeSpan.FromSeconds(20));
			await AssertResultAsync(invalidResponse, journal.For("invalid"), "taken", "invalid");
			Assert.False(first.IsCompleted);
		}
		finally
		{
			journal.For("valid").Release.TrySetResult();
			journal.For("invalid").Release.TrySetResult();
		}

		using var validResponse = await first.WaitAsync(TimeSpan.FromSeconds(20));
		await AssertResultAsync(validResponse, journal.For("valid"), "Ada", "valid");
		Assert.NotEqual(journal.For("valid").Events.First().Instance, journal.For("invalid").Events.First().Instance);
	}

	private static Task<Issue210SecurityHost> StartAsync(bool htmxor, Issue216Journal journal)
		=> Issue210SecurityHost.StartAsync(htmxor: htmxor, groupPrefix: "", configureServices: services =>
		{
			services.AddHttpContextAccessor();
			services.AddSingleton(journal);
		});

	private static HttpRequestMessage Post(string id, string name, bool direct)
	{
		var request = new HttpRequestMessage(HttpMethod.Post, "/issue-216/form");
		request.Headers.Add("X-Issue-216", id);
		if (direct)
		{
			request.Headers.Add("HX-Request", "true");
			request.Headers.Add("HX-Request-Type", "partial");
		}

		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "save", ["Model.Name"] = name,
		});
		return request;
	}

	private static async Task AssertResultAsync(
		HttpResponseMessage response, Issue216Submission observation, string name, string expected)
	{
		var html = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, html);
		Assert.Contains($"data-result=\"{expected}\"", html, StringComparison.Ordinal);
		Assert.Contains($"data-name=\"{name}\"", html, StringComparison.Ordinal);
		Assert.Equal(expected == "invalid", html.Contains("This name is already taken.", StringComparison.Ordinal));
		var callback = Assert.Single(observation.Events, item => item.Phase is "valid" or "invalid");
		Assert.Equal(expected, callback.Phase);
		Assert.All(observation.Events, item => Assert.Equal(callback.Instance, item.Instance));
		Assert.Equal(new[] { "validation-started", "validation-completed", expected },
			observation.Events.Select(item => item.Phase).Where(phase => phase is not ("binding" or "initialized")));
	}
}
#endif
