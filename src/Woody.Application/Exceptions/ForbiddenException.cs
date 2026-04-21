namespace Woody.Application.Exceptions;

/// <summary>Utilizada quando a utilizadora está autenticada mas não tem permissão para a ação (HTTP 403).</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
