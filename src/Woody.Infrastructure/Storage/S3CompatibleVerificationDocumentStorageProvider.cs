using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;

namespace Woody.Infrastructure.Storage;

/// <summary>
/// Provider R2/S3-compatible <b>privado</b> para documentos de verificação de identidade.
/// <para>
/// Os blobs são gravados em bucket sem acesso público habilitado.
/// O documento NUNCA é exposto por URL direta — o acesso é exclusivamente via streaming
/// no endpoint <c>GET /api/admin/verification/{id}/document</c> (SuperAdminOnly).
/// </para>
/// </summary>
public sealed class S3CompatibleVerificationDocumentStorageProvider
    : IVerificationDocumentStorageProvider, IDisposable
{
    private static readonly Dictionary<string, string> ContentTypeByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"]  = "image/png"
        };

    private readonly string _bucket;
    private readonly AmazonS3Client _client;

    public S3CompatibleVerificationDocumentStorageProvider(IOptions<VerificationR2StorageOptions> r2)
    {
        var opts = r2.Value;
        EnsureConfigured(opts);
        _bucket = opts.Bucket.Trim();

        var endpoint = string.IsNullOrWhiteSpace(opts.ServiceUrl?.Trim())
            ? $"https://{opts.AccountId.Trim()}.r2.cloudflarestorage.com"
            : opts.ServiceUrl!.Trim();

        var cfg = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = string.IsNullOrWhiteSpace(opts.Region) ? "auto" : opts.Region.Trim()
        };

        _client = new AmazonS3Client(
            new BasicAWSCredentials(opts.AccessKeyId.Trim(), opts.SecretAccessKey.Trim()),
            cfg);
    }

    public void Dispose() => _client.Dispose();

    /// <summary>
    /// Persiste o documento no bucket privado com chave <c>verif/{userId}/{guid}.{ext}</c>.
    /// Não usa o nome original do arquivo — apenas a extensão normalizada.
    /// </summary>
    public async Task<string> SaveAsync(
        int userId,
        Stream content,
        string normalizedExtension,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new ArgumentException("userId inválido.", nameof(userId));

        if (!ContentTypeByExtension.TryGetValue(normalizedExtension, out var contentType))
            throw new ArgumentException("Extensão não permitida para documento de verificação.", nameof(normalizedExtension));

        if (!content.CanSeek)
            throw new ArgumentException("Stream deve suportar seek para upload.");

        content.Position = 0;

        var storageKey = $"verif/{userId}/{Guid.NewGuid():N}{normalizedExtension}";

        await _client
            .PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = storageKey,
                    InputStream = content,
                    ContentType = contentType,
                    AutoCloseStream = false,
                    AutoResetStreamPosition = false
                },
                cancellationToken)
            .ConfigureAwait(false);

        return storageKey;
    }

    /// <summary>
    /// Abre o stream do documento para leitura via API.
    /// Nunca constrói URL pública — a chamadora é responsável por fazer o proxy para o cliente.
    /// </summary>
    public async Task<VerificationDocumentReadResult?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidStorageKey(storageKey))
            return null;

        var extension = Path.GetExtension(storageKey).ToLowerInvariant();
        if (!ContentTypeByExtension.TryGetValue(extension, out var fallbackContentType))
            return null;

        try
        {
            var resp = await _client
                .GetObjectAsync(
                    new GetObjectRequest { BucketName = _bucket, Key = storageKey },
                    cancellationToken)
                .ConfigureAwait(false);

            var ct = string.IsNullOrWhiteSpace(resp.Headers.ContentType)
                ? fallbackContentType
                : resp.Headers.ContentType;

            return new VerificationDocumentReadResult(resp.ResponseStream, ct, resp.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Remove o documento do bucket. Silencioso se o objeto já não existir.
    /// Chamado pelo <c>AdminVerificationService</c> após aprovação ou recusa.
    /// </summary>
    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!IsValidStorageKey(storageKey))
            return;

        try
        {
            await _client
                .DeleteObjectAsync(
                    new DeleteObjectRequest { BucketName = _bucket, Key = storageKey },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception)
        {
            // Não bloquear o fluxo de decisão caso o arquivo já não exista.
        }
    }

    /// <summary>
    /// Valida se a <paramref name="storageKey"/> tem o formato canônico
    /// <c>verif/{userId}/{hex32}.{ext}</c> esperado para documentos de identidade.
    /// </summary>
    public bool IsValidStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return false;

        if (storageKey.Contains('\\')
            || storageKey.Contains("..", StringComparison.Ordinal)
            || storageKey.StartsWith("/", StringComparison.Ordinal))
            return false;

        var parts = storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        if (!string.Equals(parts[0], "verif", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!int.TryParse(parts[1], out var userId) || userId <= 0)
            return false;

        var name = Path.GetFileNameWithoutExtension(parts[2]);
        var ext  = Path.GetExtension(parts[2]);

        if (name.Length != 32 || !name.All(Uri.IsHexDigit))
            return false;

        return ContentTypeByExtension.ContainsKey(ext);
    }

    private static void EnsureConfigured(VerificationR2StorageOptions opts)
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(opts.ServiceUrl?.Trim());
        var hasAccount  = !string.IsNullOrWhiteSpace(opts.AccountId?.Trim());

        if (!hasEndpoint && !hasAccount)
            throw new InvalidOperationException(
                "VerificationStorage S3: defina VerificationR2__AccountId ou VerificationR2__ServiceUrl.");

        if (string.IsNullOrWhiteSpace(opts.AccessKeyId) || string.IsNullOrWhiteSpace(opts.SecretAccessKey))
            throw new InvalidOperationException(
                "VerificationStorage S3: VerificationR2__AccessKeyId e VerificationR2__SecretAccessKey são obrigatórios.");

        if (string.IsNullOrWhiteSpace(opts.Bucket))
            throw new InvalidOperationException(
                "VerificationStorage S3: VerificationR2__Bucket é obrigatório.");
    }
}
