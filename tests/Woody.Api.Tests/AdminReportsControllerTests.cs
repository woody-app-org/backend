using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Woody.Api.Controllers;
using Woody.Application.DTOs.Admin;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities.Enum;

namespace Woody.Api.Tests;

/// <summary>
/// Testes unitários do AdminReportsController.
/// Cobre: controle de acesso, listagem, filtros, detalhe, atualização de status e regressão.
/// </summary>
public class AdminReportsControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AdminReportsController CreateController(
        Mock<IAdminReportsService> service,
        int? userId = null,
        string role = "SuperAdmin")
    {
        var controller = new AdminReportsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Role, role)
            };
            controller.ControllerContext.HttpContext.User =
                new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        return controller;
    }

    private static PaginatedResponseDto<AdminReportListItemDto> EmptyPage() => new()
    {
        Items          = [],
        Page           = 1,
        PageSize       = 20,
        TotalCount     = 0,
        HasNextPage    = false,
        HasPreviousPage = false
    };

    private static AdminReportListItemDto SampleListItem(int id = 1) => new()
    {
        Id           = id,
        TargetType   = "post",
        ReasonCode   = "spam",
        Status       = "Pending",
        ReporterUser = new UserPublicDto { Id = "10", Username = "reporter", Name = "Reporter" },
        ReportedContentAuthor = new UserPublicDto { Id = "20", Username = "author", Name = "Author" },
        TargetPreview = new AdminReportTargetPreviewDto { PostId = 5, ContentSnippet = "conteúdo do post" },
        SameTargetReportCount = 1,
        CreatedAt    = DateTime.UtcNow
    };

    private static AdminReportDetailDto SampleDetail(int id = 1) => new()
    {
        Id           = id,
        TargetType   = "post",
        ReasonCode   = "spam",
        Status       = "Pending",
        ReporterUser = new UserPublicDto { Id = "10", Username = "reporter", Name = "Reporter" },
        Post         = new AdminReportPostDetailDto { Id = 5, PublicId = "pst_abc001", Content = "test", CreatedAt = DateTime.UtcNow },
        SameTargetReportCount = 1,
        CreatedAt    = DateTime.UtcNow
    };

    private static AdminReportDetailDto DetailWith(
        int id = 1,
        string status = "Pending",
        DateTime? reviewedAt = null,
        string? internalNote = null,
        string? resolutionCode = null)
    {
        var d = SampleDetail(id);
        d.Status = status;
        d.ReviewedAt = reviewedAt;
        d.InternalNote = internalNote;
        d.ResolutionCode = resolutionCode;
        return d;
    }

    // ── Acesso — usuário comum não acessa ─────────────────────────────────────
    // Nota: a policy SuperAdminOnly é aplicada via ASP.NET middleware, não dentro
    // do método do controller. Em testes unitários sem pipeline completo, o
    // [Authorize] não bloqueia. Mas o controller confia no DI/middleware para isso.
    // Testamos aqui o comportamento interno quando userId ausente ou rol correto.

    [Fact]
    public async Task List_WithoutUserId_ReturnsOk_WhenServiceSucceeds()
    {
        // A proteção de acesso (SuperAdminOnly) é responsabilidade do middleware ASP.NET,
        // não do método do controller. O método retorna Ok quando o serviço retorna dados.
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage());

        var controller = CreateController(service, userId: null);

        var result = await controller.List(null, null, null, null, null, null, 1, 20);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // ── Lista — retorna paginado ──────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsPaginatedResult()
    {
        var page = new PaginatedResponseDto<AdminReportListItemDto>
        {
            Items          = [SampleListItem(1), SampleListItem(2)],
            Page           = 1,
            PageSize       = 20,
            TotalCount     = 2,
            HasNextPage    = false,
            HasPreviousPage = false
        };

        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var controller = CreateController(service, userId: 1);
        var result = await controller.List(null, null, null, null, null, null, 1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PaginatedResponseDto<AdminReportListItemDto>>(ok.Value);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(2, dto.TotalCount);
    }

    // ── Lista — filtro por status ─────────────────────────────────────────────

    [Fact]
    public async Task List_WithValidStatus_PassesFilterToService()
    {
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(ReportStatus.InReview, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage());

        var controller = CreateController(service, userId: 1);
        var result = await controller.List("InReview", null, null, null, null, null, 1, 20);

        Assert.IsType<OkObjectResult>(result.Result);
        service.Verify(s => s.ListAsync(
            ReportStatus.InReview, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task List_WithInvalidStatus_ReturnsBadRequest()
    {
        var service = new Mock<IAdminReportsService>();
        var controller = CreateController(service, userId: 1);

        var result = await controller.List("StatusInexistente", null, null, null, null, null, 1, 20);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── Lista — filtro por targetType ─────────────────────────────────────────

    [Fact]
    public async Task List_WithTargetType_PassesFilterToService()
    {
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(null, "post", null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage());

        var controller = CreateController(service, userId: 1);
        var result = await controller.List(null, "post", null, null, null, null, 1, 20);

        Assert.IsType<OkObjectResult>(result.Result);
        service.Verify(s => s.ListAsync(
            null, "post", null, null, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Lista — filtro por reasonCode ─────────────────────────────────────────

    [Fact]
    public async Task List_WithReasonCode_PassesFilterToService()
    {
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(null, null, "spam", null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage());

        var controller = CreateController(service, userId: 1);
        var result = await controller.List(null, null, "spam", null, null, null, 1, 20);

        Assert.IsType<OkObjectResult>(result.Result);
        service.Verify(s => s.ListAsync(
            null, null, "spam", null, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Lista — search por denunciante/autora ─────────────────────────────────

    [Fact]
    public async Task List_WithSearch_PassesSearchToService()
    {
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(null, null, null, "joana", null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPage());

        var controller = CreateController(service, userId: 1);
        var result = await controller.List(null, null, null, "joana", null, null, 1, 20);

        Assert.IsType<OkObjectResult>(result.Result);
        service.Verify(s => s.ListAsync(
            null, null, null, "joana", null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Lista — ordenação (testada via mock do serviço) ───────────────────────

    [Fact]
    public async Task List_PendingFirstOrdering_IsRespectedByService()
    {
        // O controller apenas repassa os parâmetros; a ordenação é responsabilidade
        // do repositório. Verificamos que o controller não sobrepõe a ordem retornada.
        var item1 = SampleListItem(1);
        item1.Status = "Pending";
        var item2 = SampleListItem(2);
        item2.Status = "Resolved";
        var items = new List<AdminReportListItemDto> { item1, item2 };
        var page = EmptyPage();
        page.Items = items;
        page.TotalCount = 2;

        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.ListAsync(null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var controller = CreateController(service, userId: 1);
        var result = await controller.List(null, null, null, null, null, null, 1, 20);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PaginatedResponseDto<AdminReportListItemDto>>(ok.Value);
        Assert.Equal("Pending", dto.Items[0].Status);
        Assert.Equal("Resolved", dto.Items[1].Status);
    }

    // ── Detalhe — post ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetail_ReturnsDetailDto_ForPostReport()
    {
        var detail = SampleDetail(1);
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var controller = CreateController(service, userId: 1);
        var result = await controller.GetDetail(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.Equal(1, dto.Id);
        Assert.Equal("post", dto.TargetType);
        Assert.NotNull(dto.Post);
    }

    // ── Detalhe — comentário ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDetail_ReturnsDetailDto_ForCommentReport()
    {
        var detail = new AdminReportDetailDto
        {
            Id         = 2,
            TargetType = "comment",
            ReasonCode = "hate_speech",
            Status     = "Pending",
            ReporterUser = new UserPublicDto { Id = "10", Username = "reporter", Name = "Reporter" },
            Comment    = new AdminReportCommentDetailDto { Id = 7, Content = "comentário ruim", CreatedAt = DateTime.UtcNow },
            SameTargetReportCount = 1,
            CreatedAt  = DateTime.UtcNow
        };

        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.GetDetailAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var controller = CreateController(service, userId: 1);
        var result = await controller.GetDetail(2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.Equal("comment", dto.TargetType);
        Assert.NotNull(dto.Comment);
        Assert.Null(dto.Post);
    }

    // ── Detalhe — 404 ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetail_ReturnsNotFound_WhenReportDoesNotExist()
    {
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.GetDetailAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminReportDetailDto?)null);

        var controller = CreateController(service, userId: 1);
        var result = await controller.GetDetail(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Detalhe — sem CPF/email ───────────────────────────────────────────────

    [Fact]
    public async Task GetDetail_DoesNotExposeEmailOrCpf()
    {
        var detail = SampleDetail(1);
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var controller = CreateController(service, userId: 1);
        var result = await controller.GetDetail(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);

        // UserPublicDto não possui campo Email nem Cpf
        var reporterType = dto.ReporterUser.GetType();
        Assert.Null(reporterType.GetProperty("Email"));
        Assert.Null(reporterType.GetProperty("Cpf"));
    }

    // ── Status — InReview ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ToInReview_ReturnsUpdatedDetail()
    {
        var updated = DetailWith(1, "InReview", reviewedAt: DateTime.UtcNow);
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(1, ReportStatus.InReview, 99, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = CreateController(service, userId: 99);
        var body = new AdminReportStatusUpdateRequest { Status = "InReview" };
        var result = await controller.UpdateStatus(1, body);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.Equal("InReview", dto.Status);
        Assert.NotNull(dto.ReviewedAt);
    }

    // ── Status — Resolved ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ToResolved_CallsServiceWithCorrectArgs()
    {
        var updated = DetailWith(1, "Resolved", internalNote: "Resolvido", resolutionCode: "removed_content");
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(1, ReportStatus.Resolved, 99, "Resolvido", "removed_content", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = CreateController(service, userId: 99);
        var body = new AdminReportStatusUpdateRequest
        {
            Status         = "Resolved",
            InternalNote   = "Resolvido",
            ResolutionCode = "removed_content"
        };
        var result = await controller.UpdateStatus(1, body);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.Equal("Resolved", dto.Status);
        Assert.Equal("Resolvido", dto.InternalNote);
        Assert.Equal("removed_content", dto.ResolutionCode);
    }

    // ── Status — Rejected ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ToRejected_ReturnsUpdatedDetail()
    {
        var updated = DetailWith(1, "Rejected");
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(1, ReportStatus.Rejected, 99, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = CreateController(service, userId: 99);
        var body = new AdminReportStatusUpdateRequest { Status = "Rejected" };
        var result = await controller.UpdateStatus(1, body);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.Equal("Rejected", dto.Status);
    }

    // ── Status — ReviewedByUserId é preenchido ────────────────────────────────

    [Fact]
    public async Task UpdateStatus_PassesReviewerUserIdFromClaims()
    {
        const int reviewerId = 42;
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(1, ReportStatus.InReview, reviewerId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailWith(1, "InReview"));

        var controller = CreateController(service, userId: reviewerId);
        var body = new AdminReportStatusUpdateRequest { Status = "InReview" };
        await controller.UpdateStatus(1, body);

        service.Verify(s => s.UpdateStatusAsync(
            1, ReportStatus.InReview, reviewerId, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Status — ReviewedAt é preenchido (verificado pelo serviço) ────────────

    [Fact]
    public async Task UpdateStatus_DetailContainsReviewedAt()
    {
        var reviewedAt = DateTime.UtcNow;
        var updated = DetailWith(1, "InReview", reviewedAt: reviewedAt);
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(1, ReportStatus.InReview, 1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = CreateController(service, userId: 1);
        var body = new AdminReportStatusUpdateRequest { Status = "InReview" };
        var result = await controller.UpdateStatus(1, body);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.NotNull(dto.ReviewedAt);
    }

    // ── Status — InternalNote é salva ─────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_InternalNoteIsSavedInResponse()
    {
        var note = "Conteúdo verificado, ação tomada.";
        var updated = DetailWith(1, "Resolved", internalNote: note);
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(1, ReportStatus.Resolved, 1, note, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = CreateController(service, userId: 1);
        var body = new AdminReportStatusUpdateRequest { Status = "Resolved", InternalNote = note };
        var result = await controller.UpdateStatus(1, body);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminReportDetailDto>(ok.Value);
        Assert.Equal(note, dto.InternalNote);
    }

    // ── Status — status inválido retorna 400 ─────────────────────────────────

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var service = new Mock<IAdminReportsService>();
        var controller = CreateController(service, userId: 1);
        var body = new AdminReportStatusUpdateRequest { Status = "StatusBogus" };

        var result = await controller.UpdateStatus(1, body);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        service.Verify(s => s.UpdateStatusAsync(
            It.IsAny<int>(), It.IsAny<ReportStatus>(), It.IsAny<int>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Status — 404 quando denúncia não existe ───────────────────────────────

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenReportDoesNotExist()
    {
        var service = new Mock<IAdminReportsService>();
        service
            .Setup(s => s.UpdateStatusAsync(999, It.IsAny<ReportStatus>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Denúncia 999 não encontrada."));

        var controller = CreateController(service, userId: 1);
        var body = new AdminReportStatusUpdateRequest { Status = "InReview" };

        var result = await controller.UpdateStatus(999, body);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Status — sem userId no claim → Unauthorized ───────────────────────────

    [Fact]
    public async Task UpdateStatus_WithoutUserId_ReturnsUnauthorized()
    {
        var service = new Mock<IAdminReportsService>();
        var controller = CreateController(service, userId: null);
        var body = new AdminReportStatusUpdateRequest { Status = "InReview" };

        var result = await controller.UpdateStatus(1, body);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}
