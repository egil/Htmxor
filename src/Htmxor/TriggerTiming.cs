namespace Htmxor;

public enum TriggerTiming
{
	/// <summary>
	/// Trigger events as soon as the response is received
	/// </summary>
	Default,

	/// <summary>
	/// Trigger events after the settling step
	/// </summary>
	AfterSettle,

	/// <summary>
	/// Trigger events after the swap step
	/// </summary>
	AfterSwap
}
