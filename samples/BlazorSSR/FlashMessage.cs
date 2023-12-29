using Microsoft.Extensions.Caching.Memory;
using System.Collections;

namespace BlazorSSR;

public class FlashMessage : IEnumerable<string>
{
    private const string FlashMsgCookieName = ".flash-messages-id";

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMemoryCache memoryCache;
    private Queue<string>? messages;

    private Queue<string> Messages
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

            messages = memoryCache.GetOrCreate(flashMsgId, _ => new Queue<string>())!;
            return messages;
        }
    }

    public int Count => Messages.Count;

    public FlashMessage(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.memoryCache = memoryCache;
    }

    public void Add(string message)
    {
        Messages.Enqueue(message);
    }

    public IEnumerator<string> GetEnumerator()
    {
        while (Count > 0)
        {
            yield return Messages.Dequeue();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
