using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.DTOs;
using Teledesk.API.Models;
using Teledesk.API.Models.Enums;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Hubs
{
    [Authorize]
    public class AtendimentoHub : Hub
    {
        private readonly TeledeskDbContext _context;
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<AtendimentoHub> _logger;

        public AtendimentoHub(
            TeledeskDbContext context,
            IWhatsAppService whatsAppService,
            ILogger<AtendimentoHub> logger)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var atendenteId = ObterAtendenteIdContexto();
            if (atendenteId.HasValue)
            {
                var atendente = await _context.Atendentes
                    .Include(a => a.Setores)
                    .FirstOrDefaultAsync(a => a.Id == atendenteId.Value);

                if (atendente != null)
                {
                    List<int> setoresIds = atendente.Perfil == PerfilAtendente.Admin
                        ? await _context.Setores.Where(s => s.Ativo).Select(s => s.Id).ToListAsync()
                        : atendente.Setores.Where(s => s.Ativo).Select(s => s.Id).ToList();

                    foreach (var setorId in setoresIds)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"setor-{setorId}");
                    }

                    _logger.LogInformation(
                        "Atendente #{Id} ({Nome}) conectado ao SignalR nos setores: [{Setores}]",
                        atendente.Id, atendente.Nome, string.Join(", ", setoresIds));
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// O atendente assume um atendimento que está na fila de espera (com trava contra concorrência).
        /// </summary>
        public async Task<bool> AssumirAtendimento(int atendimentoId)
        {
            var idAutenticado = ObterAtendenteIdContexto();
            if (!idAutenticado.HasValue)
                return false;

            var atendimento = await _context.Atendimentos
                .Include(a => a.Contato)
                .Include(a => a.Setor)
                .Include(a => a.NumeroWhatsApp)
                .FirstOrDefaultAsync(a => a.Id == atendimentoId);

            // Trava de concorrência: permite assumir se estiver AguardandoAtendente ou ainda na URA
            if (atendimento == null ||
                (atendimento.Status != StatusAtendimento.AguardandoAtendente && atendimento.Status != StatusAtendimento.EmUra))
            {
                return false;
            }

            var atendente = await _context.Atendentes
                .Include(a => a.Setores)
                .FirstOrDefaultAsync(a => a.Id == idAutenticado.Value);

            if (atendente == null)
                return false;

            // Garante que atendente comum só assuma atendimento do seu setor
            if (atendente.Perfil != PerfilAtendente.Admin &&
                atendimento.SetorId.HasValue &&
                !atendente.Setores.Any(s => s.Id == atendimento.SetorId.Value))
            {
                return false;
            }

            // Caso o operador assuma diretamente um chamado que estava na URA sem setor definido
            if (!atendimento.SetorId.HasValue)
            {
                var primeiroSetor = atendente.Setores.FirstOrDefault(s => s.Ativo);
                if (primeiroSetor != null)
                {
                    atendimento.SetorId = primeiroSetor.Id;
                    atendimento.Setor = primeiroSetor;
                }
            }

            atendimento.AtendenteId = idAutenticado.Value;
            atendimento.Status = StatusAtendimento.EmAtendimento;
            await _context.SaveChangesAsync();

            var dto = new AtendimentoDto
            {
                Id = atendimento.Id,
                ContatoId = atendimento.ContatoId,
                NomeCliente = atendimento.Contato.Nome,
                UsernameCliente = atendimento.Contato.Username,
                WhatsAppId = atendimento.Contato.WhatsAppId,
                SetorId = atendimento.SetorId,
                SetorNome = atendimento.Setor?.Nome,
                NumeroWhatsAppId = atendimento.NumeroWhatsAppId,
                NumeroExibicao = atendimento.NumeroWhatsApp?.NumeroExibicao,
                Status = atendimento.Status,
                AtendenteId = idAutenticado.Value,
                AtendenteNome = atendente.Nome,
                DataInicio = atendimento.DataInicio
            };

            await EnviarParaSetorAsync(atendimento.SetorId, "AtendimentoAssumido", dto);

            var phoneNumberId = atendimento.NumeroWhatsApp?.PhoneNumberId ?? "100000000000000";
            var setorTexto = atendimento.Setor != null ? $" do setor *{atendimento.Setor.Nome}*" : "";
            await _whatsAppService.EnviarMensagemTextoAsync(
                phoneNumberId,
                atendimento.Contato.WhatsAppId,
                $"Olá! Sou *{atendente.Nome}*{setorTexto} e darei continuidade ao seu atendimento. Como posso te ajudar?",
                atendimento.NumeroWhatsApp?.AccessToken);

            _logger.LogInformation("Atendimento #{Id} assumido pelo Atendente #{AtendenteId}", atendimentoId, idAutenticado.Value);
            return true;
        }

        /// <summary>
        /// O atendente envia uma mensagem de texto para o WhatsApp do cliente pelo número que originou o chamado.
        /// </summary>
        public async Task<bool> EnviarMensagemParaCliente(int atendimentoId, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            var atendimento = await _context.Atendimentos
                .Include(a => a.Contato)
                .Include(a => a.NumeroWhatsApp)
                .FirstOrDefaultAsync(a => a.Id == atendimentoId);

            if (atendimento == null || atendimento.Status != StatusAtendimento.EmAtendimento)
                return false;

            var phoneNumberId = atendimento.NumeroWhatsApp?.PhoneNumberId ?? "100000000000000";
            await _whatsAppService.EnviarMensagemTextoAsync(
                phoneNumberId,
                atendimento.Contato.WhatsAppId,
                texto,
                atendimento.NumeroWhatsApp?.AccessToken);

            var mensagem = new Mensagem
            {
                AtendimentoId = atendimentoId,
                Texto = texto,
                Origem = OrigemMensagem.Atendente,
                DataEnvio = DateTime.UtcNow
            };

            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            var mensagemDto = new MensagemDto
            {
                Id = mensagem.Id,
                AtendimentoId = mensagem.AtendimentoId,
                Texto = mensagem.Texto,
                Origem = mensagem.Origem,
                DataEnvio = mensagem.DataEnvio
            };

            await EnviarParaSetorAsync(atendimento.SetorId, "NovaMensagemRecebida", mensagemDto);
            return true;
        }

        /// <summary>
        /// O atendente encerra o atendimento ativo.
        /// </summary>
        public async Task<bool> FinalizarAtendimento(int atendimentoId)
        {
            var atendimento = await _context.Atendimentos
                .Include(a => a.Contato)
                .Include(a => a.NumeroWhatsApp)
                .FirstOrDefaultAsync(a => a.Id == atendimentoId);

            if (atendimento == null)
                return false;

            atendimento.Status = StatusAtendimento.Finalizado;
            atendimento.DataFim = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await EnviarParaSetorAsync(atendimento.SetorId, "AtendimentoFinalizado", atendimentoId);

            var phoneNumberId = atendimento.NumeroWhatsApp?.PhoneNumberId ?? "100000000000000";
            await _whatsAppService.EnviarMensagemTextoAsync(
                phoneNumberId,
                atendimento.Contato.WhatsAppId,
                "Seu atendimento foi finalizado. Agradecemos pelo contato! Se precisar de algo mais, basta enviar uma nova mensagem.",
                atendimento.NumeroWhatsApp?.AccessToken);

            _logger.LogInformation("Atendimento #{Id} finalizado com sucesso.", atendimentoId);
            return true;
        }

        /// <summary>
        /// Retorna a lista de atendimentos na fila e em atendimento filtrada pelos setores do atendente logado.
        /// </summary>
        public async Task<List<AtendimentoDto>> ObterFilaAtendimento(int? filtroSetorId = null)
        {
            var query = _context.Atendimentos
                .Include(a => a.Contato)
                .Include(a => a.Atendente)
                .Include(a => a.Setor)
                .Include(a => a.NumeroWhatsApp)
                .Where(a => a.Status != StatusAtendimento.Finalizado);

            var atendenteId = ObterAtendenteIdContexto();
            if (atendenteId.HasValue)
            {
                var atendente = await _context.Atendentes
                    .Include(a => a.Setores)
                    .FirstOrDefaultAsync(a => a.Id == atendenteId.Value);

                if (atendente != null && atendente.Perfil != PerfilAtendente.Admin)
                {
                    var meusSetoresIds = atendente.Setores.Select(s => s.Id).ToList();
                    query = query.Where(a => !a.SetorId.HasValue || meusSetoresIds.Contains(a.SetorId.Value));
                }
            }

            if (filtroSetorId.HasValue && filtroSetorId.Value > 0)
            {
                query = query.Where(a => a.SetorId == filtroSetorId.Value);
            }

            return await query
                .OrderBy(a => a.DataInicio)
                .Select(a => new AtendimentoDto
                {
                    Id = a.Id,
                    ContatoId = a.ContatoId,
                    NomeCliente = a.Contato.Nome,
                    UsernameCliente = a.Contato.Username,
                    WhatsAppId = a.Contato.WhatsAppId,
                    SetorId = a.SetorId,
                    SetorNome = a.Setor != null ? a.Setor.Nome : (a.Status == StatusAtendimento.EmUra ? "Na URA" : "Sem Setor"),
                    NumeroWhatsAppId = a.NumeroWhatsAppId,
                    NumeroExibicao = a.NumeroWhatsApp != null ? a.NumeroWhatsApp.NumeroExibicao : null,
                    Status = a.Status,
                    AtendenteId = a.AtendenteId,
                    AtendenteNome = a.Atendente != null ? a.Atendente.Nome : null,
                    DataInicio = a.DataInicio,
                    UltimaMensagem = a.Mensagens.OrderByDescending(m => m.DataEnvio).Select(m => m.Texto).FirstOrDefault()
                })
                .ToListAsync();
        }

        /// <summary>
        /// Retorna o histórico de mensagens de um atendimento específico.
        /// </summary>
        public async Task<List<MensagemDto>> ObterHistoricoMensagens(int atendimentoId)
        {
            return await _context.Mensagens
                .Where(m => m.AtendimentoId == atendimentoId)
                .OrderBy(m => m.DataEnvio)
                .Select(m => new MensagemDto
                {
                    Id = m.Id,
                    AtendimentoId = m.AtendimentoId,
                    Texto = m.Texto,
                    Origem = m.Origem,
                    DataEnvio = m.DataEnvio
                })
                .ToListAsync();
        }

        private int? ObterAtendenteIdContexto()
        {
            var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(sub, out int id) ? id : null;
        }

        private async Task EnviarParaSetorAsync(int? setorId, string evento, object payload)
        {
            if (setorId.HasValue)
            {
                await Clients.Group($"setor-{setorId.Value}").SendAsync(evento, payload);
            }
            else
            {
                await Clients.All.SendAsync(evento, payload);
            }
        }
    }
}
