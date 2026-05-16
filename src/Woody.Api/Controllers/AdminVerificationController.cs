using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Controllers;

/// <summary>
/// Dashboard de verificação de identidade — exclusivo para SuperAdmin.
/// Nenhum endpoint deste controller expõe DocumentStorageKey ou URLs públicas de documentos.
/// </summary>
[ApiController]
[Route("api/admin/verification")]
[Authorize(Policy = "SuperAdminOnly")]
public class AdminVerificationController : ControllerBase
{
    private readonly IAdminVerificationService _service;

    public AdminVerificationController(IAdminVerificationService service)
    {
        _service = service;
    }

    // ── GET /api/admin/verification ───────────────────────────────────────────

    /// <summary>Lista solicitações de verificação com filtros e paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<AdminVerificationListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponseDto<AdminVerificationListItemDto>>> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        VerificationStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<VerificationStatus>(status, ignoreCase: true, out var parsed))
                return BadRequest(new { error = $"Status inválido: '{status}'. Valores aceitos: PendingDocument, PendingReview, Approved, Rejected." });

            statusFilter = parsed;
        }

        var result = await _service.ListAsync(
            statusFilter, dateFrom, dateTo, page, pageSize, cancellationToken);

        return Ok(result);
    }

    // ── GET /api/admin/verification/{id} ─────────────────────────────────────

    /// <summary>Detalhe de uma solicitação específica.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminVerificationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminVerificationDetailDto>> GetDetail(
        int id,
        CancellationToken cancellationToken = default)
    {
        var detail = await _service.GetDetailAsync(id, cancellationToken);
        return detail == null ? NotFound() : Ok(detail);
    }

    // ── GET /api/admin/verification/{id}/document ─────────────────────────────

    /// <summary>
    /// Proxy protegido para visualização do documento de identidade.
    /// Nunca expõe o storageKey. Sem cache público. Requer SuperAdmin.
    /// </summary>
    [HttpGet("{id:int}/document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.OpenDocumentStreamAsync(id, cancellationToken);

        if (result == null)
            return NotFound(new { error = "Documento não encontrado ou já removido." });

        Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // inline para imagens — o admin visualiza diretamente; download se não for imagem conhecida
        var disposition = result.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? "inline"
            : "attachment";

        var extension = result.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png"  => ".png",
            _            => ".bin"
        };

        Response.Headers.Append(
            "Content-Disposition",
            $"{disposition}; filename=\"document{extension}\"");

        return File(result.Content, result.ContentType);
    }

    // ── POST /api/admin/verification/{id}/approve ─────────────────────────────

    /// <summary>Aprova uma solicitação PendingReview e descarta o documento.</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(AdminVerificationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminVerificationDetailDto>> Approve(
        int id,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = User.GetUserId();
        if (reviewerId == null)
            return Unauthorized();

        try
        {
            var result = await _service.ApproveAsync(id, reviewerId.Value, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // ── POST /api/admin/verification/{id}/reject ──────────────────────────────

    /// <summary>Recusa uma solicitação PendingReview com motivo obrigatório e descarta o documento.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(AdminVerificationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminVerificationDetailDto>> Reject(
        int id,
        [FromBody] AdminVerificationRejectRequest body,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var reviewerId = User.GetUserId();
        if (reviewerId == null)
            return Unauthorized();

        try
        {
            var result = await _service.RejectAsync(
                id, reviewerId.Value, body.RejectionReason, cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
}
