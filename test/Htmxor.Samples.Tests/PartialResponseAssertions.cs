using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

namespace Htmxor.Samples.Tests;

internal static class PartialResponseAssertions
{
	private static readonly string[] VoidElements = ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"];

	public static void IsSingleFragment(string body, string root, string requiredContent, string excludedPageContent, string? id = null)
	{
		var fragment = Assert.Single(TopLevelElements(body));
		HasRoot(fragment, root, id);
		Assert.Contains(requiredContent, fragment, StringComparison.Ordinal);
		Assert.DoesNotContain(excludedPageContent, fragment, StringComparison.Ordinal);
	}

	public static void AreRowFragments(string body, string rowIdentifier, string continuation)
	{
		var rows = TopLevelElements(body);
		Assert.NotEmpty(rows);
		Assert.All(rows, row => HasRoot(row, "tr"));
		Assert.Contains(rows, row => row.Contains($"<th>{rowIdentifier}</th>", StringComparison.Ordinal));
		Assert.Contains(rows, row => row.Contains(continuation, StringComparison.Ordinal));
	}

	public static string ContactIdentifier(string page)
	{
		var contact = Regex.Match(page, @"<tbody>\s*<tr>\s*<th>(?<id>[^<]+)</th>", RegexOptions.CultureInvariant).Groups["id"].Value;
		Assert.True(Guid.TryParse(contact, out _), "The full page must expose a contact GUID in its first data row.");
		return contact;
	}

	public static void AreCounterAndToastFragments(string body)
	{
		var fragments = TopLevelElements(body);
		Assert.Equal(2, fragments.Count);
		HasRoot(fragments[0], "div", "counter");
		Assert.Contains("role=\"status\"", fragments[0], StringComparison.Ordinal);
		Assert.Contains("hx-put=\"/examples/out-of-band-outlets\"", fragments[0], StringComparison.Ordinal);
		HasRoot(fragments[1], "div");
		Assert.Contains("hx-swap-oob=\"afterbegin:#toast-container\"", fragments[1], StringComparison.Ordinal);
		Assert.Contains("Counter is now", fragments[1], StringComparison.Ordinal);
	}

	private static void HasRoot(string fragment, string tag, string? id = null)
	{
		Assert.Equal(tag, ReadTag(fragment, 0).Name, ignoreCase: true);
		if (id is not null)
		{
			using var document = new HtmlParser().ParseDocument(fragment);
			Assert.Equal(id, document.Body?.FirstElementChild?.Id);
		}
	}

	private static IReadOnlyList<string> TopLevelElements(string body)
	{
		var roots = new List<string>();
		var stack = new Stack<string>();
		var rootStart = -1;
		var cursor = 0;
		for (var index = body.IndexOf('<'); index >= 0;)
		{
			var tag = ReadTag(body, index);
			if (stack.Count == 0)
			{
				Assert.True(string.IsNullOrWhiteSpace(body[cursor..tag.Start]), "Only selected top-level elements may appear in a fragment response.");
				rootStart = tag.Start;
			}

			if (CompleteElement(stack, tag))
			{
				roots.Add(body[rootStart..tag.End]);
				cursor = tag.End;
			}

			index = body.IndexOf('<', tag.End);
		}

		Assert.Empty(stack);
		Assert.True(string.IsNullOrWhiteSpace(body[cursor..]), "Only selected top-level elements may appear after a fragment response.");
		return roots;
	}

	private static bool CompleteElement(Stack<string> stack, TopLevelTag tag)
	{
		if (tag.IsClosing)
		{
			Assert.Equal(stack.Pop(), tag.Name, ignoreCase: true);
			return stack.Count == 0;
		}

		if (!tag.IsVoid)
		{
			stack.Push(tag.Name);
			return false;
		}

		return stack.Count == 0;
	}

	private static TopLevelTag ReadTag(string body, int index)
	{
		var start = index;
		var end = body.IndexOf('>', start);
		Assert.True(end >= 0, "A fragment response must contain complete HTML tags.");
		var token = body[(start + 1)..end].Trim();
		Assert.False(token.StartsWith('!') || token.StartsWith('?'), "A fragment response must not include a document envelope.");
		var isClosing = token.StartsWith('/');
		var name = token.TrimStart('/').Split([' ', '/', '\t', '\r', '\n'], 2)[0];
		Assert.False(string.IsNullOrWhiteSpace(name), "A fragment response contained an unnamed HTML tag.");
		return new(start, end + 1, name, isClosing, token.EndsWith('/') || VoidElements.Contains(name, StringComparer.OrdinalIgnoreCase));
	}

	private readonly record struct TopLevelTag(int Start, int End, string Name, bool IsClosing, bool IsVoid);
}
