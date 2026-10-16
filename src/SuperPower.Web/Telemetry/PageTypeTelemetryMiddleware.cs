using Microsoft.AspNetCore.Http.Features;
using Umbraco.Cms.Web.Common.Routing;

namespace SuperPower.Web.Telemetry;

public class PageTypeTelemetryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            string alias = PageTypeTag.From(
                context.Features.Get<UmbracoRouteValues>()?.PublishedRequest.PublishedContent?.ContentType.Alias);
            context.Features.Get<IHttpMetricsTagsFeature>()?.Tags.Add(new(PageTypeTag.Name, alias));
            context.Features.Get<IHttpActivityFeature>()?.Activity.SetTag(PageTypeTag.Name, alias);
        }
    }
}
