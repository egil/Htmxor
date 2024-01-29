using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Htmxor.Configuration;

public enum TriggerStyle
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
