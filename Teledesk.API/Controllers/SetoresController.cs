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
    public class SetoresController : ControllerBase
    {
        private readonly TeledeskDbContext _context;

        public SetoresController(TeledeskDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var setores = await _context.Setores
                .Include(s => s.Atendentes)
                .Include(s => s.NumerosWhatsApp)
                .OrderBy(s => s.OrdemUra)
                .ThenBy(s => s.Nome)
                .Select(s => new SetorDto
                {
                    Id = s.Id,
                    Nome = s.Nome,
                    Descricao = s.Descricao,
                    OrdemUra = s.OrdemUra,
                    Ativo = s.Ativo,
                    QuantidadeAtendentes = s.Atendentes.Count,
                    QuantidadeNumeros = s.NumerosWhatsApp.Count
                })
                .ToListAsync();

            return Ok(setores);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] SalvarSetorDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                return BadRequest("O nome do setor é obrigatório.");

            var nome = dto.Nome.Trim();
            if (await _context.Setores.AnyAsync(s => s.Nome.ToLower() == nome.ToLower()))
                return Conflict("Já existe um setor com este nome.");

            var setor = new Setor
            {
                Nome = nome,
                Descricao = dto.Descricao?.Trim(),
                OrdemUra = dto.OrdemUra,
                Ativo = dto.Ativo
            };

            _context.Setores.Add(setor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Listar), new { id = setor.Id }, new SetorDto
            {
                Id = setor.Id,
                Nome = setor.Nome,
                Descricao = setor.Descricao,
                OrdemUra = setor.OrdemUra,
                Ativo = setor.Ativo
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarSetorDto dto)
        {
            var setor = await _context.Setores.FindAsync(id);
            if (setor == null)
                return NotFound("Setor não encontrado.");

            if (!string.IsNullOrWhiteSpace(dto.Nome))
                setor.Nome = dto.Nome.Trim();

            setor.Descricao = dto.Descricao?.Trim();
            setor.OrdemUra = dto.OrdemUra;
            setor.Ativo = dto.Ativo;

            await _context.SaveChangesAsync();
            return Ok(new SetorDto
            {
                Id = setor.Id,
                Nome = setor.Nome,
                Descricao = setor.Descricao,
                OrdemUra = setor.OrdemUra,
                Ativo = setor.Ativo
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            var setor = await _context.Setores.FindAsync(id);
            if (setor == null)
                return NotFound();

            setor.Ativo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
