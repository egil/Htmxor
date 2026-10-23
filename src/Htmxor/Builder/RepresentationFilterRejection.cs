namespace Htmxor.Builder;

internal readonly record struct RepresentationFilterRejection(string Filter, string DeclaredValue, bool RequestValueAbsent);
