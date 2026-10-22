using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators;

internal sealed class RazorCompiledForm
{
	private RazorCompiledForm(INamedTypeSymbol component, bool isEditForm, Location location)
	{
		Component = component;
		IsEditForm = isEditForm;
		IsPost = isEditForm;
		Location = location;
	}

	public INamedTypeSymbol Component { get; }

	public bool IsEditForm { get; }

	public bool IsNamed { get; private set; }

	public bool HasValidName { get; private set; } = true;

	public bool IsValid { get; private set; } = true;

	public bool IsPost { get; private set; }

	public Location Location { get; private set; }

	public static IEnumerable<RazorCompiledForm> Find(Compilation compilation, CancellationToken cancellationToken)
	{
		foreach (var tree in compilation.SyntaxTrees.Where(static tree => HtmxorRouteManifest.IsRazorGeneratedPath(tree.FilePath)))
		{
			foreach (var form in Find(compilation.GetSemanticModel(tree), cancellationToken))
			{
				yield return form;
			}
		}
	}

	public static IEnumerable<RazorCompiledForm> Find(SemanticModel model, CancellationToken cancellationToken)
	{
		if (!HtmxorRouteManifest.IsRazorGeneratedPath(model.SyntaxTree.FilePath))
		{
			yield break;
		}
		foreach (var method in model.SyntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
			.Where(static method => method.Identifier.ValueText == "BuildRenderTree"))
		{
			if (model.GetDeclaredSymbol(method, cancellationToken)?.ContainingType is not { } component ||
				!HtmxorRouteManifest.HasCompiledRazorDeclaration(component))
			{
				continue;
			}
			foreach (var form in ReadMethod(model, method, component, cancellationToken))
			{
				yield return form;
			}
		}
	}

	private static IEnumerable<RazorCompiledForm> ReadMethod(
		SemanticModel model, MethodDeclarationSyntax method, INamedTypeSymbol component, CancellationToken cancellationToken)
	{
		var stacks = new Dictionary<ISymbol, Stack<RazorCompiledForm?>>(SymbolEqualityComparer.Default);
		foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (ReadBuilderCall(model, invocation, cancellationToken) is not { } call)
			{
				continue;
			}

			var (builderMethod, receiver) = call;
			if (!stacks.TryGetValue(receiver, out var stack))
			{
				stack = new Stack<RazorCompiledForm?>();
				stacks.Add(receiver, stack);
			}

			foreach (var form in ReadCall(model, invocation, component, builderMethod.Name, stack, cancellationToken))
			{
				yield return form;
			}
		}
	}

	private static (IMethodSymbol Method, ISymbol Receiver)? ReadBuilderCall(
		SemanticModel model, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
		=> invocation.Expression is MemberAccessExpressionSyntax member &&
			model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method &&
			method.ContainingType.ToDisplayString() == "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder" &&
			model.GetSymbolInfo(member.Expression, cancellationToken).Symbol is { } receiver
				? (method, receiver) : null;

	private static IEnumerable<RazorCompiledForm> ReadCall(
		SemanticModel model, InvocationExpressionSyntax invocation, INamedTypeSymbol component,
		string name, Stack<RazorCompiledForm?> stack, CancellationToken cancellationToken)
	{
		if (name is "OpenElement" or "OpenComponent")
		{
			var method = (IMethodSymbol)model.GetSymbolInfo(invocation, cancellationToken).Symbol!;
			var form = Open(model, invocation, component, method, cancellationToken);
			stack.Push(form);
			if (form is not null)
			{
				yield return form;
			}
		}
		else if (name is "CloseElement" or "CloseComponent")
		{
			if (stack.Count > 0)
			{
				stack.Pop();
			}
		}
		else if (name == "AddMarkupContent")
		{
			foreach (var form in HasErrors(model, invocation, cancellationToken)
				? Array.Empty<RazorCompiledForm>() : ReadStaticMarkup(model, invocation, component, cancellationToken))
			{
				yield return form;
			}
		}
		else if (stack.Count > 0 && stack.Peek() is { } form)
		{
			form.ReadAttribute(model, invocation, name, cancellationToken);
		}
	}

	private static RazorCompiledForm? Open(
		SemanticModel model, InvocationExpressionSyntax invocation, INamedTypeSymbol component,
		IMethodSymbol method, CancellationToken cancellationToken)
	{
		var isEditForm = method.Name == "OpenComponent" && method.TypeArguments.Length == 1 &&
			SymbolEqualityComparer.Default.Equals(method.TypeArguments[0],
				model.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.Forms.EditForm"));
		var isPlainForm = method.Name == "OpenElement" && string.Equals(ReadString(model, invocation, 1, cancellationToken), "form", StringComparison.OrdinalIgnoreCase);
		return isEditForm || isPlainForm ? new RazorCompiledForm(component, isEditForm, invocation.GetLocation())
		{
			IsValid = !HasErrors(model, invocation, cancellationToken),
		} : null;
	}

	private void ReadAttribute(SemanticModel model, InvocationExpressionSyntax invocation, string method, CancellationToken cancellationToken)
	{
		var attribute = ReadString(model, invocation, method == "AddNamedEvent" ? 0 : 1, cancellationToken);
		Location = FindMappedLocation(invocation) ?? Location;
		if (IsNameAttribute(method, attribute))
		{
			ReadName(model, invocation, method, cancellationToken);
		}
		else if (method == "AddAttribute" && !HasErrors(model, invocation, cancellationToken) &&
			string.Equals(attribute, "method", StringComparison.OrdinalIgnoreCase) &&
			string.Equals(ReadString(model, invocation, 2, cancellationToken), "post", StringComparison.OrdinalIgnoreCase))
		{
			IsPost = true;
		}
	}

	private void ReadName(SemanticModel model, InvocationExpressionSyntax invocation, string method, CancellationToken cancellationToken)
	{
		IsNamed = true;
		IsPost = true;
		// Keep the authored shape for location matching even when its name is compiler-owned.
		HasValidName &= !HasErrors(model, invocation, cancellationToken) &&
			(method != "AddNamedEvent" || HasValidNamedEventInitializer(model, invocation, cancellationToken));
		Location = FindMappedLocation(invocation) ?? FindNamedEventLocation(model, invocation, cancellationToken) ?? invocation.GetLocation();
	}

	private bool IsNameAttribute(string method, string? attribute)
		=> IsEditForm
			? method is "AddComponentParameter" or "AddAttribute" && attribute == "FormName"
			: method == "AddNamedEvent" && attribute == "onsubmit";

	private static Location? FindNamedEventLocation(SemanticModel model, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var expression = invocation.ArgumentList.Arguments.LastOrDefault()?.Expression;
		var symbol = expression is null ? null : model.GetSymbolInfo(expression, cancellationToken).Symbol;
		return symbol?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken))
			.OfType<VariableDeclaratorSyntax>().SelectMany(static variable => variable.DescendantNodes())
			.Select(static node => node.GetLocation())
			.FirstOrDefault(static location => location.GetMappedLineSpan().Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));
	}

	private static bool HasValidNamedEventInitializer(SemanticModel model, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var expression = invocation.ArgumentList.Arguments.LastOrDefault()?.Expression;
		var symbol = expression is null ? null : model.GetSymbolInfo(expression, cancellationToken).Symbol;
		return symbol?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken))
			.OfType<VariableDeclaratorSyntax>().All(variable => !HasErrors(model, variable, cancellationToken)) ?? true;
	}

	private static Location? FindMappedLocation(InvocationExpressionSyntax invocation)
		=> invocation.DescendantNodes().Select(static node => node.GetLocation())
			.FirstOrDefault(static location => location.GetMappedLineSpan().Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

	private static IEnumerable<RazorCompiledForm> ReadStaticMarkup(
		SemanticModel model, InvocationExpressionSyntax invocation, INamedTypeSymbol component, CancellationToken cancellationToken)
	{
		var markup = ReadString(model, invocation, 1, cancellationToken);
		return markup is null ? Array.Empty<RazorCompiledForm>() : RazorFormDeclaration.Read(SourceText.From(markup), string.Empty)
			.Where(static form => !form.IsEditForm && form.IsPost)
			.Select(form => new RazorCompiledForm(component, false, invocation.GetLocation()) { IsPost = true, IsNamed = form.IsNamed });
	}

	private static bool HasErrors(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
		=> model.GetDiagnostics(node.Span, cancellationToken).Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

	private static string? ReadString(SemanticModel model, InvocationExpressionSyntax invocation, int index, CancellationToken cancellationToken)
		=> invocation.ArgumentList.Arguments.Count > index
			? model.GetConstantValue(invocation.ArgumentList.Arguments[index].Expression, cancellationToken).Value as string
			: null;
}
