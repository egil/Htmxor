using System.Diagnostics.CodeAnalysis;
using System.Text.Json.JsonDiffPatch.Xunit;
using System.Text.Json.Nodes;
using FluentAssertions.Primitives;

namespace FluentAssertions;

internal static class JsonStringSemanticAssertionsExtensions
{
    public static AndConstraint<StringAssertions> BeJsonSemanticallyEqualTo(
        this StringAssertions assertions,
        [StringSyntax(StringSyntaxAttribute.Json)] string expected)
    {
        JsonAssert.Equal(
            JsonNode.Parse(expected),
            JsonNode.Parse(assertions.Subject),
            output: true);
        return new AndConstraint<StringAssertions>(assertions);
    }
}