# 💬 Teledesk - Sistema de Atendimento Multi-Atendente e Multi-Setor integrado ao WhatsApp

Projeto prático end-to-end desenvolvido em **.NET 9**, **ASP.NET Core Web API**, **JWT**, **SignalR**, **Entity Framework Core (MySQL / Pomelo)** e **Windows Forms (WinForms)**.

> 📋 **Plano de Evolução:** Consulte [`PLANO_DE_ACAO.md`](./PLANO_DE_ACAO.md) para detalhes da arquitetura, modelagem multi-setor/multi-número e roadmap.

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

### Principais Recursos Implementados
- **Login Seguro com JWT (`HS256`) + Hash de Senha:** Autenticação ligada ao servidor local (`POST /api/atendentes/login`), com perfis (`Atendente` e `Admin`) protegendo API REST e SignalR Hub.
- **Integração WhatsApp Cloud API (Oficial da Meta):**
  - Verificação de webhook (`GET /api/webhook/whatsapp`), recebimento oficial (`POST /api/webhook/whatsapp`) e endpoint de simulação local (`POST /api/webhook/whatsapp/simular`).
  - Envio via Graph API utilizando o `PhoneNumberId` de cada número cadastrado (`WhatsAppCloudService`).
- **Atendimento Multi-Setor com Número Único ou Dedicado:**
  - **Número compartilhado (`1 número -> N setores`):** exibe URA dinâmica com os setores vinculados e roteia para a fila do setor escolhido.
  - **Número dedicado (`1 número -> 1 setor`):** pula a URA e envia o cliente diretamente para a fila daquele setor.
- **Filas por Setor em Tempo Real (`SignalR`):** Cada operador entra automaticamente nos grupos `setor-{id}` dos seus setores e só recebe/assume chamados pertencentes a eles (com trava de concorrência).
- **Painel Desktop WinForms (`Teledesk.Desktop`):** Login JWT, filtro por setor, chat bidirecional e tela de **Gestão** (`GestaoForm`) para cadastrar setores, números WhatsApp, atendentes e simular mensagens localmente.

---

## 💻 Como Rodar Localmente

1. **Clonar o repositório:**
   ```bash
   git clone https://github.com/LimaLucas21/Teledesk.git
   cd Teledesk
   ```

2. **Configurar Banco MySQL e Credenciais Locais:**
   Ajuste `Teledesk.API/appsettings.json` ou crie `Teledesk.API/appsettings.Development.json` (ignorado pelo `.gitignore`):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=teledesk_db;Uid=root;Pwd=sua_senha;"
     },
     "JwtSettings": {
       "SecretKey": "SUA_CHAVE_SECRETA_COM_PELO_MENOS_32_CARACTERES",
       "Issuer": "Teledesk.API",
       "Audience": "Teledesk.Desktop",
       "ExpirationHours": 12
     },
     "WhatsAppSettings": {
       "ApiVersion": "v21.0",
       "VerifyToken": "teledesk_verify_token",
       "DefaultAccessToken": "SEU_TOKEN_META_CLOUD_API_OU_VAZIO_PARA_SIMULACAO",
       "AppSecret": ""
     }
   }
   ```

3. **Executar a API e o Desktop:**
   ```bash
   dotnet restore
   dotnet run --project Teledesk.API/Teledesk.API.csproj
   dotnet run --project Teledesk.Desktop/Teledesk.Desktop.csproj
   ```
   > Na primeira inicialização com o MySQL rodando, a API aplica as migrations e cria o usuário administrador padrão (`admin@teledesk.com` / `admin123`), 3 setores iniciais (`Suporte Técnico`, `Financeiro`, `Comercial`) e um número WhatsApp padrão para testes.

---

## 🗺️ Roadmap do Projeto

### Base Inicial (Concluída)
- [x] Setup da Solution `.sln`, MySQL (`Pomelo`), `ConversationService`, `AtendimentoHub` e `Teledesk.Desktop`.

### Evolução — WhatsApp Cloud API, Login JWT e Multi-Setor
- [x] **Fase 0:** Limpeza & Segurança (Saída completa do Telegram e remoção do token exposto)
- [x] **Fase 1:** Sistema de Login Seguro (`JWT`, `PasswordHasher`, `PerfilAtendente` e proteção do Hub/API)
- [x] **Fase 2:** Modelagem Multi-Setor e Multi-Número no MySQL (`Setor`, `NumeroWhatsApp`, N:N e CRUDs)
- [x] **Fase 3:** Integração WhatsApp Cloud API (`WhatsAppWebhookController` + `WhatsAppCloudService` por `PhoneNumberId` + Simulador local)
- [x] **Fase 4:** Evolução do `ConversationService` (URA Dinâmica vs. Roteamento Direto por Número + Fila por Setor)
- [x] **Fase 5:** Evolução do `AtendimentoHub` (SignalR com Grupos por Setor e trava contra concorrência)
- [x] **Fase 6:** Evolução do `Teledesk.Desktop` (Login JWT, Filtro por Setor e `GestaoForm`)
- [ ] **Fase 7:** Testes ponta a ponta com Meta Webhook real via túnel HTTPS, retries de HTTP e publicação em servidor externo
