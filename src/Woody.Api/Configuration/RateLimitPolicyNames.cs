namespace Woody.Api.Configuration;

public static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthRegister = "auth-register";
    /// <summary>Envio e reenvio de código por e-mail (chave principal: e-mail normalizado).</summary>
    public const string AuthEmailSend = "auth-email-send";

    /// <summary>Confirmação do código (tentativas por e-mail, separado do envio).</summary>
    public const string AuthEmailVerify = "auth-email-verify";
    public const string AuthRefresh = "auth-refresh";
    public const string Upload = "upload";
    public const string ContentCreate = "content-create";
    public const string ContentComment = "content-comment";
    public const string ReportCreate = "report-create";
    public const string PublicApi = "public-api";
    public const string PublicRead = "public-read";
    public const string AuthenticatedApi = "authenticated-api";
    public const string StripeWebhook = "stripe-webhook";
    public const string BetaInviteValidate = "beta-invite-validate";
}
