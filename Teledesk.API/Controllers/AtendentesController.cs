using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.Models;
using Teledesk.API.DTOs;

namespace Teledesk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtendentesController : ControllerBase
    {
        private readonly TeledeskDbContext _context;

        public AtendentesController(TeledeskDbContext context)
        {
            _context = context;
        }

        // GET: api/atendentes/login?email=lucas@teledesk.com
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("O e-mail é obrigatório.");

            var atendente = await _context.Atendentes
                .FirstOrDefaultAsync(a => a.Email.ToLower() == dto.Email.Trim().ToLower());

            if (atendente == null)
            {
                // Se o atendente não existir, cadastra automaticamente para facilitar o desenvolvimento
                var nomeExibicao = dto.Email.Contains('@') ? dto.Email.Split('@')[0] : "Atendente";
                atendente = new Atendente
                {
                    Nome = char.ToUpper(nomeExibicao[0]) + nomeExibicao.Substring(1),
                    Email = dto.Email.Trim().ToLower(),
                    Senha = dto.Senha
                };
                _context.Atendentes.Add(atendente);
                await _context.SaveChangesAsync();
                return Ok(atendente);
            }

            if (atendente.Senha != dto.Senha)
            {
                return Unauthorized("Senha Incorreta");
            }

            return Ok(atendente);
        }

        // GET: api/atendentes
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var atendentes = await _context.Atendentes.ToListAsync();
            return Ok(atendentes);
        }
    }
}
