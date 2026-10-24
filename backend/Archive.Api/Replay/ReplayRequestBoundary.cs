using Archive.Api.Options;

namespace Archive.Api.Replay;

/// <summary>Separates the credential-free replay host from authenticated API routes.</summary>
internal static class ReplayRequestBoundary
{
    public static bool IsReplayHost(HttpRequest request, ReplayOptions options)
    {
        var replay = new Uri(options.PublicOrigin, UriKind.Absolute);
        int? expectedPort = replay.IsDefaultPort ? null : replay.Port;
        return string.Equals(request.Host.Host, replay.Host, StringComparison.OrdinalIgnoreCase) &&
            request.Host.Port == expectedPort;
    }

    public static async Task ApplyAsync(HttpContext context, ReplayOptions options, RequestDelegate next)
    {
        var replayHost = IsReplayHost(context.Request, options);
        var replayPath = context.Request.Path.StartsWithSegments("/replay/snapshots");
        if (replayHost)
        {
            // A reverse proxy should already strip credentials; this keeps the application boundary fail-closed.
            context.Request.Headers.Remove("Cookie");
            context.Request.Headers.Remove("Authorization");
            if (!replayPath)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) &&
                !HttpMethods.IsOptions(context.Request.Method))
            {
                context.Response.Headers.Allow = "GET, HEAD, OPTIONS";
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            await next(context);
            return;
        }

        if (replayPath)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await next(context);
    }
}
