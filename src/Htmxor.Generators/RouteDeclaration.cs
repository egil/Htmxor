using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class RouteDeclaration
{
	private const string RoutePrefix = "@attribute [Htmxor.HtmxRoute(\"";
	private const string RouteSuffix = "\", Methods = new[] { \"GET\" })]";
	private const string PolicyPrefix = "@attribute [Authorize(Policy = \"";
	private const string PolicySuffix = "\")]";

	private RouteDeclaration(
		string path,
		string componentName,
		string route,
		string policy,
		bool hasDeclaration,
		bool hasPage,
		int declarationCount,
		int authorizationDeclarationCount,
		Location location)
	{
		Path = path;
		ComponentName = componentName;
		Route = route;
		Policy = policy;
		HasDeclaration = hasDeclaration;
		HasPage = hasPage;
		DeclarationCount = declarationCount;
		AuthorizationDeclarationCount = authorizationDeclarationCount;
		Location = location;
	}

	public string Path { get; }

	public string ComponentName { get; }

	public string Route { get; }

	public string Policy { get; }

	public bool HasDeclaration { get; }

	public bool HasPage { get; }

	public int DeclarationCount { get; }

	public int AuthorizationDeclarationCount { get; }

	public Location Location { get; }

	public bool IsSupported =>
		Route.Length > 0 &&
		Policy.Length > 0 &&
		!HasPage &&
		DeclarationCount == 1 &&
		AuthorizationDeclarationCount == 1 &&
		HasConstrainedParameter(Route);

	public static RouteDeclaration Read(AdditionalText file, CancellationToken cancellationToken)
	{
		var source = file.GetText(cancellationToken);
		if (source is null)
		{
			return Empty(file.Path);
		}

		var routeLineIndex = FindLine(source, "HtmxRoute(");
		if (routeLineIndex < 0)
		{
			return Empty(file.Path);
		}

		var routeLine = source.Lines[routeLineIndex];
		var route = ExtractValue(routeLine.ToString().Trim(), RoutePrefix, RouteSuffix);
		var policy = FindValue(source, PolicyPrefix, PolicySuffix);

		return new RouteDeclaration(
			file.Path,
			System.IO.Path.GetFileNameWithoutExtension(file.Path),
			route ?? string.Empty,
			policy ?? string.Empty,
			hasDeclaration: true,
			hasPage: FindLine(source, "@page") >= 0,
			declarationCount: CountLines(source, "HtmxRoute("),
			authorizationDeclarationCount: CountAuthorizationDeclarations(source),
			Location.Create(file.Path, routeLine.Span, source.Lines.GetLinePositionSpan(routeLine.Span)));
	}

	private static RouteDeclaration Empty(string path)
		=> new(
			path,
			string.Empty,
			string.Empty,
			string.Empty,
			hasDeclaration: false,
			hasPage: false,
			declarationCount: 0,
			authorizationDeclarationCount: 0,
			Location.None);

	private static bool HasConstrainedParameter(string route)
	{
		var openingBrace = route.IndexOf('{');
		var constraint = route.IndexOf(':', openingBrace + 1);
		var closingBrace = route.IndexOf('}', constraint + 1);

		return openingBrace >= 0 && constraint > openingBrace && closingBrace > constraint;
	}

	private static int FindLine(SourceText source, string marker)
	{
		for (var index = 0; index < source.Lines.Count; index++)
		{
			if (source.Lines[index].ToString().IndexOf(marker, StringComparison.Ordinal) >= 0)
			{
				return index;
			}
		}

		return -1;
	}

	private static int CountLines(SourceText source, string marker)
	{
		var count = 0;
		foreach (var line in source.Lines)
		{
			if (line.ToString().IndexOf(marker, StringComparison.Ordinal) >= 0)
			{
				count++;
			}
		}

		return count;
	}

	private static int CountAuthorizationDeclarations(SourceText source)
	{
		var count = 0;
		foreach (var line in source.Lines)
		{
			var text = line.ToString().TrimStart();
			if (!text.StartsWith("@attribute [", StringComparison.Ordinal))
			{
				continue;
			}

			var startIndex = 0;
			while ((startIndex = text.IndexOf("Authorize", startIndex, StringComparison.Ordinal)) >= 0)
			{
				count++;
				startIndex += "Authorize".Length;
			}
		}

		return count;
	}

	private static string? FindValue(SourceText source, string prefix, string suffix)
	{
		foreach (var line in source.Lines)
		{
			var value = ExtractValue(line.ToString().Trim(), prefix, suffix);
			if (value is not null)
			{
				return value;
			}
		}

		return null;
	}

	private static string? ExtractValue(string line, string prefix, string suffix)
	{
		if (!line.StartsWith(prefix, StringComparison.Ordinal) ||
			!line.EndsWith(suffix, StringComparison.Ordinal))
		{
			return null;
		}

		return line.Substring(prefix.Length, line.Length - prefix.Length - suffix.Length);
	}
}
