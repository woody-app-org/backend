namespace Woody.Application.UseCases.Auth.Register;

public sealed class RegistrationConflictException : InvalidOperationException
{
    public RegistrationConflictException(string message, string field)
        : base(message)
    {
        Field = field;
    }

    /// <summary>Campo em conflito: <c>username</c>, <c>email</c> ou <c>cpf</c>.</summary>
    public string Field { get; }
}
