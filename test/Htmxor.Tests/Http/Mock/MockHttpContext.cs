using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Htmxor.Http.Mock;

internal class MockHttpContext : HttpContext
{
	public override HttpRequest Request { get; } = new MockHttpRequest();
	public override HttpResponse Response { get; } = new MockHttpResponse();

    public override void Abort()
	{
		throw new NotImplementedException();
	}

	public override IFeatureCollection Features { get; } = default!;
	public override ConnectionInfo Connection { get; } = default!;
    public override WebSocketManager WebSockets { get; } = default!;
    public override ClaimsPrincipal User { get; set; } = default!;
    public override IDictionary<object, object?> Items { get; set; } = default!;
    public override IServiceProvider RequestServices { get; set; } = default!;
    public override CancellationToken RequestAborted { get; set; } = default!;
    public override string TraceIdentifier { get; set; } = default!;
    public override ISession Session { get; set; } = default!;
}
