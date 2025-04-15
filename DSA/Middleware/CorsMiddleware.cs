using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DSA.API.Middleware
{
    public class CorsMiddleware
    {
        private readonly RequestDelegate _next;

        public CorsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Dodaj nagłówki CORS ręcznie do każdego żądania
            context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:3000");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

            // Obsługa żądania preflight
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                await context.Response.CompleteAsync();
                return;
            }

            await _next(context);
        }
    }
}