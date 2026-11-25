# Plano de Ação — Evolução do Teledesk

---

## 1. Resumo da Evolução Implementada

O projeto evoluiu de um protótipo mono-canal (Telegram, sem separação de setores e com senha em texto puro) para uma arquitetura multi-atendente, multi-setor e multi-número preparada para **WhatsApp Cloud API (Meta)**:

1. **Autenticação JWT + Hash de Senha:** `AtendentesController` autentica com `PasswordHasher<Atendente>`, emite token JWT (`HS256` via `JwtTokenService`) e protege endpoints e SignalR com `JwtBearerAuthHandler`.
2. **Saída do Telegram e Entrada da WhatsApp Cloud API:** remoção de `Telegram.Bot`, `TelegramPollingService` e `BotController`; criação de `WhatsAppWebhookController` (`GET` verificação Meta, `POST` evento oficial e `POST /simular`) e `WhatsAppCloudService`.
3. **Estrutura Multi-Setor e Multi-Número:**
   - **Número Compartilhado (`1 NumeroWhatsApp -> N Setores`):** URA dinâmica lista os setores vinculados e encaminha para a fila do setor escolhido.
   - **Número Dedicado (`1 NumeroWhatsApp -> 1 Setor`):** pula a URA e envia direto para a fila daquele setor.
4. **SignalR por Setor (`AtendimentoHub`):** usuários conectados entram nos grupos `setor-{id}` dos seus setores e têm trava de concorrência ao assumir chamados.
5. **Painel WinForms (`Teledesk.Desktop`):** atualizado com envio de JWT, filtro por setor e nova janela `GestaoForm` para administrar setores, números WhatsApp, atendentes e simular mensagens.

---

## 2. Arquitetura Atual

```text
[ Cliente (WhatsApp) ]
        │
        ▼ (Webhook HTTP - WhatsApp Cloud API / Meta)
[ ASP.NET Core Web API (.NET 9 + JWT + SignalR por Setor) ] ◄──► [ MySQL (EF Core / Pomelo) ]
        │
        ▼ (REST + SignalR WebSockets autenticados via JWT)
[ WinForms Desktop - Teledesk.Desktop (Login JWT + Fila por Setor + GestaoForm) ]
```

---

## 3. Status das Fases

### Fase 0 — Limpeza & Segurança (Saída do Telegram) — Concluída
- [x] Remover `Telegram.Bot` de `Teledesk.API.csproj`.
- [x] Remover `TelegramPollingService.cs` e `BotController.cs`.
- [x] Remover `TelegramSettings` de `appsettings.json`.

### Fase 1 — Login Seguro com JWT e Perfis — Concluída
- [x] Criar `PerfilAtendente` (`Atendente`, `Admin`).
- [x] Atualizar `Atendente` (`SenhaHash`, `Perfil`, `Ativo`, `Setores`).
- [x] Implementar `IJwtTokenService` / `JwtTokenService` e `JwtBearerAuthHandler`.
- [x] Atualizar `AtendentesController` com `PasswordHasher`, seed inicial (`admin@teledesk.com` / `admin123`) e `[Authorize]`.

### Fase 2 — Modelagem Multi-Setor e Multi-Número no MySQL — Concluída
- [x] Criar `Setor` e `NumeroWhatsApp`.
- [x] Atualizar `Contato` (`WhatsAppId`), `Atendimento` (`SetorId`, `NumeroWhatsAppId`) e `Mensagem` (`WhatsAppMessageId`).
- [x] Configurar Fluent API no `TeledeskDbContext` e gerar migration EF Core.
- [x] Criar `SetoresController` e `NumerosWhatsAppController` com proteção `[Authorize(Roles = "Admin")]`.

### Fase 3 — Integração WhatsApp Cloud API (Meta) — Concluída
- [x] Criar DTOs oficiais da Meta (`WhatsAppWebhookDto.cs`).
- [x] Criar `IWhatsAppService` e `WhatsAppCloudService` com fallback de log para simulação local.
- [x] Criar `WhatsAppWebhookController` (`GET` verificação `hub.challenge`, `POST` recebimento com validação opcional `X-Hub-Signature-256`, e `POST /simular`).

### Fase 4 — Motor de Estado (`ConversationService` Multi-Setor) — Concluída
- [x] Idempotência por `WhatsAppMessageId`.
- [x] Roteamento direto para `AguardandoAtendente` quando o número possui 1 setor ativo.
- [x] URA dinâmica listando os setores vinculados quando o número possui 2+ setores ativos.

### Fase 5 — SignalR com Grupos por Setor — Concluída
- [x] Proteger `AtendimentoHub` com `[Authorize]`.
- [x] Associar conexões aos grupos `setor-{setorId}` em `OnConnectedAsync`.
- [x] Filtrar `ObterFilaAtendimento` por setor e validar concorrência em `AssumirAtendimento`.

### Fase 6 — Painel Desktop WinForms (`Teledesk.Desktop`) — Concluída
- [x] Atualizar `Form1` para enviar Bearer Token no SignalR, filtrar fila por setor e exibir setor/número do atendimento.
- [x] Criar `GestaoForm` com abas de Setores, Números WhatsApp, Atendentes e Simulador WhatsApp.

### Fase 7 — Validação Externa & Produção — Próximos Passos
- [ ] Subir túnel HTTPS (Dev Tunnels / ngrok) e cadastrar o Webhook no Meta for Developers com um `PhoneNumberId` e `AccessToken` reais.
- [ ] Configurar política de retry HTTP (Polly / `Microsoft.Extensions.Http.Resilience`) para chamadas à Graph API.
- [ ] Publicar API em servidor externo (VPS / Azure / Docker) com variáveis de ambiente seguras.
