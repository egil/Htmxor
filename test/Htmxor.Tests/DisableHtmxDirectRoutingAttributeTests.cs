using System.Reflection;

namespace Htmxor;

public sealed class DisableHtmxDirectRoutingAttributeTests
{
	[Fact]
	public void Marker_is_a_sealed_parameterless_class_attribute_that_is_neither_repeatable_nor_inherited()
	{
		var type = typeof(DisableHtmxDirectRoutingAttribute);

		Assert.True(type.IsSealed);
		Assert.Empty(Assert.Single(type.GetConstructors()).GetParameters());
		Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

		var usage = type.GetCustomAttribute<AttributeUsageAttribute>();
		Assert.NotNull(usage);
		Assert.Equal(AttributeTargets.Class, usage!.ValidOn);
		Assert.False(usage.AllowMultiple);
		Assert.False(usage.Inherited);
	}
}
