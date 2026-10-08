namespace Firmeza.Api.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Previene MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Previene Clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Protección contra XSS reflejado
        headers["X-XSS-Protection"] = "1; mode=block";

        // Política de Referrer para no filtrar URLs sensibles
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy restrictiva para API JSON pura (exceptuando endpoints de Scalar UI en dev)
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (!path.StartsWith("/scalar") && !path.StartsWith("/openapi"))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none';";
        }

        // Permissions Policy para deshabilitar acceso a hardware del cliente
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
