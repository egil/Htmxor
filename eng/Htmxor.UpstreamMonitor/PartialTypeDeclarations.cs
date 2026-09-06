namespace Htmxor.UpstreamMonitor;

internal static class PartialTypeDeclarations
{
	private static readonly string[] AccessModifiers = ["public", "private", "protected", "internal"];
	private static readonly string[] ShapeModifiers = ["abstract", "sealed", "static", "readonly", "ref", "partial"];

	public static IEnumerable<SourceType> Merge(IEnumerable<SourceType> declarations)
	{
		var parts = declarations.ToArray();
		var modifiers = parts.Select(part => part.Modifiers.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
		if (!modifiers.Any(part => part.Contains("partial", StringComparer.Ordinal)))
		{
			return parts;
		}
		if (modifiers.Any(part => !part.Contains("partial", StringComparer.Ordinal)) ||
			parts.Select(part => part.Kind).Distinct(StringComparer.Ordinal).Count() != 1)
		{
			throw new MonitorFailure("Partial type declarations have incompatible kinds or partial modifiers.");
		}
		var shape = ShapeModifiers.Where(modifier => modifiers.Any(part => part.Contains(modifier, StringComparer.Ordinal))).ToArray();
		ValidateShape(shape);
		var parameters = parts.Select(part => part.PrimaryConstructorParameters).Where(value => value is not null).ToArray();
		if (parameters.Length > 1)
		{
			throw new MonitorFailure("Partial type declarations have multiple primary constructors.");
		}
		return [parts[0] with
		{
			Modifiers = string.Join(' ', new[] { Accessibility(parts, modifiers) }.Concat(shape)),
			Bases = parts.SelectMany(part => part.Bases).Distinct(StringComparer.Ordinal).ToArray(),
			Constraints = parts.SelectMany(part => part.Constraints).Distinct(StringComparer.Ordinal).ToArray(),
			Body = string.Join('\n', parts.Select(part => part.Body)),
			DeclarationBody = string.Join('\n', parts.Select(part => part.DeclarationBody)),
			PrimaryConstructorParameters = parameters.SingleOrDefault(),
		}];
	}

	private static string Accessibility(SourceType[] parts, string[][] modifiers)
	{
		// Omitted accessibility inherits any explicit sibling declaration; defaults apply only when every part omits it.
		var explicitAccess = modifiers.Select(part => string.Join(' ', AccessModifiers.Where(modifier => part.Contains(modifier, StringComparer.Ordinal))))
			.Where(access => access.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
		if (explicitAccess.Length > 1)
		{
			throw new MonitorFailure("Partial type declarations have conflicting accessibility.");
		}
		return explicitAccess.SingleOrDefault() ?? parts[0].DefaultAccessibility;
	}

	private static void ValidateShape(string[] modifiers)
	{
		if (modifiers.Contains("abstract", StringComparer.Ordinal) && modifiers.Contains("sealed", StringComparer.Ordinal))
		{
			throw new MonitorFailure("Partial type declarations cannot be both abstract and sealed.");
		}
	}
}
