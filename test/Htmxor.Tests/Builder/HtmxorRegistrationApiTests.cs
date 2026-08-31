using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Htmxor.Tests.Builder;

public sealed class HtmxorRegistrationApiTests
{
	[Fact]
	public void Runtime_package_does_not_expose_a_destination_registration_overload()
	{
		var publicRegistrationMethods = typeof(HtmxorComponentEndpointRouteBuilderExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(static method => method.Name == "AddHtmxorComponentEndpoints");

		Assert.Empty(publicRegistrationMethods);
	}

	[Fact]
	public void Runtime_package_does_not_retain_an_internal_destination_registration_bridge()
	{
		var compatibilityMethods = typeof(HtmxorComponentEndpointRouteBuilderExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			.Where(static method =>
				method.Name == "AddHtmxorComponentEndpoints" &&
				method.GetParameters().Length == 2);

		Assert.Empty(compatibilityMethods);
	}
}
