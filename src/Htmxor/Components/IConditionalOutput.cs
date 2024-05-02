namespace Htmxor.Components;

/// <summary>
/// Represents a component that can conditionally produce markup.
/// </summary>
public interface IConditionalOutputComponent
{
    /// <summary>
    /// Determine whether this component should produce any markup during a request.
    /// </summary>
    /// <param name="conditionalChildren">The number of children that implements <see cref="IConditionalOutputComponent"/>.</param>
    /// <returns><see langword="true"/> if the component should produce markup, <see langword="false"/> otherwise.</returns>
    bool ShouldOutput(int conditionalChildren);
}

