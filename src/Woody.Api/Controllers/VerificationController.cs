using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

/// <summary>
/// Endpoints de verificação de identidade da própria usuária.
/// Acesso restrito: apenas a própria usuária autenticada.
/// Documentos NÃO são retornados aqui — apenas SuperAdmin pode acessar via endpoint de admin (etapa futura).
/// </summary>
[ApiController]
[Route("api/verification")]
[Authorize]
public class VerificationController : ControllerBase
{
    // Slack para overhead do multipart (headers, boundaries)
    private const long MultipartSlackBytes = 512 * 1024;

    private readonly IVerificationService _verificationService;
    private readonly VerificationStorageOptions _storageOptions;

    public VerificationController(
        IVerificationService verificationService,
        IOptions<VerificationStorageOptions> storageOptions)
    {
        _verificationService = verificationService;
        _storageOptions = storageOptions.Value;
    }

    /// <summary>
    /// Envia o documento de identidade (frente do RG).
    /// Permitido apenas quando status = PendingDocument ou Rejected.
    /// O arquivo é salvo em storage privado e nunca exposto publicamente.
    /// </summary>
    [HttpPost("document")]
    [EnableRateLimiting(RateLimitPolicyNames.Upload)]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit] // Limite aplicado dinamicamente via VerificationStorageOptions
    public async Task<IActionResult> SubmitDocument(
        [FromForm] VerificationDocumentUploadForm? form,
        CancellationToken cancellationToken)
    {
        // Aplicar limite de tamanho da request alinhado à configuração do serviço
        var effectiveLimit = _storageOptions.MaxUploadBytes + MultipartSlackBytes;
        if (HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } sizeFeature)
            sizeFeature.MaxRequestBodySize = effectiveLimit;

        if (form?.File == null)
            return BadRequest(new { error = "Arquivo obrigatório." });

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!form.ConsentGiven)
            return BadRequest(new { error = "É necessário confirmar o consentimento para o tratamento dos dados." });

        try
        {
            await using var stream = form.File.OpenReadStream();
            var result = await _verificationService.SubmitDocumentAsync(
                userId.Value,
                stream,
                form.File.FileName,
                form.File.ContentType,
                form.File.Length,
                form.ConsentGiven,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retorna o status de verificação da própria usuária.
    /// Nunca retorna storageKey, URL do documento ou dados sensíveis do documento.
    /// </summary>
    [HttpGet("status")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<VerificationStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var status = await _verificationService.GetStatusAsync(userId.Value, cancellationToken);
        if (status == null)
            return NotFound(new { error = "Registro de verificação não encontrado." });

        return Ok(status);
    }

    /// <summary>
    /// Remove o documento enviado e volta o status para PendingDocument.
    /// Permitido apenas quando status = PendingReview ou Rejected.
    /// Útil para reenvio ou cancelamento.
    /// </summary>
    [HttpDelete("document")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
    public async Task<ActionResult<VerificationStatusDto>> DeleteDocument(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var result = await _verificationService.DeleteDocumentAsync(userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}

/// <summary>Form model para upload de documento de verificação.</summary>
public sealed class VerificationDocumentUploadForm
{
    public IFormFile? File { get; set; }

    /// <summary>
    /// A usuária confirma ciência do tratamento dos dados conforme informado na tela.
    /// Deve ser <c>true</c> para prosseguir.
    /// </summary>
    public bool ConsentGiven { get; set; }
}
