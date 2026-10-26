namespace Htmxor.AspNetCore10.Issue375.Browser;

public sealed class Issue50CacheProbe
{
	private int renderCount;

	public int RenderCount => Volatile.Read(ref renderCount);

	public int RecordRender() => Interlocked.Increment(ref renderCount);
}
