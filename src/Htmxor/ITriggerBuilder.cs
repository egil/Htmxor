namespace Htmxor;

public interface ITriggerBuilder
{
	KeyValuePair<string, List<HtmxTriggerSpecification>> Build();
}
