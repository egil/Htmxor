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
}
