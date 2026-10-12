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
