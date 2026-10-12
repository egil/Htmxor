using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Builder;

public sealed class HtmxorExplicitActionCatalogTests
{
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
