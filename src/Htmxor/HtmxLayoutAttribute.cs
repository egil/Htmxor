using Microsoft.AspNetCore.Components;

namespace Htmxor;

/// <summary>
/// Indicates that the associated component type uses a specified layout during Htmx requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class HtmxLayoutAttribute : Attribute
{
	/// <summary>
	/// Constructs an instance of <see cref="LayoutAttribute"/>.
	/// </summary>
	/// <param name="layoutType">The type of the layout.</param>
	public HtmxLayoutAttribute([DynamicallyAccessedMembers(LinkerFlags.Component)] Type layoutType)
	{
		LayoutType = layoutType ?? throw new ArgumentNullException(nameof(layoutType));

		if (!typeof(IComponent).IsAssignableFrom(layoutType))
		{
			throw new ArgumentException(
				$"Invalid layout type: {layoutType.FullName} " +
				$"does not implement {typeof(IComponent).FullName}.",
				nameof(layoutType));
		}

		// Note that we can't validate its acceptance of a 'Body' parameter at this stage,
		// because the contract doesn't force them to be known statically. However it will
		// be a runtime error if the referenced component type rejects the 'Body' parameter
		// when it gets used.
	}

	/// <summary>
	/// The type of the layout. The type must implement <see cref="IComponent"/>
	/// and must accept a parameter with the name 'Body'.
	/// </summary>
	public Type LayoutType { get; private set; }

	/// <summary>
	/// The priority of the layout. Layouts with higher priority
	/// will be used over layouts with lower priority.
	/// </summary>
	/// <remarks>
	/// If multiple <see cref="HtmxLayoutAttribute"/> instances are applied to a component,
	/// the <see cref="Priority"/> is used to determine which one to use during rendering.
	/// This allows a default htmx layout to be defined in a base class or in <c>_Imports.razor</c>,
	/// that can be overridden in a derived class or in a component itself.
	/// </remarks>
	public int Priority { get; set; }
}
