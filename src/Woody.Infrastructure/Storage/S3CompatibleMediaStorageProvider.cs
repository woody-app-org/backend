using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Media;
using Woody.Domain.Media;

namespace Woody.Infrastructure.Storage;

/// <summary>Armazenamento S3-compatible (Cloudflare R2, MinIO, etc.).</summary>
public sealed class S3CompatibleMediaStorageProvider : IMediaStorageProvider, IDisposable
{
    private readonly MediaStorageOptions _media;
    private readonly string _bucket;
    private readonly AmazonS3Client _client;
    private readonly R2MediaStorageOptions _r2;

    public S3CompatibleMediaStorageProvider(
        IOptions<MediaStorageOptions> media,
        IOptions<R2MediaStorageOptions> r2)
    {
        _media = media.Value;
        _r2 = r2.Value;
        EnsureConfigured(_r2);
        _bucket = _r2.Bucket.Trim();

        var endpoint = string.IsNullOrWhiteSpace(_r2.ServiceUrl?.Trim())
            ? $"https://{_r2.AccountId.Trim()}.r2.cloudflarestorage.com"
            : _r2.ServiceUrl!.Trim();

        var cfg = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = string.IsNullOrWhiteSpace(_r2.Region) ? "auto" : _r2.Region.Trim()
        };

        _client = new AmazonS3Client(
            new BasicAWSCredentials(_r2.AccessKeyId.Trim(), _r2.SecretAccessKey.Trim()),
            cfg);
    }

    public void Dispose() => _client.Dispose();

    public async Task<StoredMediaFile> PutImageAsync(
        MediaStorageWriteContext context,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (UploadedImagePolicy.GetContentTypeForStorageKey($"file{normalizedExtension}") != contentType)
            throw new ArgumentException("Tipo de arquivo inválido.");

        return await PutBlobAsync(context, content, normalizedExtension, contentType, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<StoredMediaFile> PutVideoAsync(
        MediaStorageWriteContext context,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (UploadedVideoPolicy.GetContentTypeForStorageKey($"file{normalizedExtension}") != contentType)
            throw new ArgumentException("Tipo de arquivo inválido.");

        return await PutBlobAsync(context, content, normalizedExtension, contentType, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> TryDeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!MediaStorageKeySyntax.IsSafeServerMediaStorageKey(storageKey))
            return false;

        try
        {
            await _client
                .DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = storageKey }, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
    }

    public async Task<MediaReadResult?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!MediaStorageKeySyntax.IsSafeServerMediaStorageKey(storageKey))
            return null;

        var fallbackContentType = AttachmentStorageCatalog.GetContentTypeForStorageKey(storageKey);
        if (fallbackContentType == null)
            return null;

        try
        {
            var resp = await _client
                .GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = storageKey }, cancellationToken)
                .ConfigureAwait(false);
            var ct = string.IsNullOrWhiteSpace(resp.Headers.ContentType)
                ? fallbackContentType
                : resp.Headers.ContentType;
            return new MediaReadResult(resp.ResponseStream, ct, resp.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public string BuildPublicImageUrl(string storageKey)
    {
        if (!string.IsNullOrWhiteSpace(_r2.PublicBaseUrl?.Trim()))
            return $"{_r2.PublicBaseUrl.TrimEnd('/')}/{MediaStorageUrlEncoding.EncodeKeyForUrlPath(storageKey)}";

        var basePath = string.IsNullOrWhiteSpace(_media.PublicUrlPath)
            ? "/api/media/images"
            : _media.PublicUrlPath.TrimEnd('/');
        return $"{basePath}/{MediaStorageUrlEncoding.EncodeKeyForUrlPath(storageKey)}";
    }

    public string BuildPublicVideoUrl(string storageKey)
    {
        if (!string.IsNullOrWhiteSpace(_r2.PublicBaseUrl?.Trim()))
            return $"{_r2.PublicBaseUrl.TrimEnd('/')}/{MediaStorageUrlEncoding.EncodeKeyForUrlPath(storageKey)}";

        var basePath = string.IsNullOrWhiteSpace(_media.PublicVideoUrlPath)
            ? "/api/media/videos"
            : _media.PublicVideoUrlPath.TrimEnd('/');
        return $"{basePath}/{MediaStorageUrlEncoding.EncodeKeyForUrlPath(storageKey)}";
    }

    public string? TryCreatePresignedGetUrl(string storageKey, bool isVideo, TimeSpan lifetime)
    {
        if (!MediaStorageKeySyntax.IsSafeServerMediaStorageKey(storageKey) || lifetime <= TimeSpan.Zero)
            return null;

        var expectedVideo = UploadedVideoPolicy.GetContentTypeForStorageKey(storageKey) != null;
        var expectedImage = UploadedImagePolicy.GetContentTypeForStorageKey(storageKey) != null;
        if (isVideo && !expectedVideo)
            return null;
        if (!isVideo && !expectedImage)
            return null;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        };

        return _client.GetPreSignedURL(request);
    }

    private async Task<StoredMediaFile> PutBlobAsync(
        MediaStorageWriteContext context,
        Stream content,
        string normalizedExtension,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
            throw new ArgumentException("Stream deve permitir leitura com tamanho conhecido.");

        var sizeBytes = content.Length;
        content.Position = 0;

        var prefix = context.ObjectKeyPrefix.Trim().Replace('\\', '/');
        if (prefix.Length == 0 || !prefix.EndsWith('/'))
            throw new ArgumentException("Prefixo de object key inválido.");

        var storageKey = $"{prefix}{Guid.NewGuid():N}{normalizedExtension}";

        // Cloudflare R2 não implementa STREAMING-AWS4-HMAC-SHA256-PAYLOAD (upload chunked do SDK).
        // DisablePayloadSigning evita esse modo e mantém SigV4 compatível com R2.
        await _client
            .PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = storageKey,
                    InputStream = content,
                    ContentType = contentType,
                    AutoCloseStream = false,
                    AutoResetStreamPosition = false,
                    DisablePayloadSigning = true
                },
                cancellationToken)
            .ConfigureAwait(false);

        return new StoredMediaFile(storageKey, contentType, sizeBytes);
    }

    private static void EnsureConfigured(R2MediaStorageOptions r)
    {
        var hasEndpoint = !string.IsNullOrWhiteSpace(r.ServiceUrl?.Trim());
        var hasAccount = !string.IsNullOrWhiteSpace(r.AccountId?.Trim());
        if (!hasEndpoint && !hasAccount)
        {
            throw new InvalidOperationException(
                "Armazenamento S3: defina R2:AccountId (ou R2_ACCOUNT_ID) ou R2:ServiceUrl para o endpoint.");
        }

        if (string.IsNullOrWhiteSpace(r.AccessKeyId) || string.IsNullOrWhiteSpace(r.SecretAccessKey))
            throw new InvalidOperationException("Armazenamento S3: AccessKeyId e SecretAccessKey são obrigatórios.");

        if (string.IsNullOrWhiteSpace(r.Bucket))
            throw new InvalidOperationException("Armazenamento S3: Bucket é obrigatório.");
    }

    private static string NormalizeExtension(string extension)
    {
        var e = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        return e;
    }
}
