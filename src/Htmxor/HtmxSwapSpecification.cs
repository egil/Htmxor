using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Htmxor;

/// <summary>
/// Represents an htmx swap specification
/// </summary>
internal sealed record HtmxSwapSpecification
{
	public SwapStyle SwapStyle { get; set; }
	public string? SwapDelay { get; set; }
	public string? SettleDelay { get; set; }
	public string? Show { get; set; }
	public string? Transition { get; set; }
	public string? IgnoreTitle { get; set; }
	public string? Scroll { get; set; }
	public string? FocusScroll { get; set; }
}
