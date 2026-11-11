namespace Api.Middleware;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            bool visualizer = context.Request.Path.StartsWithSegments(Api.Constants.ApiConstants.VisualizerPath);
            bool deckExplorer = context.Request.Path.StartsWithSegments(Api.Constants.ApiConstants.DeckExplorerPath);
            bool hostedClient = visualizer || deckExplorer;
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
            if (hostedClient)
            {
                if (deckExplorer)
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                string frameAncestors = visualizer ? "*" : "'none'";
                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'self'; script-src 'self'; worker-src 'self' blob:; style-src 'self' 'unsafe-inline'; " +
                    "connect-src 'self' ws: wss:; img-src 'self' data: blob:; media-src 'self' blob:; " +
                    $"object-src 'none'; base-uri 'self'; frame-ancestors {frameAncestors}";
            }
            else
            {
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}
