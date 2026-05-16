namespace Woody.Domain.Media;

/// <summary>Limites de referência e metadados declarados para vídeo em posts vs mensagens.</summary>
public static class MediaReferenceConstraints
{
    public const int PostVideoMaxDeclaredSeconds = 120;
    public const int MessageVideoMaxDeclaredSeconds = 30;

    public const long PostVideoMaxUploadBytes = 100L * 1024 * 1024;
    public const long MessageVideoMaxUploadBytes = 50L * 1024 * 1024;

    public const long ImageMaxUploadBytes = 10L * 1024 * 1024;
}
