using System.Runtime.CompilerServices;
using System.Text;
using AngleSharp.Diffing.Core;
using AngleSharp.Dom;
using Bunit;
using Bunit.Diffing;
using Bunit.Rendering;
using Microsoft.AspNetCore.Http;

namespace Htmxor.TestAssets;

public sealed class SemanticHtmlContentBodyAssertion : IScenarioAssertion
{
    public string Expected { get; }

    public SemanticHtmlContentBodyAssertion(string expected)
    {
        Expected = expected;
    }

    public void Assert(Scenario scenario, HttpContext context, ScenarioAssertionException ex)
    {
        var received = ex.ReadBody(context);

        using var parser = new BunitHtmlParser();
        var receivedNodes = parser.Parse(received);
        var expectedNodes = parser.Parse(Expected);
        var diffs = receivedNodes.CompareTo(expectedNodes);

        if (diffs.Count > 0)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Response body does not contain the expected HTML:");
            builder.AppendLine();
            CreateDiffMessage(
                ((IEnumerable<INode>)receivedNodes).ToDiffMarkup(),
                ((IEnumerable<INode>)expectedNodes).ToDiffMarkup(),
                diffs,
                builder);
            ex.Add(builder.ToString());
        }
    }
    private static void CreateDiffMessage(string received, string verified, IReadOnlyList<IDiff> diffs, StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("HTML comparison failed. The following errors were found:");

        for (var i = 0; i < diffs.Count; i++)
        {
            builder.Append($"  {i + 1}: ");
            builder.AppendLine(diffs[i] switch
            {
                NodeDiff { Target: DiffTarget.Text } diff when diff.Control.Path.Equals(diff.Test.Path, StringComparison.Ordinal)
                    => $"The text in {diff.Control.Path} is different.",
                NodeDiff { Target: DiffTarget.Text } diff => $"The expected {NodeName(diff.Control)} at {diff.Control.Path} and the actual {NodeName(diff.Test)} at {diff.Test.Path} is different.",
                NodeDiff diff when diff.Control.Path.Equals(diff.Test.Path, StringComparison.Ordinal)
                    => $"The {NodeName(diff.Control)}s at {diff.Control.Path} are different.",
                NodeDiff diff => $"The expected {NodeName(diff.Control)} at {diff.Control.Path} and the actual {NodeName(diff.Test)} at {diff.Test.Path} are different.",
                AttrDiff diff when diff.Control.Path.Equals(diff.Test.Path, StringComparison.Ordinal)
                    => $"The values of the attributes at {diff.Control.Path} are different.",
                AttrDiff diff => $"The value of the attribute {diff.Control.Path} and actual attribute {diff.Test.Path} are different.",
                MissingNodeDiff diff => $"The {NodeName(diff.Control)} at {diff.Control.Path} is missing.",
                MissingAttrDiff diff => $"The attribute at {diff.Control.Path} is missing.",
                UnexpectedNodeDiff diff => $"The {NodeName(diff.Test)} at {diff.Test.Path} was not expected.",
                UnexpectedAttrDiff diff => $"The attribute at {diff.Test.Path} was not expected.",
                _ => throw new SwitchExpressionException($"Unknown diff type detected: {diffs[i].GetType()}")
            });
        }

        builder.AppendLine(
            $"""

             Actual HTML:

             {received}

             Expected HTML:

             {verified}
             """);

        static string NodeName(ComparisonSource source) => source.Node.NodeType.ToString().ToLowerInvariant();
    }
}