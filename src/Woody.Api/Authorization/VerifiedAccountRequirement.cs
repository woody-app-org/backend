using Microsoft.AspNetCore.Authorization;

namespace Woody.Api.Authorization;

/// <summary>
/// Exige que a conta da usuária esteja com VerificationStatus == Approved.
/// O handler sempre consulta o banco — não depende da claim do JWT, evitando token stale.
/// SuperAdmin tem bypass automático.
/// </summary>
public sealed class VerifiedAccountRequirement : IAuthorizationRequirement { }
