using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Htmxor.Http;

namespace Htmxor.TestAssets;

internal static class AlbaUrlExpressionExtensions
{
    public static Scenario WithHxHeaders(
        this Scenario scenario,
        bool? isBoosted = null,
        bool? isHistoryRestoreRequest = null,
        string? currentURL = null,
        string? target = null,
        string? triggerName = null,
        string? trigger = null,
        string? prompt = null)
    {
        scenario.WithRequestHeader(HtmxRequestHeaderNames.HtmxRequest, "true");

        if (isBoosted is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.Boosted, "true");
        }

        if (isHistoryRestoreRequest is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.HistoryRestoreRequest, "true");
        }

        if (currentURL is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.CurrentURL, currentURL);
        }

        if (target is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.Target, target);
        }

        if (triggerName is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.TriggerName, triggerName);
        }

        if (trigger is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.Trigger, trigger);
        }

        if (prompt is not null)
        {
            scenario.WithRequestHeader(HtmxRequestHeaderNames.Prompt, prompt);
        }

        return scenario;
    }
}
