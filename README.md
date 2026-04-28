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

A partir de **`src/`**:

```powershell
dotnet run --project .\Woody.Api\ --launch-profile http
```

- Perfil **`http`**: `http://localhost:5000` (Swagger em `/swagger`). Alinhado com o frontend em dev (`VITE_API_BASE_URL` → `http://localhost:5000`).
- Perfil **`https`**: também expõe `http://localhost:5000` em conjunto com HTTPS.

Com `.env` na raiz do backend, pode carregar antes de correr a API:

```powershell
. .\scripts\Load-DotEnv.ps1
cd src
dotnet run --project .\Woody.Api\ --launch-profile http
```

### 5. Frontend (repositório separado)

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
├── docker-compose.yml      # Postgres local
├── .env.example            # Modelo (versionado)
├── .env                    # Local — NÃO commitar
├── run-migrations.ps1
├── scripts/
│   └── Load-DotEnv.ps1
└── src/
    ├── Woody.Api/
    ├── Woody.Application/
    ├── Woody.Domain/
    └── Woody.Infrastructure/   # EF Core, migrações, seed
```