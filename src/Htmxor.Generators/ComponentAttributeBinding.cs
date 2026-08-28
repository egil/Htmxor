using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class ComponentAttributeBinding
{
	private ComponentAttributeBinding(
		ImmutableArray<BoundComponentAttribute> attributes,
		bool hasErrors,
		Compilation compilation)
	{
		Attributes = attributes;
		HasErrors = hasErrors;
		Compilation = compilation;
	}

	public ImmutableArray<BoundComponentAttribute> Attributes { get; }

	public bool HasErrors { get; }

	private Compilation Compilation { get; }

	public ImmutableArray<BoundComponentAttribute> FindAttributes(string metadataName)
	{
		var expectedType = Compilation.GetTypeByMetadataName(metadataName);
		if (expectedType is null)
		{
			return ImmutableArray<BoundComponentAttribute>.Empty;
		}

		return Attributes
			.Where(attribute => SymbolEqualityComparer.Default.Equals(
				attribute.Data.AttributeClass,
				expectedType))
			.ToImmutableArray();
	}

	public static ComponentAttributeBinding Bind(
		RazorDirectiveDocument component,
		ImmutableArray<RazorDirectiveDocument> imports,
		Compilation compilation,
		CSharpParseOptions parseOptions,
		string rootNamespace)
	{
		var synthetic = SyntheticComponent.Create(component, imports, parseOptions, rootNamespace);
		var augmentedCompilation = compilation.AddSyntaxTrees(synthetic.Tree);
		var semanticModel = augmentedCompilation.GetSemanticModel(synthetic.Tree);
		var declaration = synthetic.Tree.GetRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.SingleOrDefault();
		if (declaration is null)
		{
			return new ComponentAttributeBinding(
				ImmutableArray<BoundComponentAttribute>.Empty,
				hasErrors: true,
				augmentedCompilation);
		}

		var symbol = semanticModel.GetDeclaredSymbol(declaration);
		if (symbol is null)
		{
			return new ComponentAttributeBinding(
				ImmutableArray<BoundComponentAttribute>.Empty,
				hasErrors: true,
				augmentedCompilation);
		}

		var attributes = symbol.GetAttributes()
			.Where(attribute => attribute.ApplicationSyntaxReference?.SyntaxTree == synthetic.Tree)
			.Select(attribute => BoundComponentAttribute.Create(attribute, synthetic.Mappings))
			.ToImmutableArray();
		var expectedCount = declaration.AttributeLists.Sum(static list => list.Attributes.Count);
		var hasErrors = synthetic.Tree.GetDiagnostics().Any() ||
			attributes.Length != expectedCount ||
			attributes.Any(static attribute => attribute.HasErrors);

		return new ComponentAttributeBinding(attributes, hasErrors, augmentedCompilation);
	}
}

internal sealed class BoundComponentAttribute
{
	private BoundComponentAttribute(AttributeData data, Location location, bool hasErrors)
	{
		Data = data;
		Location = location;
		HasErrors = hasErrors;
	}

	public AttributeData Data { get; }

	public Location Location { get; }

	public bool HasErrors { get; }

	public static BoundComponentAttribute Create(
		AttributeData data,
		ImmutableArray<AttributeSpanMapping> mappings)
	{
		var syntaxReference = data.ApplicationSyntaxReference;
		var location = syntaxReference is null
			? Location.None
			: MapLocation(syntaxReference.Span, mappings);
		return new BoundComponentAttribute(data, location, HasBindingErrors(data));
	}

	private static bool HasBindingErrors(AttributeData data)
		=> data.AttributeClass is null ||
		data.AttributeClass.TypeKind == TypeKind.Error ||
		data.AttributeConstructor is null ||
		data.ConstructorArguments.Any(HasTypedConstantErrors) ||
		data.NamedArguments.Any(static pair => HasTypedConstantErrors(pair.Value));

	private static bool HasTypedConstantErrors(TypedConstant constant)
		=> constant.Kind == TypedConstantKind.Error ||
		(constant.Kind == TypedConstantKind.Array && constant.Values.Any(HasTypedConstantErrors));

	private static Location MapLocation(
		TextSpan syntheticSpan,
		ImmutableArray<AttributeSpanMapping> mappings)
	{
		foreach (var mapping in mappings)
		{
			if (!mapping.Contains(syntheticSpan))
			{
				continue;
			}

			return mapping.Original.Location;
		}

		return Location.None;
	}
}

internal readonly struct AttributeSpanMapping(
	TextSpan syntheticSpan,
	RazorAttributeDirective original)
{
	public TextSpan SyntheticSpan { get; } = syntheticSpan;

	public RazorAttributeDirective Original { get; } = original;

	public bool Contains(TextSpan span)
		=> span.Start >= SyntheticSpan.Start && span.End <= SyntheticSpan.End;
}

internal sealed class SyntheticComponent
{
	private SyntheticComponent(
		SyntaxTree tree,
		ImmutableArray<AttributeSpanMapping> mappings)
	{
		Tree = tree;
		Mappings = mappings;
	}

	public SyntaxTree Tree { get; }

	public ImmutableArray<AttributeSpanMapping> Mappings { get; }

	public static SyntheticComponent Create(
		RazorDirectiveDocument component,
		ImmutableArray<RazorDirectiveDocument> imports,
		CSharpParseOptions parseOptions,
		string rootNamespace)
	{
		var source = new StringBuilder();
		foreach (var import in imports)
		{
			AppendUsings(source, import);
		}

		AppendUsings(source, component);
		AppendNamespaceStart(source, rootNamespace);
		var mappings = ImmutableArray.CreateBuilder<AttributeSpanMapping>();
		foreach (var import in imports)
		{
			AppendAttributes(source, import, mappings);
		}

		AppendAttributes(source, component, mappings);
		source.Append("partial class ").Append(component.ComponentName).AppendLine(" { }");
		AppendNamespaceEnd(source, rootNamespace);
		var tree = CSharpSyntaxTree.ParseText(
			SourceText.From(source.ToString(), Encoding.UTF8),
			parseOptions,
			component.File.Path + ".htmxor-attributes.g.cs");

		return new SyntheticComponent(tree, mappings.ToImmutable());
	}

	private static void AppendUsings(StringBuilder source, RazorDirectiveDocument document)
	{
		foreach (var directive in document.Usings)
		{
			source.AppendLine(directive);
		}
	}

	private static void AppendAttributes(
		StringBuilder source,
		RazorDirectiveDocument document,
		ImmutableArray<AttributeSpanMapping>.Builder mappings)
	{
		foreach (var attribute in document.Attributes)
		{
			var start = source.Length;
			source.AppendLine(attribute.Text);
			mappings.Add(new AttributeSpanMapping(
				new TextSpan(start, attribute.Text.Length),
				attribute));
		}
	}

	private static void AppendNamespaceStart(StringBuilder source, string rootNamespace)
	{
		if (rootNamespace.Length > 0)
		{
			source.Append("namespace ").Append(rootNamespace).AppendLine().AppendLine("{");
		}
	}

	private static void AppendNamespaceEnd(StringBuilder source, string rootNamespace)
	{
		if (rootNamespace.Length > 0)
		{
			source.AppendLine("}");
		}
	}
}
