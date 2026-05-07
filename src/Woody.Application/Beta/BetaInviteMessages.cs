namespace Woody.Application.Beta;

public static class BetaInviteMessages
{
    /// <summary>Mensagem quando beta está ativo e falta o código.</summary>
    public const string RequiredWhenBetaActive = "Convite obrigatório para criar conta neste período.";

    /// <summary>Mensagem genérica para convite inválido no registo (detalhes não expostos).</summary>
    public const string InvalidForRegistration = "Convite inválido ou expirado.";

    /// <summary>Mensagem para o endpoint público de validação (mesmo nível de detalhe para todos os falhanços).</summary>
    public const string PublicInvalid = "Convite inválido ou expirado.";
}
