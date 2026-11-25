using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Teledesk.API.Models;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationHours;

        public JwtTokenService(IConfiguration configuration)
        {
            _secretKey = configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurada.");
            _issuer = configuration["JwtSettings:Issuer"] ?? "Teledesk.API";
            _audience = configuration["JwtSettings:Audience"] ?? "Teledesk.Desktop";
            _expirationHours = int.TryParse(configuration["JwtSettings:ExpirationHours"], out var h) ? h : 12;
        }

        public string GerarToken(Atendente atendente)
        {
            var headerJson = JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" });
            var exp = DateTimeOffset.UtcNow.AddHours(_expirationHours).ToUnixTimeSeconds();
            var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var setoresIds = string.Join(",", atendente.Setores.Select(s => s.Id));

            var payload = new Dictionary<string, object>
            {
                ["sub"] = atendente.Id.ToString(),
                ["name"] = atendente.Nome,
                ["email"] = atendente.Email,
                ["role"] = atendente.Perfil.ToString(),
                ["setores"] = setoresIds,
                ["iss"] = _issuer,
                ["aud"] = _audience,
                ["iat"] = iat,
                ["exp"] = exp
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var headerEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var signingInput = $"{headerEncoded}.{payloadEncoded}";

            var signature = ComputeHmacSha256(signingInput);
            return $"{signingInput}.{signature}";
        }

        public ClaimsPrincipal? ValidarToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            var signingInput = $"{parts[0]}.{parts[1]}";
            var expectedSignatureBytes = ComputeHmacSha256Bytes(signingInput);
            byte[] actualSignatureBytes;

            try
            {
                actualSignatureBytes = Base64UrlDecode(parts[2]);
            }
            catch
            {
                return null;
            }

            if (!CryptographicOperations.FixedTimeEquals(expectedSignatureBytes, actualSignatureBytes))
                return null;

            try
            {
                var payloadBytes = Base64UrlDecode(parts[1]);
                using var doc = JsonDocument.Parse(payloadBytes);
                var root = doc.RootElement;

                if (root.TryGetProperty("iss", out var iss) && iss.GetString() != _issuer)
                    return null;

                if (root.TryGetProperty("aud", out var aud) && aud.GetString() != _audience)
                    return null;

                if (!root.TryGetProperty("exp", out var expProp) ||
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expProp.GetInt64())
                {
                    return null;
                }

                var claims = new List<Claim>();
                if (root.TryGetProperty("sub", out var sub))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? string.Empty));
                if (root.TryGetProperty("name", out var name))
                    claims.Add(new Claim(ClaimTypes.Name, name.GetString() ?? string.Empty));
                if (root.TryGetProperty("email", out var email))
                    claims.Add(new Claim(ClaimTypes.Email, email.GetString() ?? string.Empty));
                if (root.TryGetProperty("role", out var role))
                    claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                if (root.TryGetProperty("setores", out var setores))
                    claims.Add(new Claim("setores", setores.GetString() ?? string.Empty));

                var identity = new ClaimsIdentity(claims, "Bearer");
                return new ClaimsPrincipal(identity);
            }
            catch
            {
                return null;
            }
        }

        private string ComputeHmacSha256(string input)
        {
            return Base64UrlEncode(ComputeHmacSha256Bytes(input));
        }

        private byte[] ComputeHmacSha256Bytes(string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            using var hmac = new HMACSHA256(keyBytes);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}

