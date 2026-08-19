using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.Models;

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
        [HttpGet("login")]
        public async Task<IActionResult> Login([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("O e-mail é obrigatório.");

            var atendente = await _context.Atendentes
                .FirstOrDefaultAsync(a => a.Email.ToLower() == email.Trim().ToLower());

            if (atendente == null)
            {
                // Se a tabela estiver vazia, cria um atendente de teste automaticamente
                if (!await _context.Atendentes.AnyAsync())
                {
                    atendente = new Atendente
                    {
                        Nome = "Lucas Lima (Atendente)",
                        Email = email.Trim().ToLower()
                    };
                    _context.Atendentes.Add(atendente);
                    await _context.SaveChangesAsync();
                    return Ok(atendente);
                }

                return NotFound("Atendente não encontrado.");
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
