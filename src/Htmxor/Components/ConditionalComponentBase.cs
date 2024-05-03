using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public abstract class ConditionalComponentBase : ComponentBase, IConditionalOutputComponent
{
	/// <inheritdoc/>
	/// <remarks>The <see cref="ConditionalComponentBase"/> defaults to returning <see langword="true"/>
	/// when the request is a full page request or if there are no direct conditional children.</remarks>
	public virtual bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> context.Request.IsFullPageRequest || directConditionalChildren == 0;
}
