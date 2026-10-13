using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Htmxor.Builder;

public sealed class HtmxorExplicitActionCatalogTests
{
	[Fact]
	public void Compiled_explicit_interface_action_fails_closed_at_registration()
	{
		var (assembly, owner) = CompileCSharpOwner("[Htmxor.HtmxAction(\"PUT\")] void IHandler.Save() { }", withInterface: true);
		var handler = Assert.Single(owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic)
			.Where(static method => method.IsDefined(typeof(HtmxActionAttribute), inherit: false)));
		Assert.Equal("Consumer.IHandler.Save", handler.Name);
		Assert.True(handler.IsPrivate);
		Assert.True(handler.IsVirtual);
		Assert.True(handler.IsFinal);
		Assert.False(handler.IsSpecialName);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Contains("invalid explicit action", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Compiled_implicit_interface_action_remains_available()
	{
		var (assembly, owner) = CompileCSharpOwner("[Htmxor.HtmxAction(\"PUT\")] public void Save() { }", withInterface: true);
		var handler = Assert.Single(owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic)
			.Where(static method => method.IsDefined(typeof(HtmxActionAttribute), inherit: false)));
		Assert.Equal("Save", handler.Name);
		var action = Assert.Single(HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Equal("PUT", action.HttpMethod);
		Assert.Equal(owner, action.ComponentType);
		Assert.Equal(handler, action.ExplicitHandler);
	}

	[Fact]
	public void Compiled_special_name_ordinary_action_remains_available()
	{
		var (assembly, owner) = CompileCSharpOwner("[System.Runtime.CompilerServices.SpecialName] [Htmxor.HtmxAction(\"PUT\")] private void Save() { }");
		var handler = Assert.Single(owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic)
			.Where(static method => method.IsDefined(typeof(HtmxActionAttribute), inherit: false)));
		Assert.Equal("Save", handler.Name);
		Assert.True(handler.IsSpecialName);
		var action = Assert.Single(HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Equal("PUT", action.HttpMethod);
		Assert.Equal(owner, action.ComponentType);
		Assert.Equal(handler, action.ExplicitHandler);
	}

	[Theory]
	[InlineData("public Htmxor.HtmxEventArgs Value { get => null!; [Htmxor.HtmxAction(\"PUT\")] set { } }")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] ~Owner() { }")]
	public void Compiled_nonordinary_action_fails_closed_at_registration(string member)
	{
		var (assembly, owner) = CompileCSharpOwner(member);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Contains("invalid explicit action", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] void Save() { _ = this.GetHashCode(); } Save(); }")]
	[InlineData("private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] () => { _ = this.GetHashCode(); };")]
	public void Compiled_instance_capture_helper_fails_closed_at_registration(string member)
	{
		var (assembly, owner) = CompileCSharpOwner(member);
		var annotated = owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic)
			.Where(static method => method.IsDefined(typeof(HtmxActionAttribute), inherit: false)).ToArray();
		Assert.Single(annotated);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Contains("invalid explicit action", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static void Save() { } Save(); }", true)]
	[InlineData("private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] static () => { };", false)]
	public void Compiled_static_helper_is_already_rejected_at_registration(string member, bool onOwner)
	{
		var (assembly, owner) = CompileCSharpOwner(member);
		var annotated = owner.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(static method => method.IsDefined(typeof(HtmxActionAttribute), inherit: false)).ToArray();
		if (onOwner) Assert.True(Assert.Single(annotated).IsStatic);
		else Assert.Empty(annotated);
		Assert.Contains(assembly.DefinedTypes.SelectMany(static type => type.GetMethods(BindingFlags.DeclaredOnly |
			BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)),
			static method => method.IsDefined(typeof(HtmxActionAttribute), inherit: false));

		var exception = Assert.Throws<InvalidOperationException>(() =>
			HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Contains("invalid explicit action", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private void Save() { }")]
	[InlineData("[Htmxor.HtmxAction(\"PUT\")] private void Finalize() { }")]
	[InlineData("[System.Runtime.CompilerServices.CompilerGenerated] [Htmxor.HtmxAction(\"PUT\")] private void Save() { }")]
	public void Compiled_ordinary_action_remains_available(string member)
	{
		var (assembly, owner) = CompileCSharpOwner(member);

		var action = Assert.Single(HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Equal("PUT", action.HttpMethod);
		Assert.Equal(owner, action.ComponentType);
	}

	private static (Assembly Assembly, Type Owner) CompileCSharpOwner(string member, bool withInterface = false)
	{
		var interfaceDeclaration = withInterface ? "public interface IHandler { void Save(); }" : "";
		var interfaceImplementation = withInterface ? ", IHandler" : "";
		var source = $$"""
			namespace Consumer;
			{{interfaceDeclaration}}
			[Microsoft.AspNetCore.Components.Route("/explicit")]
			public class Owner : Microsoft.AspNetCore.Components.ComponentBase{{interfaceImplementation}}
			{
				{{member}}
			}
			""";
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
			.Concat([typeof(ComponentBase).Assembly.Location, typeof(HtmxActionAttribute).Assembly.Location])
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static path => MetadataReference.CreateFromFile(path));
		var compilation = CSharpCompilation.Create("Issue180Catalog" + Guid.NewGuid().ToString("N"),
			[CSharpSyntaxTree.ParseText(source)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Assert.Empty(compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		using var stream = new MemoryStream();
		var result = compilation.Emit(stream);
		Assert.True(result.Success, string.Join("; ", result.Diagnostics));
		var assembly = Assembly.Load(stream.ToArray());
		return (assembly, assembly.GetType("Consumer.Owner", throwOnError: true)!);
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Compiled_explicit_action_joins_the_omitted_allowlist_and_stock_forms(bool namedForm)
	{
		var (assembly, owner) = CompileOwner();

		var descriptor = Assert.Single(HtmxorAttributedRouteCatalog.Build(assembly, [owner.FullName!], [], namedForm ? [owner.FullName!] : []));

		Assert.Equal(namedForm ? ["GET", "POST", "PUT"] : ["GET", "PUT"], descriptor.HttpMethods);
	}

	[Fact]
	public void Routed_base_keeps_its_own_explicit_action_method()
	{
		var (assembly, baseOwner, derivedOwner) = CompileRoutedHierarchy();

		var routes = HtmxorAttributedRouteCatalog.Build(assembly, [baseOwner.FullName!, derivedOwner.FullName!]);
		var route = Assert.Single(routes, descriptor => descriptor.ComponentType == baseOwner);

		Assert.Equal(["GET", "PUT"], route.HttpMethods);
	}

	[Fact]
	public void Independently_routed_derived_owner_has_no_inherited_action_method()
	{
		var (assembly, baseOwner, derivedOwner) = CompileRoutedHierarchy();

		var routes = HtmxorAttributedRouteCatalog.Build(assembly, [baseOwner.FullName!, derivedOwner.FullName!]);
		var route = Assert.Single(routes, descriptor => descriptor.ComponentType == derivedOwner);

		Assert.Equal(["GET"], route.HttpMethods);
	}

	private static (Assembly Assembly, Type BaseOwner, Type DerivedOwner) CompileRoutedHierarchy()
	{
		var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Issue180Hierarchy" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
		var module = assembly.DefineDynamicModule("Owners");
		var baseOwner = module.DefineType("Unexpected.Namespace.BaseOwner", TypeAttributes.Public, typeof(ComponentBase));
		baseOwner.SetCustomAttribute(new CustomAttributeBuilder(typeof(HtmxRouteAttribute).GetConstructor([typeof(string)])!, ["/catalog-base"]));
		// Public makes the base MethodInfo visible to an inherited-method scan on the derived owner.
		var handler = baseOwner.DefineMethod("Save", MethodAttributes.Public, typeof(void), Type.EmptyTypes);
		handler.SetCustomAttribute(new CustomAttributeBuilder(typeof(HtmxActionAttribute).GetConstructor([typeof(string)])!, ["PUT"]));
		handler.GetILGenerator().Emit(OpCodes.Ret);
		var compiledBase = baseOwner.CreateType()!;
		var derivedOwner = module.DefineType("Unexpected.Namespace.DerivedOwner", TypeAttributes.Public, compiledBase);
		derivedOwner.SetCustomAttribute(new CustomAttributeBuilder(typeof(HtmxRouteAttribute).GetConstructor([typeof(string)])!, ["/catalog-derived"]));
		return (assembly, compiledBase, derivedOwner.CreateType()!);
	}

	private static (Assembly Assembly, Type Owner) CompileOwner()
	{
		var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Issue180Catalog" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
		var module = assembly.DefineDynamicModule("Owner");
		var owner = module.DefineType("Unexpected.Namespace.Owner", TypeAttributes.Public, typeof(ComponentBase));
		owner.SetCustomAttribute(new CustomAttributeBuilder(typeof(HtmxRouteAttribute).GetConstructor([typeof(string)])!, ["/catalog-explicit"]));
		var handler = owner.DefineMethod("Save", MethodAttributes.Private, typeof(void), Type.EmptyTypes);
		handler.SetCustomAttribute(new CustomAttributeBuilder(typeof(HtmxActionAttribute).GetConstructor([typeof(string)])!, ["put"]));
		handler.GetILGenerator().Emit(OpCodes.Ret);
		return (assembly, owner.CreateType()!);
	}
}
