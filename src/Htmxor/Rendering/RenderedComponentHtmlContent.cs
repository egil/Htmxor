using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;

namespace Htmxor.Rendering;

/// <summary>
/// An implementation of IHtmlContent that holds a reference to a component until
/// we're ready to emit it as HTML to the response.
/// We don't construct the actual HTML until we receive the call to WriteTo.
/// </summary>
internal sealed class RenderedComponentHtmlContent : IHtmlAsyncContent
{
	private readonly Dispatcher? dispatcher;
	private readonly HtmxorRootComponent? htmlToEmitOrNull;

	public static RenderedComponentHtmlContent Empty { get; }
		= new RenderedComponentHtmlContent(null, default);

	public RenderedComponentHtmlContent(
		Dispatcher? dispatcher, // If null, we're only emitting the markers
		HtmxorRootComponent? htmlToEmitOrNull)
	{
		this.dispatcher = dispatcher;
		this.htmlToEmitOrNull = htmlToEmitOrNull;
	}

	public async ValueTask WriteToAsync(TextWriter writer)
	{
		if (dispatcher is null)
		{
			WriteTo(writer, HtmlEncoder.Default);
		}
		else
		{
			await dispatcher.InvokeAsync(() => WriteTo(writer, HtmlEncoder.Default));
		}
	}

	public void WriteTo(TextWriter writer, HtmlEncoder encoder)
	{
		if (htmlToEmitOrNull is { } htmlToEmit)
		{
			htmlToEmit.WriteHtmlTo(writer);
		}
	}

	public Task QuiescenceTask
		=> htmlToEmitOrNull.HasValue
		? htmlToEmitOrNull.Value.QuiescenceTask
		: Task.CompletedTask;
}
