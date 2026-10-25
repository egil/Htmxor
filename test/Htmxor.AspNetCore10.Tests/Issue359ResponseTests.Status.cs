using System.Net;
using Microsoft.AspNetCore.Http;

namespace Htmxor.AspNetCore10;

public sealed partial class Issue359ResponseTests
{
	[Fact]
	public void General_status_accepts_286_and_returns_the_active_response()
	{
		var context = CreateNavigationContext(validMarker: true);
		var response = context.GetHtmxContext().Response;

		Assert.Multiple(
			() => Assert.Same(response, response.StatusCode((HttpStatusCode)286)),
			() => Assert.Equal(286, context.Response.StatusCode)
		);
	}

}
