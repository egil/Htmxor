using System.Reflection;

namespace Htmxor.Builder;

internal sealed record HtmxorRoutePages(Assembly Assembly, IReadOnlyList<Type> PageTypes);
