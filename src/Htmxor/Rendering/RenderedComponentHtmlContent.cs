// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;

namespace Htmxor.Rendering;

// An implementation of IHtmlContent that holds a reference to a component until
// we're ready to emit it as HTML to the response.
// We don't construct the actual HTML until we receive the call to WriteTo.
internal sealed class RenderedComponentHtmlContent : IHtmlAsyncContent
{
    private readonly Dispatcher? _dispatcher;
    private readonly HtmxorRootComponent? _htmlToEmitOrNull;

    public static RenderedComponentHtmlContent Empty { get; }
        = new RenderedComponentHtmlContent(null, default);

    public RenderedComponentHtmlContent(
        Dispatcher? dispatcher, // If null, we're only emitting the markers
        HtmxorRootComponent? htmlToEmitOrNull)
    {
        _dispatcher = dispatcher;
        _htmlToEmitOrNull = htmlToEmitOrNull;
    }

    public async ValueTask WriteToAsync(TextWriter writer)
    {
        if (_dispatcher is null)
        {
            WriteTo(writer, HtmlEncoder.Default);
        }
        else
        {
            await _dispatcher.InvokeAsync(() => WriteTo(writer, HtmlEncoder.Default));
        }
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        if (_htmlToEmitOrNull is { } htmlToEmit)
        {
            htmlToEmit.WriteHtmlTo(writer);
        }
    }

    public Task QuiescenceTask
        => _htmlToEmitOrNull.HasValue
        ? _htmlToEmitOrNull.Value.QuiescenceTask
        : Task.CompletedTask;
}
