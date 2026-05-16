using Woody.Domain.Media;

namespace Woody.Application.Configuration;

public class MediaStorageOptions
{
    /// <summary><c>Local</c> (disco) ou <c>S3</c> (R2/S3-compatible).</summary>
    public string Driver { get; set; } = "Local";

    public string RootPath { get; set; } = "App_Data/media";
    public string PublicUrlPath { get; set; } = "/api/media/images";
    /// <summary>Prefixo público para vídeos servidos pela API (R2/S3 pode substituir só a origem da URL).</summary>
    public string PublicVideoUrlPath { get; set; } = "/api/media/videos";

    public long MaxImageSizeBytes { get; set; } = MediaReferenceConstraints.ImageMaxUploadBytes;

    public long MaxPostVideoUploadBytes { get; set; } = MediaReferenceConstraints.PostVideoMaxUploadBytes;

    public long MaxMessageVideoUploadBytes { get; set; } = MediaReferenceConstraints.MessageVideoMaxUploadBytes;
}
