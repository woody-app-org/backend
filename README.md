# Woody — API (backend)

API ASP.NET Core com PostgreSQL. Este repositório contém a solução .NET; o frontend (Vite/React) é um projeto à parte.

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (compatível com a solução `Woody.sln`)
- [Docker](https://www.docker.com/get-started) (apenas para PostgreSQL local)
- PowerShell (Windows) ou terminal com suporte a variáveis de ambiente

## Arranque rápido (local)

### 1. Subir o PostgreSQL

Na raiz deste repositório (`backend/`):

```bash
docker compose up -d
```

Credenciais **de desenvolvimento local** (definidas em `docker-compose.yml`). Não reutilize estes valores em produção; o compose faz bind do Postgres apenas em `127.0.0.1:5432`.

| Campo    | Valor        |
|----------|--------------|
| Host     | `localhost`  |
| Porta    | `5432`       |
| Base     | `woody_db`   |
| Utilizador | `woody_user` |
| Palavra-passe | `woody@123` |

### 2. Configurar variáveis de ambiente

1. Copie o exemplo: `copy .env.example .env` (ou equivalente no macOS/Linux).

A API e as ferramentas EF leem a ligação através de:

- **`ConnectionStrings__DefaultConnection`** (Npgsql), ou
- **`DATABASE_URL`** (formato `postgresql://...`, útil alinhado com Railway/Heroku).

Na `DATABASE_URL`, caracteres especiais na palavra-passe devem estar **codificados em URL** (ex.: `@` → `%40`).

Exemplo equivalente ao Docker Compose local:

```env
DATABASE_URL=postgresql://woody_user:woody%40123@localhost:5432/woody_db
```

Ou, em alternativa:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=woody_db;Username=woody_user;Password=woody@123
```

Defina também:

- `Jwt__Secret` (mínimo ~32 caracteres recomendado fora de dev)
- `Resend__ApiKey` (chave da API Resend)
- `Resend__FromEmail` (remetente usado no envio do código)
- `Resend__FromName` (opcional, nome do remetente)
- `CORS_ORIGINS` em produção, com as origens exatas do frontend separadas por vírgula
- `PRELAUNCH_HASH_SECRET` ou `PreLaunch__HashSecret` (segredo forte para o fluxo de pré-inscrição em produção — ver secção «Primeiro release»)

Parâmetros de verificação de e-mail (com valores padrão no `appsettings.json`, sobrescrevíveis por env vars):

- `EmailVerification__ExpirationMinutes`
- `EmailVerification__MaxAttempts`

Ver comentários em `.env.example`.

### 3. Aplicar migrações (base de dados)

**Opção A — script (recomendado)**  
Carrega automaticamente o `.env` e corre o EF a partir de `src/`:

```powershell
.\run-migrations.ps1
```

**Opção B — comando manual (PowerShell)**  
Defina a ligação na sessão atual e execute a partir da pasta **`src/`**:

```powershell
cd src
$env:DATABASE_URL = "postgresql://woody_user:woody%40123@localhost:5432/woody_db"
dotnet ef database update --project .\Woody.Infrastructure\
```

**Opção C — variáveis já exportadas**  
Se `DATABASE_URL` ou `ConnectionStrings__DefaultConnection` já estiverem definidas no sistema, basta `dotnet ef database update` com os mesmos `--project` / `--startup-project` acima, a partir de `src/`.

O `WoodyDbContextFactory` (design-time) também carrega `appsettings.json` de `Woody.Api` se encontrar a árvore do repositório; mesmo assim, para migrar costuma ser mais simples usar `.env` + `run-migrations.ps1` ou `DATABASE_URL` na sessão. Logging sensível do EF em design-time só é ativado com `WOODY_EF_ENABLE_SENSITIVE_LOGGING=true`; não use essa flag em CI ou produção.

### 4. Executar a API

**Recomendado (raiz do backend):** carrega o `.env` e **não** usa perfil de lançamento que sobrescreva o ambiente:

```powershell
.\run-api.ps1
```

(Isto usa `--no-launch-profile` e URLs `https://localhost:7101` + `http://localhost:5000`, para o `ASPNETCORE_ENVIRONMENT` do `.env` valer de facto.)

**Porque o `.env` parecia ser ignorado:** o ficheiro `Properties/launchSettings.json` costumava definir `ASPNETCORE_ENVIRONMENT=Development` nos perfis `http`/`https`. O `dotnet run --launch-profile ...` **aplica essas variáveis por cima** das que vêm do `.env` na mesma sessão — por isso continuavas com seed e `EnableSensitiveDataLogging` mesmo com `Production` no `.env`.

**Manual a partir da raiz deste repositório (`backend/`)**:

```powershell
. .\scripts\Load-DotEnv.ps1
cd src
dotnet run --project .\Woody.Api\ --no-launch-profile --urls "http://localhost:5000"
# ou HTTPS local:
dotnet run --project .\Woody.Api\ --no-launch-profile --urls "https://localhost:7101;http://localhost:5000"
```

- Com **`ASPNETCORE_ENVIRONMENT=Development`** no `.env`: Swagger em `/swagger` (se estiver registado), seed opcional conforme `Program.cs`.
- Com **`Production`**: sem seed automático por ambiente, sem logging sensível do EF em runtime (`WoodyDbConfiguration`).

Perfis **`http`** / **`https`** no Visual Studio/Rider continuam a definir URLs; define **`ASPNETCORE_ENVIRONMENT`** no `.env` ou nas propriedades de depuração do IDE se precisares de Development ao premir F5.

### 5. Primeiro release (produção / Railway)

A V1 pública foca-se no **cadastro pré-lançamento** (`POST /api/prelaunch/signups`). O processo de arranque em **`Production`** valida **toda** a configuração da API (`Jwt`, `Resend`, `EmailVerification`, `Billing`, `CORS_ORIGINS`, etc.) — não existe modo “só waitlist” no código; todas as variáveis abaixo têm de estar definidas no serviço (ou a aplicação **não inicia**). Detalhe: `Program.cs`, `ValidateProductionDeployment`, `ValidateResendOptions`.

#### Docker (imagem na raiz do `backend/`)

```powershell
docker build -t woody-api .
# Exemplo mínimo; em produção injeta todas as variáveis (Railway, etc.)
docker run --rm -p 8080:8080 -e PORT=8080 woody-api
```

O **`Dockerfile`** define `ASPNETCORE_ENVIRONMENT=Production`, `PORT=8080` e `ENTRYPOINT ["dotnet", "Woody.Api.dll"]`. O Kestrel escuta em `0.0.0.0:$PORT` quando `PORT` existe (`ConfigureRailwayPort` em `Program.cs`).

#### Railway (resumo)

| Campo | Valor |
|--------|--------|
| **Root Directory** | Pasta onde estão `Woody.sln` e `Dockerfile` (se o mono-repo tiver frontend, apontar para a subpasta `backend`). |
| **Dockerfile** | `Dockerfile` (raiz do backend). |
| **Porta** | `8080` (ou o que o Railway injetar em `PORT` — o código usa `PORT`). |
| **Health check** | **`/health`** (liveness) ou **`/health/ready`** (inclui PostgreSQL; se a BD falhar, responde 503). |

**Migrações:** o Dockerfile **não** corre `dotnet ef`. Aplica-as uma vez (CI, job one-shot ou máquina com SDK) contra a mesma `DATABASE_URL` do serviço:

```powershell
cd src
dotnet ef database update --project .\Woody.Infrastructure\ --startup-project .\Woody.Api\
```

#### Variáveis obrigatórias / críticas no primeiro release

| Variável | Notas |
|----------|--------|
| `DATABASE_URL` ou `ConnectionStrings__DefaultConnection` | PostgreSQL (`DatabaseConnectionResolver`). |
| `CORS_ORIGINS` | Origens do frontend, separadas por vírgula. Em `Production` **é obrigatório** (senão a API não arranca). |
| `Jwt__Secret` | Mínimo **32** caracteres em `Production` (`Program.cs`). |
| `Resend__ApiKey`, `Resend__FromEmail` | Obrigatórios no arranque (`ValidateResendOptions`). |
| `PRELAUNCH_HASH_SECRET` ou `PreLaunch__HashSecret` | Segredo forte para hash de IP/UA no pré-lançamento; **não deixar vazio** em produção (`PreLaunchController.ResolveHashSecret`). |
| `ForwardedHeaders__TrustPrivateNetworkProxies` | `true` recomendado atrás do proxy do Railway para **IP real** em rate limit e pré-inscrição (`Program.cs`). |
| `PUBLIC_LAUNCH_MODE` | Opcional: `waitlist_form` bloqueia o resto da API e deixa só pré-inscrição + health + CORS preflight (`WaitlistFormModeMiddleware`). |
| `WOODY_ENABLE_DEV_SEED` | Não definir como `true` em produção. |

Stripe: se preencheres chaves/URLs de billing em produção, `ValidateProductionStripeOptions` exige HTTPS e secrets — vê `Program.cs`. Se Stripe estiver vazio no config, essa validação extra não corre.

Checklist adicional: `SECURITY_DEPLOY_CHECKLIST.md`.

### 6. Frontend (repositório separado)

No projeto **woody-frontend**:

1. `npm install`
2. Copie `.env.example` → `.env.development` (ou use `VITE_API_BASE_URL=http://localhost:5000`).
3. `npm run dev`

## Seed de desenvolvimento

Com `ASPNETCORE_ENVIRONMENT=Development`, ao iniciar a API pode executar-se o `DbSeeder` (dados fictícios para testes: utilizadoras, comunidades, posts, etc.). Em outros ambientes não produtivos, só corre se definir `WOODY_ENABLE_DEV_SEED=true`. Em `Production`, a API falha ao iniciar se essa flag estiver ativa.

Contas de exemplo (ver também `DbSeeder.cs`): `admin` / `admin123`, `user1`…`user4` com as palavras-passe documentadas no código de seed, e `user5`–`user20` com palavra-passe de seed comum. Essas credenciais são públicas e devem existir apenas em bancos locais descartáveis.

## Comandos EF úteis

Executar a partir de **`src/`** (com a mesma ligação à BD que nas migrações):

```powershell
# Nova migração
dotnet ef migrations add NomeDaMigracao --project .\Woody.Infrastructure\

# Reverter para uma migração específica
dotnet ef database update NomeDaMigracaoAnterior --project .\Woody.Infrastructure\
```

## Estrutura (resumo)

```
backend/
├── Woody.sln
├── Dockerfile            # Build/publish Production (Railway, etc.)
├── docker-compose.yml    # Postgres local
├── .env.example          # Modelo (versionado)
├── .env                  # Local — NÃO commitar
├── run-api.ps1
├── run-migrations.ps1
├── scripts/
│   └── Load-DotEnv.ps1
└── src/
    ├── Woody.Api/
    ├── Woody.Application/
    ├── Woody.Domain/
    └── Woody.Infrastructure/   # EF Core, migrações, seed
```

## Build e testes (antes do release)

Na raiz do `backend/`:

```powershell
dotnet restore Woody.sln
dotnet build Woody.sln -c Release
dotnet test Woody.sln -c Release
```