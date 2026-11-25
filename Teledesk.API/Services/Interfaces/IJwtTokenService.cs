using System.Security.Claims;
using Teledesk.API.Models;

namespace Teledesk.API.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GerarToken(Atendente atendente);
        ClaimsPrincipal? ValidarToken(string token);
    }
}

