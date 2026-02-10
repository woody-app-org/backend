# Woody Project

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started)
- [Docker Compose](https://docs.docker.com/compose/install/)
- PowerShell

## Configuração e Execução

### 1. Inicializar o banco de dados

Execute o Docker Compose para provisionar o banco de dados PostgreSQL:

```bash
docker compose up --build
```

O banco será configurado com as seguintes credenciais:
- **Host:** localhost
- **Porta:** 5432
- **Usuário:** woody_user
- **Senha:** woody@123

### 2. Aplicar as migrações

Navegue até o diretório `src` e execute as migrações do Entity Framework:

```bash
cd src
```

```powershell
$env:DB_HOST="localhost"; $env:DB_PORT="5432"; $env:DB_USERNAME="woody_user"; $env:DB_PASS="woody@123"; dotnet ef database update --project .\Woody.Infrastructure\
```

### 3. Executar a aplicação

```powershell
dotnet run --project .\Woody.Api\ --launch-profile https
```

A aplicação estará disponível em `https://localhost` (porta exibida no console).

## Comandos Adicionais

### Criar nova migração

```powershell
dotnet ef migrations add NomeDaMigracao --project .\Woody.Infrastructure\
```

### Reverter migração

```powershell
$env:DB_HOST="localhost"; $env:DB_PORT="5432"; $env:DB_USERNAME="woody_user"; $env:DB_PASS="woody@123"; dotnet ef database update MigracaoAnterior --project .\Woody.Infrastructure\
```

## Estrutura do Projeto

```
src/
├── Woody.Api/              # Camada de apresentação
├── Woody.Infrastructure/   # Camada de infraestrutura e persistência
```

## Observações

- Certifique-se de que o Docker está em execução antes de aplicar as migrações
- Todos os comandos devem ser executados a partir do diretório `src/`
- As variáveis de ambiente são configuradas inline para garantir a conexão correta com o banco de dados