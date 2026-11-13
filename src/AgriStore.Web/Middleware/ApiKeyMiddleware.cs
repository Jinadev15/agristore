namespace AgriStore.Web.Middleware;

/// <summary>
/// Protects API writes. Any POST/PUT/DELETE under /api must send the header "X-Api-Key".
/// GET requests stay open so dashboards and tools can read data.
/// </summary>
public class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    public const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var isApiWrite = context.Request.Path.StartsWithSegments("/api")
                         && !HttpMethods.IsGet(context.Request.Method);

        if (isApiWrite)
        {
            var expected = config["Api:Key"];
            var provided = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrEmpty(expected) || provided != expected)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = $"Missing or invalid {HeaderName} header." });
                return;
            }
        }

        await next(context);
    }
}
