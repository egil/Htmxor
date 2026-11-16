using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Htmxor.TestAssets.Alba;
using Microsoft.JSInterop.WebAssembly;

namespace Htmxor.Rendering;

public class UnloadableAssemblyTypesTests : TestAppTestBase
{
	private static Assembly? assemblyWithUnloadableTypes;

	public UnloadableAssemblyTypesTests(TestAppFixture fixture) : base(fixture)
	{
	}

	// The renderer's type lookup runs once per process, so the assembly must be
	// loaded before any test in this process can render.
	[ModuleInitializer]
	[SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Test assembly needs process-wide setup before the first render.")]
	internal static void LoadAssemblyWithUnloadableTypes()
		=> assemblyWithUnloadableTypes = typeof(WebAssemblyJSRuntime).Assembly;

	[Fact]
	public async Task Renders_component_when_a_loaded_assembly_has_unloadable_types()
	{
		assemblyWithUnloadableTypes.Should().NotBeNull();
		AppDomain.CurrentDomain.GetAssemblies().Should().Contain(assemblyWithUnloadableTypes!);
		assemblyWithUnloadableTypes!.Invoking(a => a.GetTypes()).Should().Throw<ReflectionTypeLoadException>();

		await Host.Scenario(s =>
		{
			s.Get.Url("/normal-and-hx");
			s.WithHxHeaders();
			s.ContentShouldBeHtml("""
                <h1>Hello, world!</h1>
                """);
		});
	}
}
