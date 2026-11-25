using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.DTOs;
using Teledesk.API.Models;

namespace Teledesk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NumerosWhatsAppController : ControllerBase
    {
        private readonly TeledeskDbContext _context;

        public NumerosWhatsAppController(TeledeskDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var numeros = await _context.NumerosWhatsApp
                .Include(n => n.Setores)
                .OrderBy(n => n.NomeIdentificacao)
                .ToListAsync();

            return Ok(numeros.Select(MapToDto).ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] SalvarNumeroWhatsAppDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeIdentificacao) ||
                string.IsNullOrWhiteSpace(dto.NumeroExibicao) ||
                string.IsNullOrWhiteSpace(dto.PhoneNumberId))
            {
                return BadRequest("Nome de identificação, Número de exibição e PhoneNumberId são obrigatórios.");
            }

            var phoneNumberId = dto.PhoneNumberId.Trim();
            if (await _context.NumerosWhatsApp.AnyAsync(n => n.PhoneNumberId == phoneNumberId))
                return Conflict("Já existe um número cadastrado com este PhoneNumberId.");

            var setores = await _context.Setores
                .Where(s => dto.SetoresIds.Contains(s.Id))
                .ToListAsync();

            var numero = new NumeroWhatsApp
            {
                NomeIdentificacao = dto.NomeIdentificacao.Trim(),
                NumeroExibicao = dto.NumeroExibicao.Trim(),
                PhoneNumberId = phoneNumberId,
                AccessToken = string.IsNullOrWhiteSpace(dto.AccessToken) ? null : dto.AccessToken.Trim(),
                Ativo = dto.Ativo,
                Setores = setores
            };

            _context.NumerosWhatsApp.Add(numero);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Listar), new { id = numero.Id }, MapToDto(numero));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarNumeroWhatsAppDto dto)
        {
            var numero = await _context.NumerosWhatsApp
                .Include(n => n.Setores)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (numero == null)
                return NotFound("Número WhatsApp não encontrado.");

            if (!string.IsNullOrWhiteSpace(dto.NomeIdentificacao))
                numero.NomeIdentificacao = dto.NomeIdentificacao.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NumeroExibicao))
                numero.NumeroExibicao = dto.NumeroExibicao.Trim();

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumberId))
                numero.PhoneNumberId = dto.PhoneNumberId.Trim();

            if (dto.AccessToken != null)
                numero.AccessToken = string.IsNullOrWhiteSpace(dto.AccessToken) ? null : dto.AccessToken.Trim();

            numero.Ativo = dto.Ativo;

            var setores = await _context.Setores
                .Where(s => dto.SetoresIds.Contains(s.Id))
                .ToListAsync();

            numero.Setores.Clear();
            foreach (var setor in setores)
            {
                numero.Setores.Add(setor);
            }

            await _context.SaveChangesAsync();
            return Ok(MapToDto(numero));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var numero = await _context.NumerosWhatsApp.FindAsync(id);
            if (numero == null)
                return NotFound();

            numero.Ativo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static NumeroWhatsAppDto MapToDto(NumeroWhatsApp n) => new()
        {
            Id = n.Id,
            NomeIdentificacao = n.NomeIdentificacao,
            NumeroExibicao = n.NumeroExibicao,
            PhoneNumberId = n.PhoneNumberId,
            PossuiTokenProprio = !string.IsNullOrWhiteSpace(n.AccessToken),
            Ativo = n.Ativo,
            Setores = n.Setores
                .OrderBy(s => s.OrdemUra)
                .Select(s => new SetorResumoDto { Id = s.Id, Nome = s.Nome })
                .ToList()
        };
    }
}
