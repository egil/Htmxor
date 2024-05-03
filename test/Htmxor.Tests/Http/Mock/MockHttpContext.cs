using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Htmxor.Http.Mock;

public class MockHttpContext : HttpContext
{
	public override HttpRequest Request { get; } = new MockHttpRequest();
	public override HttpResponse Response { get; } = new MockHttpResponse();

	public override IFeatureCollection Features { get; } = default!;
	public override ConnectionInfo Connection { get; } = default!;
	public override WebSocketManager WebSockets { get; } = default!;
	public override ClaimsPrincipal User { get; set; } = default!;
	public override IDictionary<object, object?> Items { get; set; } = default!;
	public override IServiceProvider RequestServices { get; set; } = default!;
	public override CancellationToken RequestAborted { get; set; } = default!;
	public override string TraceIdentifier { get; set; } = default!;
	public override ISession Session { get; set; } = default!;

	public override void Abort()
	{
		throw new NotImplementedException();
	}
}