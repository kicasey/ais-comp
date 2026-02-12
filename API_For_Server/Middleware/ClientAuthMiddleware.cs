using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace API_For_Server.Middleware;

/// <summary>
/// Validates Client ID and Client Secret from headers.
/// Accepts: X-Client-Id + X-Client-Secret, or Authorization: Basic base64(clientId:clientSecret).
/// </summary>
public class ClientAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<ClientAuthMiddleware> _logger;

    public ClientAuthMiddleware(RequestDelegate next, IConfiguration config, ILogger<ClientAuthMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var expectedId = _config["ClientAuth:ClientId"];
        var expectedSecret = _config["ClientAuth:ClientSecret"];

        if (string.IsNullOrEmpty(expectedId) || string.IsNullOrEmpty(expectedSecret))
        {
            _logger.LogWarning("ClientAuth not configured (ClientId/ClientSecret missing). Rejecting requests.");
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new { message = "API authentication not configured." });
            return;
        }

        string? clientId = null;
        string? clientSecret = null;

        if (context.Request.Headers.TryGetValue("X-Client-Id", out var idHeader) &&
            context.Request.Headers.TryGetValue("X-Client-Secret", out var secretHeader))
        {
            clientId = idHeader.ToString().Trim();
            clientSecret = secretHeader.ToString().Trim();
        }
        else if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var auth) &&
                 auth.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) &&
                 !string.IsNullOrEmpty(auth.Parameter))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter));
                var parts = decoded.Split(':', 2);
                if (parts.Length == 2)
                {
                    clientId = parts[0];
                    clientSecret = parts[1];
                }
            }
            catch { /* ignore */ }
        }

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid Client ID / Client Secret." });
            return;
        }

        if (!ConstantTimeEquals(clientId, expectedId) || !ConstantTimeEquals(clientSecret, expectedSecret))
        {
            _logger.LogWarning("Invalid client credentials attempt from {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid Client ID or Client Secret." });
            return;
        }

        await _next(context);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }
}
