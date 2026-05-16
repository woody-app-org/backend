namespace Woody.Application.Exceptions;

/// <summary>Utilizadora autenticada mas sem permissão para anexar neste contexto.</summary>
public sealed class MediaUploadForbiddenException : Exception
{
    public MediaUploadForbiddenException(string message)
        : base(message)
    {
    }
}
