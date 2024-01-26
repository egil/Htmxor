using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using Htmxor.Antiforgery;
using Htmxor.Configuration;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static Htmxor.WebApplicationBuilderExtensions;

namespace Htmxor;

public static class WebApplicationBuilderExtensions
{
    public static HtmxorConfigBuilder AddHtmxor(this IHostApplicationBuilder builder) =>
        new HtmxorConfigBuilder(builder);

}
