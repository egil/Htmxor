namespace Htmxor;

/// <summary>
/// htmx configuration allows for the creation of a trigger specification cache in order to improve
/// trigger-handling performance.  The cache is a key/value store mapping well-formed hx-trigger parameters
/// to their parsed specifications.
/// </summary>
public class TriggerSpecificationCache : Dictionary<string, IReadOnlyList<HtmxTriggerSpecification>>
{
	public TriggerSpecificationCache() : base(StringComparer.Ordinal)
	{
	}

	public TriggerSpecificationCache(IEnumerable<KeyValuePair<string, IReadOnlyList<HtmxTriggerSpecification>>> triggers)
		: base(triggers, StringComparer.Ordinal)
	{
	}

	public TriggerSpecificationCache(params KeyValuePair<string, IReadOnlyList<HtmxTriggerSpecification>>[] triggers)
		: base(triggers, StringComparer.Ordinal)
	{
	}

	public void Add(KeyValuePair<string, IReadOnlyList<HtmxTriggerSpecification>> triggerAndValue)
	{
		Add(triggerAndValue.Key, triggerAndValue.Value);
	}
}
