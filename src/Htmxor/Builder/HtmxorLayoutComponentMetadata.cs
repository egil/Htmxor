using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Htmxor.Builder;

internal sealed class HtmxorLayoutComponentMetadata([DynamicallyAccessedMembers(LinkerFlags.Component)] Type layoutType)
{
    public Type Type { get; } = layoutType;
}
