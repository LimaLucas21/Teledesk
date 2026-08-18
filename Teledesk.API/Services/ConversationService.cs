using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Teledesk.API.Data;
using Teledesk.API.DTOs;
using Teledesk.API.Hubs;
using Teledesk.API.Models;
using Teledesk.API.Models.Enums;
using Teledesk.API.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Teledesk.API.Services
{
    public class ConversationService : IConversationService
    {
        private readonly TeledeskDbContext _context;
        private readonly ITelegramBotClient _botClient;
        private readonly IHubContext<AtendimentoHub> _hubContext;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            TeledeskDbContext context,
            ITelegramBotClient botClient,
            IHubContext<AtendimentoHub> hubContext,
            ILogger<ConversationService> logger)
        {
            _context = context;
            _botClient = botClient;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task ProcessUpdateAsync(Update update)
        {
            // 1. Validação de guarda: Apenas processar se for mensagem de texto
            if (update.Message == null || string.IsNullOrWhiteSpace(update.Message.Text))
                return;

            var chatId = update.Message.Chat.Id;
            var texto = update.Message.Text.Trim();
            var nome = update.Message.From?.FirstName ?? "Cliente";
            var username = update.Message.From?.Username;

            _logger.LogInformation("Processando mensagem de {Nome} ({ChatId}): {Texto}", nome, chatId, texto);

            // 2. Identificar ou cadastrar o Contato
            var contato = await _context.Contatos
                .FirstOrDefaultAsync(c => c.TelegramChatId == chatId);

            if (contato == null)
            {
                contato = new Contato
                {
                    TelegramChatId = chatId,
                    Nome = nome,
                    Username = username,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Contatos.Add(contato);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Novo contato cadastrado: {Nome} (ID: {Id})", contato.Nome, contato.Id);
            }

            // 3. Localizar ou abrir um Atendimento ativo (diferente de Finalizado)
            var atendimento = await _context.Atendimentos
                .FirstOrDefaultAsync(a => a.ContatoId == contato.Id && a.Status != StatusAtendimento.Finalizado);

            if (atendimento == null)
            {
                atendimento = new Atendimento
                {
                    ContatoId = contato.Id,
                    Status = StatusAtendimento.EmUra,
                    DataInicio = DateTime.UtcNow
                };

                _context.Atendimentos.Add(atendimento);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Novo atendimento iniciado para o contato {Nome} (Atendimento ID: {Id})", contato.Nome, atendimento.Id);
            }

            // 4. Gravar a mensagem do cliente no histórico
            var mensagem = new Mensagem
            {
                AtendimentoId = atendimento.Id,
                Texto = texto,
                Origem = OrigemMensagem.Cliente,
                DataEnvio = DateTime.UtcNow
            };

            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            // 5. Máquina de Estados da Conversa
            if (atendimento.Status == StatusAtendimento.EmUra)
            {
                await ProcessarUraAsync(atendimento, chatId, texto, nome, username);
            }
            else
            {
                // Se já estiver na fila ou em atendimento humano, notifica o SignalR em tempo real
                var mensagemDto = new MensagemDto
                {
                    Id = mensagem.Id,
                    AtendimentoId = mensagem.AtendimentoId,
                    Texto = mensagem.Texto,
                    Origem = mensagem.Origem,
                    DataEnvio = mensagem.DataEnvio
                };

                await _hubContext.Clients.All.SendAsync("NovaMensagemRecebida", mensagemDto);
                _logger.LogInformation("Mensagem transmitida via SignalR para o atendimento #{AtendimentoId}", atendimento.Id);
            }
        }

        private async Task EnviarMensagemAsync(int atendimentoId, long chatId, string texto)
        {
            await _botClient.SendMessage(chatId, texto);

            var mensagemSistema = new Mensagem
            {
                AtendimentoId = atendimentoId,
                Texto = texto,
                Origem = OrigemMensagem.Sistema,
                DataEnvio = DateTime.UtcNow
            };

            _context.Mensagens.Add(mensagemSistema);
            await _context.SaveChangesAsync();
        }

        private async Task ProcessarUraAsync(Atendimento atendimento, long chatId, string texto, string nome, string? username)
        {
            if (texto == "1")
            {
                await EnviarMensagemAsync(atendimento.Id, chatId, "🕒 Nosso horário de atendimento é de segunda a sexta-feira, das 08h às 18h.");
            }
            else if (texto == "2")
            {
                // Transbordo para fila humana
                atendimento.Status = StatusAtendimento.AguardandoAtendente;
                await _context.SaveChangesAsync();

                await EnviarMensagemAsync(atendimento.Id, chatId, "👨‍💻 Você foi transferido para a nossa fila de atendimento! Por favor, aguarde um instante que um de nossos operadores irá te atender.");

                // Notifica em tempo real todos os atendentes conectados ao SignalR Hub
                var atendimentoDto = new AtendimentoDto
                {
                    Id = atendimento.Id,
                    ContatoId = atendimento.ContatoId,
                    NomeCliente = nome,
                    UsernameCliente = username,
                    TelegramChatId = chatId,
                    Status = atendimento.Status,
                    DataInicio = atendimento.DataInicio,
                    UltimaMensagem = texto
                };

                await _hubContext.Clients.All.SendAsync("NovaConversaNaFila", atendimentoDto);
                _logger.LogInformation("Notificação de nova conversa na fila transmitida via SignalR (Atendimento #{Id})", atendimento.Id);
            }
            else
            {
                var menu = $"Olá, {nome}! Bem-vindo ao Teledesk 📞\n\n" +
                           "Como podemos te ajudar hoje?\n" +
                           "1 - Horário de Atendimento\n" +
                           "2 - Falar com Atendente Humano\n\n" +
                           "Digite o número da opção desejada:";

                await EnviarMensagemAsync(atendimento.Id, chatId, menu);
            }
        }
    }
}
