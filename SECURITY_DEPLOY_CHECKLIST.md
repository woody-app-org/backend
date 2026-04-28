# Checklist De Deploy Seguro

Use este checklist antes de publicar o backend Woody em qualquer ambiente acessível fora da máquina local.

## Secrets E Configuração

- `Jwt__Secret` definido por secret manager/env var, com valor forte, único por ambiente e nunca commitado.
- `ConnectionStrings__DefaultConnection` ou `DATABASE_URL` definido fora do código e fora de logs.
- `Resend__ApiKey` definido por secret manager/env var.
- `Billing__Stripe__SecretKey` e `Billing__Stripe__WebhookSecret` definidos por secret manager/env var quando billing Stripe estiver ativo.
- Stripe price ids definidos por ambiente, sem reutilizar chaves de teste em produção.
- `CORS_ORIGINS` definido em produção com origens exatas do frontend, separadas por vírgula. Não use wildcard com credenciais.
- `WOODY_ENABLE_DEV_SEED` ausente ou `false` em produção.
- `WOODY_EF_ENABLE_SENSITIVE_LOGGING` ausente ou `false` em CI, staging compartilhado e produção.

## Banco De Dados

- PostgreSQL não exposto publicamente. Use rede privada/VPC, túnel controlado ou allowlist estrita.
- Porta `5432` não publicada em host de produção.
- Conexões externas exigem TLS/SSL (`sslmode=require`, `verify-ca` ou `verify-full`, conforme o provedor).
- Usuário da aplicação sem privilégios de superuser e com permissões mínimas necessárias.
- Migrations aplicadas por pipeline controlado, com backup antes de alterações destrutivas.
- Backups automáticos configurados e restauração testada.
- Retenção, criptografia em repouso e rotação de credenciais documentadas.

## Seeds E Dados Sensíveis

- `DbSeeder` executado apenas em `Development` local.
- Nenhuma conta demo (`admin123`, `user123`, `Woody2026!`) presente em banco de produção.
- Nenhum dado real inserido por migrations ou seed.
- Dumps de produção anonimizados antes de uso local.

## Logs E Observabilidade

- Logs não incluem request body, senha, access token, refresh token, `Authorization` header, Stripe secret ou connection string.
- Proxies/load balancers não registram query string de `/hubs/*`, pois SignalR pode transportar `access_token` na query durante WebSocket.
- EF sensitive data logging desligado em CI/staging compartilhado/produção.
- Logs de erro externos têm redaction para headers e query params sensíveis.
- Acesso aos logs limitado por papel e com retenção definida.

## Rede E HTTP

- HTTPS obrigatório no tráfego público.
- `ForwardedHeaders` configurado apenas atrás de proxy confiável no provedor.
- Health checks públicos não expõem config, versão detalhada, connection string nem stack trace.
- `AllowedHosts` e regras do proxy/WAF revisados para o domínio real.

## Stripe E Compras

- Webhook Stripe configurado para o endpoint correto e usando `WebhookSecret` do ambiente.
- Redirect de sucesso não ativa plano.
- Frontend nunca ativa plano.
- Eventos Stripe duplicados são idempotentes.
- Falhas parciais do webhook geram retry sem marcar evento como processado.

## Revisão Final

- `dotnet test .\Woody.sln` passa antes do deploy.
- Migrations pendentes revisadas.
- Variáveis obrigatórias conferidas no ambiente alvo.
- Plano de rollback definido.
- Rotação de secrets e procedimento de revogação documentados.
