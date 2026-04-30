using Woody.Domain.Media;

namespace Woody.Application.Configuration;

public class MediaStorageOptions
{
    public string RootPath { get; set; } = "App_Data/media";
    public string PublicUrlPath { get; set; } = "/api/media/images";
    /// <summary>Prefixo público para vídeos servidos pela API (R2/S3 pode substituir só a origem da URL).</summary>
    public string PublicVideoUrlPath { get; set; } = "/api/media/videos";
    public long MaxImageSizeBytes { get; set; } = UploadedImagePolicy.DefaultMaxSizeBytes;
    public long MaxVideoSizeBytes { get; set; } = UploadedVideoPolicy.DefaultMaxSizeBytes;
}
