# 💬 Teledesk - Sistema de Atendimento Multi-Atendente e Multi-Setor integrado ao WhatsApp

Projeto prático de estudo end-to-end desenvolvido em **.NET 9**, **ASP.NET Core Web API**, **JWT**, **SignalR**, **Entity Framework Core (MySQL / Pomelo)** e **Windows Forms (WinForms)**.

> 📋 **Plano de Evolução Completo:** Consulte o documento [`PLANO_DE_ACAO.md`](./PLANO_DE_ACAO.md) para ver o diagnóstico da base atual, a arquitetura alvo, a modelagem multi-setor/multi-número e todas as fases planejadas.

---

## 🏗️ Arquitetura Geral

```text
[ Cliente (WhatsApp) ]
        │
        ▼ (Webhook HTTP - WhatsApp Cloud API / Meta)
[ ASP.NET Core Web API (.NET 9 + JWT + SignalR por Setor) ] ◄──► [ MySQL (EF Core / Pomelo) ]
        │
        ▼ (REST + SignalR WebSockets autenticados via JWT)
[ WinForms Desktop - Teledesk.Desktop (Atendentes & Admin) ]
```

### Principais Capacidades Planejadas na Evolução
- **Autenticação Segura com JWT:** Evoluir o login atual (que usa senha em texto puro) para hash seguro, perfis (`Atendente` / `Admin`) e proteção via JWT Bearer para API e SignalR.
- **Migração para WhatsApp Cloud API (Oficial da Meta):** Recebimento via Webhook oficial e envio via Graph API por `PhoneNumberId`.
- **Atendimento Multi-Setor com Número Único ou Dedicado:**
  - **Número compartilhado:** vários setores/atendentes usam o mesmo número; o cliente escolhe o setor via URA dinâmica e entra na fila do setor escolhido.
  - **Número dedicado por setor:** um número específico pode ser vinculado a um único setor, direcionando o cliente diretamente para a fila daquele setor.
- **Filas por Setor em Tempo Real (SignalR):** Cada atendente recebe apenas os atendimentos e mensagens dos setores aos quais está vinculado.

---

## 💻 Como Continuar / Rodar em uma Nova Máquina

1. **Clonar o repositório:**
   ```bash
   git clone https://github.com/LimaLucas21/Teledesk.git
   cd Teledesk
   ```

2. **Configurar as Credenciais Locais:**
   Crie o arquivo `Teledesk.API/appsettings.Development.json` (ignorado pelo `.gitignore`) com a string de conexão do MySQL (e, nas próximas fases, a chave JWT e as credenciais da **WhatsApp Cloud API**):
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=teledesk_db;Uid=root;Pwd=sua_senha;"
     }
   }
   ```

3. **Restaurar, Aplicar Migrations e Executar o Projeto:**
   ```bash
   dotnet restore
   dotnet ef database update --project Teledesk.API
   dotnet run --project Teledesk.API/Teledesk.API.csproj
   ```

---

## 🗺️ Roadmap do Projeto

### Base Inicial (Concluída com Telegram + MySQL)
- [x] Setup da Solution `.sln`, estrutura inicial da API e integração básica inicial com Telegram.
- [x] Migração para **MySQL (EF Core / Pomelo)** e criação das entidades iniciais (`Contato`, `Atendente`, `Atendimento`, `Mensagem`).
- [x] Motor inicial de URA (`ConversationService`), `AtendimentoHub` (SignalR), login inicial (`AtendentesController`) e cliente WinForms (`Teledesk.Desktop`).

### Evolução — WhatsApp Cloud API, Login JWT e Multi-Setor
- [ ] **Fase 0:** Limpeza & Segurança (Saída do Telegram, remoção do `TelegramPollingService` e remoção de token exposto)
- [ ] **Fase 1:** Evolução do Login para Autenticação Segura (`JWT`, hash de senha, `PerfilAtendente` e proteção do Hub/API)
- [ ] **Fase 2:** Modelagem Multi-Setor e Multi-Número no MySQL (`Setor`, `NumeroWhatsApp`, vínculos N:N e CRUDs Admin)
- [ ] **Fase 3:** Integração WhatsApp Cloud API (Webhook Meta + Envio via Graph API por `PhoneNumberId`)
- [ ] **Fase 4:** Evolução do `ConversationService` (URA Dinâmica vs. Roteamento Direto por Número + Fila por Setor)
- [ ] **Fase 5:** Evolução do `AtendimentoHub` (SignalR com Grupos por Setor e proteção contra concorrência)
- [ ] **Fase 6:** Evolução do `Teledesk.Desktop` (Login JWT, Fila por Setor e Módulo Admin no WinForms)
- [ ] **Fase 7:** Resiliência, Idempotência (`WhatsAppMessageId`), Logs Estruturados e Preparação para Produção

---

## 🎯 Próximo Desafio: Fase 0 — Limpeza & Segurança (Saída do Telegram)

Antes de evoluir a autenticação para JWT e implementar a arquitetura multi-setor para WhatsApp:
1. Remover o pacote `Telegram.Bot` de `Teledesk.API/Teledesk.API.csproj`.
2. Remover `TelegramPollingService.cs`, `BotController.cs` e desacoplar `ITelegramBotClient` de `Program.cs`, `ConversationService.cs` e `AtendimentoHub.cs`.
3. Revogar o token antigo do Telegram no `@BotFather` e removê-lo de `Teledesk.API/appsettings.json`.
4. Limpar o arquivo `Teledesk.API/Teledesk.API.http`.
