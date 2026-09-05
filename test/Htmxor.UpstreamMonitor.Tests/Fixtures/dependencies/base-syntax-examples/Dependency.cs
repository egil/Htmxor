using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Htmxor.UpstreamMonitor.Tests.BaseSyntaxFixtures.BaseSyntaxExamples;

internal static class Examples
{
	public const string Declaration = "class Dependency() : ComponentBase() { }";
	public const string GenericDeclaration = "class Dependency : InputBase<string> { }";
	public static Type InputType => typeof(InputBase<string>);
	public static ComponentBase Cast(object value) => (ComponentBase)value;
}

// class Dependency() : ComponentBase() { }
/* class Dependency : InputBase<string> { } */
