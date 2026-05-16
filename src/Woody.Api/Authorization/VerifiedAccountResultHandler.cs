using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Woody.Api.Authorization;

/// <summary>
/// Intercepta resultados de autorização para transformar falhas de VerifiedAccount
/// em 403 com corpo JSON consistente, em vez do 403 vazio padrão.
/// Todos os outros casos delegam para o handler padrão do ASP.NET Core.
/// </summary>
public sealed class VerifiedAccountResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly AuthorizationMiddlewareResultHandler Default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var isVerificationBlock = authorizeResult.AuthorizationFailure?.FailureReasons
                .Any(r => r.Message == "ACCOUNT_PENDING_VERIFICATION") == true;

            if (isVerificationBlock)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "ACCOUNT_PENDING_VERIFICATION",
                    message = "Conta em revisão. Aguarde a aprovação para acessar a plataforma."
                });
                return;
            }
        }

        await Default.HandleAsync(next, context, policy, authorizeResult);
    }
}
