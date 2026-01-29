using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
namespace HMACServerApp.Models
{
    public class HMACAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memorycache;
        private readonly IConfiguration _configuration;

        private static readonly TimeSpan NonceExpriry = TimeSpan.FromMinutes(5);

        public HMACAuthenticationMiddleware(RequestDelegate next, IMemoryCache memoryCache, IConfiguration configuration)
        {
            _next = next;
            _memorycache = memoryCache;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var isHMACEnabled = _configuration.GetValue<bool>("HMACSetting:EnableHMAC");
            if (!isHMACEnabled)
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Authorization header missing");
            }

            if (!authHeader.ToString().StartsWith("HMAC ", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid Authorization header");
            }

            var tokenParts = authHeader.ToString().Substring("HMAC ".Length).Trim().Split('|');
            if (tokenParts.Length != 4)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid HMAC token format");
                return;
            }

            var clientId = tokenParts[0];
            var token = tokenParts[1];
            var nonce = tokenParts[2];
            var timestamp = tokenParts[3];

            var clientSecretService = context.RequestServices.GetRequiredService<ClientSecretService>();

            var secretKey = await clientSecretService.GetSecretKeyAsync(clientId);

            if (string.IsNullOrEmpty(secretKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid Client ID");
                return;
            }

            if (!long.TryParse(timestamp, out var timestampSeconds))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid Timestamp");
            }

            var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds).UtcDateTime;
            var currentTime = DateTime.UtcNow;

            //Check timestamp validity (within 5 minutes)
            //This is to avoid Reply Attack
            if (Math.Abs((currentTime - requestTime).TotalMinutes) > 5)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Request timestamp is outside the allowed time window");
                return;
            }

            var nonceKey = $"{clientId}:{nonce}";
            if (_memorycache.TryGetValue(nonceKey, out _))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Nonce already used");
                return;
            }

            _memorycache.Set(nonceKey, true, NonceExpriry);

            var requestBody = string.Empty;
            if (context.Request.Method == HttpMethod.Post.Method || context.Request.Method == HttpMethod.Put.Method)
            {
                context.Request.EnableBuffering();
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }
            }

            var isValid = ValidateToken(token, nonce, timestamp, context.Request, requestBody, secretKey);

            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid HMAC token");
                return;
            }

            await _next(context);
        }

        private bool ValidateToken(string token, string nonce, string timestamp, HttpRequest request, string requestBody, string secretKey)
        {
            var path = Convert.ToString(request.Path);

            var requestContent = new StringBuilder()
                .Append(request.Method.ToUpper())
                .Append(path.ToUpper())
                .Append(nonce)
                .Append(timestamp);

            if(request.Method == HttpMethod.Post.Method || request.Method == HttpMethod.Put.Method)
            {
                requestContent.Append(requestBody);
            }

            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            var requestContentBytes = Encoding.UTF8.GetBytes(requestContent.ToString());

            using (var hmac = new HMACSHA256(secretKeyBytes))
            {
                var hashBytes = hmac.ComputeHash(requestContentBytes);
                var computedToken = Convert.ToBase64String(hashBytes);
                if (!computedToken.Equals(token))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
