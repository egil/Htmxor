using Htmxor.Http;
using Microsoft.AspNetCore.Http;

namespace Htmxor;

/// <summary>
/// Indicates that the associated component should match the specified route template pattern and one or more of the optional properties.
/// </summary>
/// <remarks>
/// If one or more additional properties is specified on the attribute, all specified properties much match for the route to be used.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HtmxRouteAttribute : Attribute, IEquatable<HtmxRouteAttribute>
{
	internal static readonly string ImplicitHttpMethod = HttpMethods.Get;

	public static string[] DefaultHttpMethods => [ImplicitHttpMethod];

	/// <summary>
	/// Gets the route template.
	/// </summary>
	[StringSyntax("Route")]
	public string Template { get; }

	/// <summary>
	/// Gets the HTTP methods supported by the route.
	/// On a Razor-authored declaration, omission keeps GET implicit and allows Htmxor to infer additional methods
	/// from supported component bindings. A C#-authored declaration must specify this property.
	/// When specified, this complete allow-list is authoritative.
	/// </summary>
	public string[] Methods { get; init; } = [ImplicitHttpMethod];

	/// <summary>
	/// Specify to only use this route if the <see cref="HtmxRequestHeaderNames.CurrentUrl"/> header matches the specified value.
	/// A relative declaration is resolved against the parsed request URL. Absolute declarations
	/// must be HTTP(S); scheme, host, and effective port use URI comparison rules, while path and
	/// query comparison is ordinal and case-sensitive. Fragments are not part of the comparison.
	/// If null or whitespace, this route is not limited to a specific URL. This is a representation
	/// hint only and never an authorization boundary.
	/// </summary>
	public string? CurrentUrl { get; init; }

	/// <summary>
	/// Specify to only use this representation if the complete <see cref="HtmxRequestHeaderNames.Target"/>
	/// element identity in `tag#id` or `tag` form matches the specified value.
	/// If null or whitespace, this route is not limited to a specific target.
	/// </summary>
	public string? Target { get; init; }

	/// <summary>
	/// Specify to only use this representation if the complete <see cref="HtmxRequestHeaderNames.Target"/>
	/// element identity in `tag#id` or `tag` form matches one of the specified values.
	/// If null or empty, this route is not limited to a specific set of targets.
	/// </summary>
	public string[] Targets { get; init; } = [];

	/// <summary>
	/// Constructs an instance of <see cref="HtmxRouteAttribute"/>.
	/// </summary>
	/// <param name="template">The route template.</param>
	public HtmxRouteAttribute([StringSyntax(StringSyntaxAttribute.Uri)] string template)
	{
		Template = template;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as HtmxRouteAttribute);
	}

	public bool Equals(HtmxRouteAttribute? other)
	{
		return other is not null
			&& Template.Equals(other.Template, StringComparison.OrdinalIgnoreCase)
			&& Methods.SequenceEqual(other.Methods, StringComparer.OrdinalIgnoreCase)
			&& string.Equals(CurrentUrl, other.CurrentUrl, StringComparison.Ordinal)
			&& HtmxElementIdentity.Equals(Target, other.Target)
			&& HasSameTargets(Targets, other.Targets);
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(Template, StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < Methods.Length; i++)
		{
			hash.Add(Methods[i], StringComparer.OrdinalIgnoreCase);
		}

		hash.Add(CurrentUrl, StringComparer.Ordinal);
		hash.Add(HtmxElementIdentity.GetHashCode(Target));

		for (int i = 0; i < Targets.Length; i++)
		{
			hash.Add(HtmxElementIdentity.GetHashCode(Targets[i]));
		}

		return hash.ToHashCode();
	}

	private static bool HasSameTargets(string[] left, string[] right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}

		for (var index = 0; index < left.Length; index++)
		{
			if (!HtmxElementIdentity.Equals(left[index], right[index]))
			{
				return false;
			}
		}

		return true;
	}
}
