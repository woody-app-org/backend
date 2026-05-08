using Microsoft.AspNetCore.Authorization;
using Woody.Api.Extensions;
using Woody.Application.Interfaces;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Authorization;

/// <summary>
/// Handler que verifica o status de aprovação da conta consultando o banco de dados,
/// nunca confiando apenas na claim do JWT (que pode estar desatualizada após aprovação/recusa).
/// Registrado como Scoped para ter acesso ao IUserRepository por request.
/// </summary>
public sealed class VerifiedAccountAuthorizationHandler
    : AuthorizationHandler<VerifiedAccountRequirement>
{
    private readonly IUserRepository _users;
    private readonly ILogger<VerifiedAccountAuthorizationHandler> _logger;

    public VerifiedAccountAuthorizationHandler(
        IUserRepository users,
        ILogger<VerifiedAccountAuthorizationHandler> logger)
    {
        _users = users;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedAccountRequirement requirement)
    {
        // SuperAdmin bypassa — garante acesso ao painel admin independente de status de verificação
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.GetUserId();
        if (userId == null)
        {
            // Não autenticado — deixa o framework lidar com 401
            return;
        }

        var user = await _users.GetByIdNoTrackingAsync(userId.Value, CancellationToken.None);

        if (user == null)
        {
            _logger.LogWarning(
                "VerifiedAccount: usuário {UserId} autenticado mas não encontrado no banco.", userId);
            context.Fail(new AuthorizationFailureReason(this, "ACCOUNT_PENDING_VERIFICATION"));
            return;
        }

        if (user.VerificationStatus == VerificationStatus.Approved)
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogDebug(
            "VerifiedAccount: acesso negado para usuário {UserId} com status {Status}.",
            userId, user.VerificationStatus);

        // Falha com razão identificável — o VerifiedAccountResultHandler usa para montar o 403
        context.Fail(new AuthorizationFailureReason(this, "ACCOUNT_PENDING_VERIFICATION"));
    }
}
