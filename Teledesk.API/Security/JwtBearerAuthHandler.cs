using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Security
{
    public class JwtBearerAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IJwtTokenService _jwtTokenService;

        public JwtBearerAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IJwtTokenService jwtTokenService)
            : base(options, logger, encoder)
        {
            _jwtTokenService = jwtTokenService;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? token = null;

            var authHeader = Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader["Bearer ".Length..].Trim();
            }
            else if (Request.Query.TryGetValue("access_token", out var accessToken))
            {
                // SignalR envia o token na query string durante a negociação WebSocket
                token = accessToken.ToString();
            }

            if (string.IsNullOrWhiteSpace(token))
                return Task.FromResult(AuthenticateResult.NoResult());

            var principal = _jwtTokenService.ValidarToken(token);
            if (principal == null)
                return Task.FromResult(AuthenticateResult.Fail("Token JWT inválido ou expirado."));

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

