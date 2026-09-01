using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Builder;

public class ConditionalResponseBodyStreamTests
{
	[Fact]
	public async Task Suppressed_Task_write_honors_a_pre_cancelled_token()
	{
		var response = CreateSuppressingResponse();
		using var inner = new MemoryStream();
		using var stream = new ConditionalResponseBodyStream(inner, response);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			stream.WriteAsync(new byte[1], 0, 1, cancellation.Token));

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(0, inner.Length);
	}

	[Fact]
	public async Task Suppressed_ValueTask_write_honors_a_pre_cancelled_token()
	{
		var response = CreateSuppressingResponse();
		using var inner = new MemoryStream();
		using var stream = new ConditionalResponseBodyStream(inner, response);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1]), cancellation.Token));

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(0, inner.Length);
	}

	private static HtmxResponse CreateSuppressingResponse()
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[HtmxRequestHeaderNames.HtmxRequest] = "true";
		return context.GetHtmxContext().Response.EmptyBody();
	}
}
