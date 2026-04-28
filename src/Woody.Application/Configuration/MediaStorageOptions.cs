using Woody.Domain.Media;

namespace Woody.Application.Configuration;

public class MediaStorageOptions
{
    public string RootPath { get; set; } = "App_Data/media";
    public string PublicUrlPath { get; set; } = "/api/media/images";
    public long MaxImageSizeBytes { get; set; } = UploadedImagePolicy.DefaultMaxSizeBytes;
}
