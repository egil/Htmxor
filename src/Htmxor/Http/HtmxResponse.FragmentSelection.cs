namespace Htmxor.Http;

public sealed partial class HtmxResponse
{
	private IReadOnlyList<string> selectedFragmentNames = Array.Empty<string>();

	/// <summary>
	/// Gets the application-selected server fragment names in caller order. An empty list
	/// represents whole-component output. Normal requests always write the whole component.
	/// </summary>
	public IReadOnlyList<string> SelectedFragmentNames => selectedFragmentNames;

	/// <summary>
	/// Selects the whole component output, replacing any previous server fragment selection.
	/// </summary>
	public HtmxResponse SelectWholeComponent()
	{
		selectedFragmentNames = Array.Empty<string>();
		return this;
	}

	/// <summary>
	/// Selects one stable server fragment name, independently of browser delivery headers.
	/// </summary>
	public HtmxResponse SelectFragment(string name) => SelectFragments(name);

	/// <summary>
	/// Selects stable server fragment names in caller order, replacing any previous selection.
	/// The caller's array is copied so later mutations cannot change this request's selection.
	/// </summary>
	/// <remarks>
	/// Names follow the identifier rules of <see cref="Htmxor.Components.HtmxFragment.Name"/>.
	/// Invalid, repeated, unknown, or overlapping ancestor/descendant selections are rejected against
	/// the completed direct-request render before response HTML is written.
	/// </remarks>
	public HtmxResponse SelectFragments(params string[] names)
	{
		ArgumentNullException.ThrowIfNull(names);
		selectedFragmentNames = Array.AsReadOnly((string[])names.Clone());
		return this;
	}
}
