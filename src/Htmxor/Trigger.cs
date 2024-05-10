using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Htmxor;

/// <summary>
/// Creates builder objects to begin the fluent chain for constructing triggers 
/// </summary>
public static class Trigger
{
	public static TriggerModifierBuilder OnEvent(string eventName) => new TriggerBuilder().OnEvent(eventName);
	public static TriggerModifierBuilder Sse(string sseEventName) => new TriggerBuilder().Sse(sseEventName);

	public static TriggerModifierBuilder Load() => new TriggerBuilder().Load();

	public static TriggerModifierBuilder Revealed() => new TriggerBuilder().Revealed();

	public static TriggerModifierBuilder Intersect(string? root = null, float? threshold = null) => new TriggerBuilder().Intersect(root, threshold);
	public static TriggerModifierBuilder Every(TimeSpan interval) => new TriggerBuilder().Every(interval);
	public static TriggerBuilder Custom(string triggerDefinition) => new TriggerBuilder().Custom(triggerDefinition);
}
