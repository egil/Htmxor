using System.ComponentModel;
using Htmxor.Builder;

namespace Htmxor.Endpoints;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHtmxorGeneratedComponentActionRequest
{
	bool TryConsume(HtmxorGeneratedComponentAction action);
}
