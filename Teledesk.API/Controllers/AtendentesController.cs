using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.DTOs;
using Teledesk.API.Models;
using Teledesk.API.Models.Enums;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtendentesController : ControllerBase
    {
        private readonly TeledeskDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly PasswordHasher<Atendente> _passwordHasher = new();

        public AtendentesController(TeledeskDbContext context, IJwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest("E-mail e senha são obrigatórios.");

            var emailNormalizado = dto.Email.Trim().ToLower();
            var atendente = await _context.Atendentes
                .Include(a => a.Setores)
                .FirstOrDefaultAsync(a => a.Email.ToLower() == emailNormalizado);

            if (atendente == null || !atendente.Ativo)
                return Unauthorized("Credenciais inválidas ou usuário inativo.");

            bool senhaValida = false;
            try
            {
                var resultado = _passwordHasher.VerifyHashedPassword(atendente, atendente.SenhaHash, dto.Senha);
                if (resultado == PasswordVerificationResult.Success ||
                    resultado == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    senhaValida = true;
                }
            }
            catch (FormatException)
            {
                // Compatibilidade com senhas antigas em texto puro no banco local: converte para hash
                if (atendente.SenhaHash == dto.Senha)
                {
                    atendente.SenhaHash = _passwordHasher.HashPassword(atendente, dto.Senha);
                    await _context.SaveChangesAsync();
                    senhaValida = true;
                }
            }

            if (!senhaValida)
                return Unauthorized("Credenciais inválidas.");

            atendente.Status = StatusAtendente.Disponivel;
            await _context.SaveChangesAsync();

            var token = _jwtTokenService.GerarToken(atendente);

            var response = new LoginResponseDto
            {
                Id = atendente.Id,
                Nome = atendente.Nome,
                Email = atendente.Email,
                Perfil = atendente.Perfil,
                Status = atendente.Status,
                Token = token,
                Setores = atendente.Setores
                    .Where(s => s.Ativo)
                    .OrderBy(s => s.OrdemUra)
                    .Select(s => new SetorResumoDto { Id = s.Id, Nome = s.Nome })
                    .ToList()
            };

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> ObterPerfilAtual()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int atendenteId))
                return Unauthorized();

            var atendente = await _context.Atendentes
                .Include(a => a.Setores)
                .FirstOrDefaultAsync(a => a.Id == atendenteId);

            if (atendente == null)
                return NotFound();

            return Ok(MapToDto(atendente));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var atendentes = await _context.Atendentes
                .Include(a => a.Setores)
                .OrderBy(a => a.Nome)
                .ToListAsync();

            return Ok(atendentes.Select(MapToDto).ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] SalvarAtendenteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Senha))
                return BadRequest("Nome, e-mail e senha são obrigatórios.");

            var emailNormalizado = dto.Email.Trim().ToLower();
            if (await _context.Atendentes.AnyAsync(a => a.Email.ToLower() == emailNormalizado))
                return Conflict("Já existe um atendente com este e-mail.");

            var setores = await _context.Setores
                .Where(s => dto.SetoresIds.Contains(s.Id))
                .ToListAsync();

            var atendente = new Atendente
            {
                Nome = dto.Nome.Trim(),
                Email = emailNormalizado,
                Perfil = dto.Perfil,
                Ativo = dto.Ativo,
                Setores = setores
            };
            atendente.SenhaHash = _passwordHasher.HashPassword(atendente, dto.Senha);

            _context.Atendentes.Add(atendente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Listar), new { id = atendente.Id }, MapToDto(atendente));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarAtendenteDto dto)
        {
            var atendente = await _context.Atendentes
                .Include(a => a.Setores)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (atendente == null)
                return NotFound("Atendente não encontrado.");

            if (!string.IsNullOrWhiteSpace(dto.Nome))
                atendente.Nome = dto.Nome.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Email))
                atendente.Email = dto.Email.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(dto.Senha))
                atendente.SenhaHash = _passwordHasher.HashPassword(atendente, dto.Senha);

            atendente.Perfil = dto.Perfil;
            atendente.Ativo = dto.Ativo;

            var setores = await _context.Setores
                .Where(s => dto.SetoresIds.Contains(s.Id))
                .ToListAsync();

            atendente.Setores.Clear();
            foreach (var setor in setores)
            {
                atendente.Setores.Add(setor);
            }

            await _context.SaveChangesAsync();
            return Ok(MapToDto(atendente));
        }

        private static AtendenteDto MapToDto(Atendente a) => new()
        {
            Id = a.Id,
            Nome = a.Nome,
            Email = a.Email,
            Perfil = a.Perfil,
            Status = a.Status,
            Ativo = a.Ativo,
            DataCriacao = a.DataCriacao,
            Setores = a.Setores
                .OrderBy(s => s.OrdemUra)
                .Select(s => new SetorResumoDto { Id = s.Id, Nome = s.Nome })
                .ToList()
        };
    }
}
