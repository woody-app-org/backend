using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Controllers;

/// <summary>
/// Painel administrativo de denúncias — exclusivo para SuperAdmin.
/// Permite listar, filtrar, visualizar detalhes e registrar andamento de denúncias.
/// Não expõe CPF, e-mail completo, documentos de identidade ou tokens.
/// </summary>
[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = "SuperAdminOnly")]
public class AdminReportsController : ControllerBase
{
    private readonly IAdminReportsService _service;

    public AdminReportsController(IAdminReportsService service)
    {
        _service = service;
    }

    // ── GET /api/admin/reports ────────────────────────────────────────────────

    /// <summary>Lista denúncias com filtros e paginação. Pendentes aparecem primeiro.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<AdminReportListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponseDto<AdminReportListItemDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? targetType,
        [FromQuery] string? reasonCode,
        [FromQuery] string? search,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ReportStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ReportStatus>(status, ignoreCase: true, out var parsed))
                return BadRequest(new
                {
                    error = $"Status inválido: '{status}'. Valores aceitos: Pending, InReview, Resolved, Rejected."
                });

            statusFilter = parsed;
        }

        var result = await _service.ListAsync(
            statusFilter, targetType, reasonCode, search,
            dateFrom, dateTo, page, pageSize, cancellationToken);

        return Ok(result);
    }

    // ── GET /api/admin/reports/{reportId} ─────────────────────────────────────

    /// <summary>Detalhe completo de uma denúncia, incluindo conteúdo denunciado.</summary>
    [HttpGet("{reportId:int}")]
    [ProducesResponseType(typeof(AdminReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminReportDetailDto>> GetDetail(
        int reportId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _service.GetDetailAsync(reportId, cancellationToken);
        return detail == null ? NotFound() : Ok(detail);
    }

    // ── PATCH /api/admin/reports/{reportId}/status ────────────────────────────

    /// <summary>Registra o andamento da análise: status, nota interna e código de resolução.</summary>
    [HttpPatch("{reportId:int}/status")]
    [ProducesResponseType(typeof(AdminReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminReportDetailDto>> UpdateStatus(
        int reportId,
        [FromBody] AdminReportStatusUpdateRequest body,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Enum.TryParse<ReportStatus>(body.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new
            {
                error = $"Status inválido: '{body.Status}'. Valores aceitos: Pending, InReview, Resolved, Rejected."
            });

        var reviewerId = User.GetUserId();
        if (reviewerId == null)
            return Unauthorized();

        try
        {
            var result = await _service.UpdateStatusAsync(
                reportId, newStatus, reviewerId.Value,
                body.InternalNote, body.ResolutionCode,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
