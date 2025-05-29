public class SecureHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecureHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Zezwól na preflight requests
        if (context.Request.Method == "OPTIONS") // <--- KLUCZOWY WARUNEK
        {
            await _next(context); // Przekaż dalej (do CORS middleware, jeśli jest wcześniej)
            return;               // Zakończ przetwarzanie w tym middleware dla OPTIONS
        }


        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // Mniej restrykcyjne CSP
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "connect-src 'self' https://*.vercel.app; " +
            "img-src 'self' data:; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline';");

        await _next(context);
    }
}
