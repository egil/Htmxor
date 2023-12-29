using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using System.Collections;

namespace BlazorSSR.Components.FlashMessages;

public class FlashMessageQueue : IEnumerable<FlashMessage>
{
    private const string FlashMsgCookieName = ".flash-messages-id";

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMemoryCache memoryCache;
    private Queue<FlashMessage>? messages;

    private Queue<FlashMessage> Messages
    {
        get
        {
            if (messages is not null)
            {
                return messages;
            }

            var flashMsgId = httpContextAccessor?.HttpContext?.Request.Cookies[FlashMsgCookieName];
            if (string.IsNullOrWhiteSpace(flashMsgId))
            {
                var cookieOptions = new CookieOptions
                {
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true,
                    MaxAge = TimeSpan.FromHours(1),
                };

                flashMsgId = Guid.NewGuid().ToString();
                httpContextAccessor?.HttpContext?.Response.Cookies.Append(FlashMsgCookieName, flashMsgId, cookieOptions);
            }

            messages = memoryCache.GetOrCreate(flashMsgId, _ => new Queue<FlashMessage>())!;
            return messages;
        }
    }

    public int Count => Messages.Count;

    public FlashMessageQueue(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.memoryCache = memoryCache;
    }

    public void Add(string message, FlashMessageType type = FlashMessageType.Info)
    {
        Add(new() { Content = b => b.AddContent(0, message), Type = type });
    }

    public void Add(RenderFragment message, FlashMessageType type = FlashMessageType.Info)
    {
        Add(new() { Content = message, Type = type });
    }

    public void Add(FlashMessage message) => Messages.Enqueue(message);

    public IEnumerator<FlashMessage> GetEnumerator()
    {
        while (Count > 0)
        {
            yield return Messages.Dequeue();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
