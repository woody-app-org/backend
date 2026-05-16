# Multimédia (Woody)

Documentação operacional: limites, fluxo e extensões futuras (bucket R2/S3, GIF/sticker).

## Limites (referência)

| Contexto | Imagem / GIF / sticker (upload) | Vídeo post (upload) | Vídeo mensagem (upload) |
|----------|----------------------------------|---------------------|-------------------------|
| Tamanho máximo | `MediaReferenceConstraints.ImageMaxUploadBytes` (10 MiB) | `PostVideoMaxUploadBytes` (100 MiB) | `MessageVideoMaxUploadBytes` (50 MiB) |
| Duração declarada (vídeo) | — | `PostVideoMaxDeclaredSeconds` (120 s) | `MessageVideoMaxDeclaredSeconds` (30 s) |

Configurável em `appsettings.json` → secção `MediaStorage` (`MaxImageSizeBytes`, `MaxPostVideoUploadBytes`, `MaxMessageVideoUploadBytes`).

## Fluxo de upload (API)

1. Cliente chama `POST /api/media/images` ou `POST /api/media/videos` com `multipart/form-data`, ficheiro e contexto (`scope=post|message`, `publicationContext`, `communityId` ou `conversationId`).
2. `MediaUploadApplicationService` valida permissões (post em perfil/comunidade ou participação na conversa).
3. `MediaUploadService` valida MIME, extensão, assinatura mágica, tamanho e duração (vídeo).
4. `IMediaStorageProvider` grava o blob e devolve `StorageKey` + metadados; a URL pública vem de `BuildPublicImageUrl` / `BuildPublicVideoUrl` (local ou CDN).

## Persistência em posts / mensagens

- O cliente envia `url` (e opcionalmente `storageKey`, `mimeType`, `fileSize` alinhados ao resultado do upload).
- O servidor **resolve** a `storageKey` a partir de URLs locais (`/api/media/images/…`, `/api/media/videos/…`) e **rejeita** `storageKey` / `mimeType` / `fileSize` inconsistentes ou indevidos para URLs externas (`LocalAttachmentRequestMetadata`).
- `MediaAttachment` guarda `Url`, `StorageKey`, `MimeType`, `FileSize` quando aplicável.

## Armazenamento remoto (R2 / S3-compatible)

1. Definir `MediaStorage:Driver` = `S3` em configuração (ou variável `MediaStorage__Driver`).
2. Preencher secção `R2` ou variáveis planas `R2_ACCOUNT_ID`, `R2_ACCESS_KEY_ID`, `R2_SECRET_ACCESS_KEY`, `R2_BUCKET`, opcionalmente `R2_PUBLIC_BASE_URL`, `R2_SERVICE_URL`, `R2_REGION`.
3. Implementação: `S3CompatibleMediaStorageProvider` (pacote `AWSSDK.S3`). URLs públicas usam `R2_PUBLIC_BASE_URL` quando definido; caso contrário mantêm-se rotas `/api/media/…` servidas via `OpenReadAsync` no bucket.

Ver também comentários em `DependencyInjectionConfig` (API) e `R2MediaStorageEnvironmentConfigure`.

## GIF / sticker (catálogo)

- Pesquisa: endpoint de messaging que delega em `IGifStickerSearchProvider` (implementação local de catálogo na infra).
- Para trocar o provedor (Tenor, GIPHY, etc.), registar outra implementação de `IGifStickerSearchProvider` no DI da API, mantendo o contrato de DTOs em `StickerGifSearchItemDto`.

## Segurança (resumo)

- Upload: extensões perigosas bloqueadas (`UploadedImagePolicy` / `UploadedVideoPolicy`); validação de conteúdo por magic bytes.
- URLs em posts: `InputValidator` + políticas de domínio (`PublicImageUrlPolicy`, `PublicVideoUrlPolicy`); anexos em DM: `DirectMessageAttachmentPolicy`.
- Metadados declarados no create post/message só são aceites quando coerentes com URLs de media Woody.
