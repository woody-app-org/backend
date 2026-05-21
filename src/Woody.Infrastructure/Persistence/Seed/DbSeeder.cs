using Microsoft.EntityFrameworkCore;
using Woody.Application.Beta;
using Woody.Application.Billing;
using Woody.Application.Posts;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;
using VerificationStatus = Woody.Domain.Entities.Enum.VerificationStatus;

namespace Woody.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados de desenvolvimento. Idempotente por utilizador (username e email), comunidade (slug) e contagens mínimas.
/// Para repovoar do zero: apagar a base ou <c>dotnet ef database drop</c> antes de migrar.
/// </summary>
/// <remarks>
/// QA premium comunidade: slug <c>mulheres-tech</c> — <see cref="SeedCommunityPremiumDemo"/> + admin como staff;
/// impulsionamento de exemplo: <see cref="SeedCommunityPostBoostDemo"/>; painel analytics denso: <see cref="SeedMulheresTechDashboardDemo"/>.
/// </remarks>
public static class DbSeeder
{
    private const string DevPasswordSuffix = "Woody2026!";

    public static void Seed(WoodyDbContext context)
    {
        SeedUsers(context);
        EnsureSuperAdmin(context);
        SeedBetaInvites(context);
        EnsureUserSubscriptions(context);
        SeedProDemoSubscription(context);
        EnsureIdentityVerifications(context);
        SeedCommunitiesAndMemberships(context);
        EnsureCommunitySubscriptions(context);
        SeedCommunityPremiumDemo(context);
        EnsureAdminStaffOnPremiumDemoCommunity(context);
        SeedPosts(context);
        SeedAdminGalleryDemoPosts(context);
        SeedCommunityPostBoostDemo(context);
        SeedComments(context);
        SeedFollows(context);
        SeedLikes(context);
        SeedMulheresTechDashboardDemo(context);
        SeedUserInterestsAndSocialLinks(context);
        SeedJoinRequests(context);
        SeedContentReports(context);
        RecalculateCommunityMemberCounts(context);
    }

    private static void SeedUsers(WoodyDbContext context)
    {
        var hasher = new PasswordHasher();
        var now = DateTime.UtcNow;

        var defs = new (string Username, string Email, string Password, string Role, string DisplayName, string? Bio,
            string? Pronouns, string? ProfilePic, string? BannerPic, string? Location)[]
        {
            ("admin", "admin@example.com", "admin123", "Admin", "Admin",
                "Conta de administradora da plataforma.", "ela/dela", Pic(1), PicCover(1), "Lisboa"),
            ("user1", "user1@example.com", "user123", "User", "Beatriz Nascimento",
                "Product designer a gostar de comunidades acolhedoras.", "ela/dela", Pic(2), PicCover(2), "Porto"),
            ("user2", "user2@example.com", "user234", "User", "Camila Ribeiro",
                "Mãe, leitora e curiosa sobre bem-estar.", "ela/dela", Pic(3), PicCover(3), "Braga"),
            ("user3", "user3@example.com", "user345", "User", "Débora Silva",
                "Trabalho em tech e partilho dicas de carreira.", "ela/dela", Pic(4), PicCover(4), "Coimbra"),
            ("user4", "user4@example.com", "user456", "User", "Elena Martins",
                "Viagens em solo e segurança no espaço público.", "ela/dela", Pic(5), PicCover(5), "Faro"),
            ("user5", "user5@example.com", DevPasswordSuffix, "User", "Fernanda Costa",
                "Psicóloga em formação; interessada em saúde mental.", "ela/dela", Pic(6), PicCover(6), "Aveiro"),
            ("user6", "user6@example.com", DevPasswordSuffix, "User", "Gabriela Alves",
                "Escritora amadora e clube do livro.", "ela/dela", Pic(7), PicCover(7), "Évora"),
            ("user7", "user7@example.com", DevPasswordSuffix, "User", "Helena Sousa",
                "Mentora de carreira para mulheres na tech.", "ela/dela", Pic(8), PicCover(8), "Lisboa"),
            ("user8", "user8@example.com", DevPasswordSuffix, "User", "Inês Rodrigues",
                "Activista LGBTQIA+ e espaços seguros.", "ela/dela", Pic(9), PicCover(9), "Porto"),
            ("user9", "user9@example.com", DevPasswordSuffix, "User", "Joana Ferreira",
                "Relacionamentos saudáveis e limites.", "ela/dela", Pic(10), PicCover(10), "Setúbal"),
            ("user10", "user10@example.com", DevPasswordSuffix, "User", "Karina Lopes",
                "Finanças pessoais sem tabu.", "ela/dela", Pic(11), PicCover(11), "Leiria"),
            ("user11", "user11@example.com", DevPasswordSuffix, "User", "Lúcia Pinto",
                "Empreendedora e partilha de recursos.", "ela/dela", Pic(12), PicCover(12), "Viseu"),
            ("user12", "user12@example.com", DevPasswordSuffix, "User", "Mariana Teixeira",
                "Estudante de enfermagem; bem-estar no dia a dia.", "ela/dela", Pic(13), PicCover(13), "Guarda"),
            ("user13", "user13@example.com", DevPasswordSuffix, "User", "Natália Cardoso",
                "Cultura, arte e conversas profundas.", "ela/dela", Pic(14), PicCover(14), "Lisboa"),
            ("user14", "user14@example.com", DevPasswordSuffix, "User", "Olívia Monteiro",
                "Segurança digital para mulheres.", "ela/dela", Pic(15), PicCover(15), "Braga"),
            ("user15", "user15@example.com", DevPasswordSuffix, "User", "Patrícia Gomes",
                "Maternidade e trabalho: descomplicar.", "ela/dela", Pic(16), PicCover(16), "Porto"),
            ("user16", "user16@example.com", DevPasswordSuffix, "User", "Rita Carvalho",
                "Runner e comunidade de desporto ao ar livre.", "ela/dela", Pic(17), PicCover(17), "Cascais"),
            ("user17", "user17@example.com", DevPasswordSuffix, "User", "Sofia Araújo",
                "Voluntariado e causas sociais.", "ela/dela", Pic(18), PicCover(18), "Matosinhos"),
            ("user18", "user18@example.com", DevPasswordSuffix, "User", "Teresa Melo",
                "Receitas, nutrição e corpo positivo.", "ela/dela", Pic(19), PicCover(19), "Sintra"),
            ("user19", "user19@example.com", DevPasswordSuffix, "User", "Vera Baptista",
                "Jogadora e streaming casual.", "ela/dela", Pic(20), PicCover(20), "Amadora"),
            ("user20", "user20@example.com", DevPasswordSuffix, "User", "Yara Nunes",
                "Nova na Woody — a explorar grupos.", "ela/dela", null, null, "Loures")
        };

        var existingUsernames = new HashSet<string>(
            context.Users.Select(u => u.Username),
            StringComparer.Ordinal);
        var existingEmails = new HashSet<string>(
            context.Users.Select(u => u.Email.ToLowerInvariant()));

        foreach (var d in defs)
        {
            var emailLower = d.Email.ToLowerInvariant();
            if (existingUsernames.Contains(d.Username) || existingEmails.Contains(emailLower))
                continue;

            context.Users.Add(new User
            {
                Username = d.Username,
                Email = d.Email,
                Password = hasher.HashPassword(d.Password),
                Role = d.Role,
                DisplayName = d.DisplayName,
                Bio = d.Bio,
                Pronouns = d.Pronouns,
                ProfilePic = d.ProfilePic,
                BannerPic = d.BannerPic,
                Location = d.Location,
                CreatedAt = now,
                UpdatedAt = now
            });
            existingUsernames.Add(d.Username);
            existingEmails.Add(emailLower);
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Cria/atualiza a conta SuperAdmin a partir de variáveis de ambiente.
    /// Variáveis: WOODY_SUPERADMIN_USERNAME, WOODY_SUPERADMIN_EMAIL, WOODY_SUPERADMIN_PASSWORD.
    /// Se as variáveis não estiverem definidas, promove a conta "admin" seed para SuperAdmin (apenas em dev).
    /// </summary>
    private static void EnsureSuperAdmin(WoodyDbContext context)
    {
        var now = DateTime.UtcNow;
        var hasher = new PasswordHasher();

        var username = Environment.GetEnvironmentVariable("WOODY_SUPERADMIN_USERNAME")?.Trim();
        var email = Environment.GetEnvironmentVariable("WOODY_SUPERADMIN_EMAIL")?.Trim();
        var password = Environment.GetEnvironmentVariable("WOODY_SUPERADMIN_PASSWORD")?.Trim();

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
        {
            var existing = context.Users.FirstOrDefault(u => u.Email == email.ToLowerInvariant());
            if (existing == null)
            {
                context.Users.Add(new User
                {
                    Username = username,
                    Email = email.ToLowerInvariant(),
                    Password = hasher.HashPassword(password),
                    Role = "SuperAdmin",
                    DisplayName = username,
                    IsEmailVerified = true,
                    EmailVerifiedAt = now,
                    VerificationStatus = VerificationStatus.Approved,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                context.SaveChanges();
            }
            else if (existing.Role != "SuperAdmin")
            {
                existing.Role = "SuperAdmin";
                existing.VerificationStatus = VerificationStatus.Approved;
                existing.UpdatedAt = now;
                context.SaveChanges();
            }
        }
        else
        {
            // Fallback dev: promover conta "admin" seed para SuperAdmin
            var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
            if (adminUser != null && adminUser.Role != "SuperAdmin")
            {
                adminUser.Role = "SuperAdmin";
                adminUser.VerificationStatus = VerificationStatus.Approved;
                adminUser.UpdatedAt = now;
                context.SaveChanges();
            }
        }
    }

    /// <summary>
    /// Garante que todos os utilizadores seed tenham um registo em <c>identity_verifications</c> com status Approved.
    /// Também garante que novos utilizadores seed (adicionados futuramente) sejam cobertos.
    /// Utilizadores reais criados via registo nascem com PendingDocument — este método NÃO os altera.
    /// </summary>
    private static void EnsureIdentityVerifications(WoodyDbContext context)
    {
        var now = DateTime.UtcNow;

        // Atualiza o campo desnormalizado de todos os users seed para Approved
        var seedUsers = context.Users
            .Where(u => u.VerificationStatus != VerificationStatus.Approved
                        && u.VerificationStatus != VerificationStatus.Rejected)
            .ToList();

        foreach (var u in seedUsers)
        {
            u.VerificationStatus = VerificationStatus.Approved;
            u.UpdatedAt = now;
        }

        if (context.ChangeTracker.HasChanges())
            context.SaveChanges();

        // Cria registros de IdentityVerification ausentes para todos os users (idempotente)
        var userIds = context.Users.Select(u => u.Id).ToList();
        var existingVerifIds = context.IdentityVerifications.Select(v => v.UserId).ToHashSet();

        foreach (var userId in userIds.Where(id => !existingVerifIds.Contains(id)))
        {
            var user = context.Users.Find(userId);
            if (user == null) continue;

            context.IdentityVerifications.Add(new IdentityVerification
            {
                UserId = userId,
                Status = user.VerificationStatus,
                ReviewedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (context.ChangeTracker.HasChanges())
            context.SaveChanges();
    }

    private static void SeedBetaInvites(WoodyDbContext context)
    {
        var raw = Environment.GetEnvironmentVariable("WOODY_DEV_BETA_INVITE_CODE")?.Trim();
        if (string.IsNullOrEmpty(raw))
            raw = "WOODY-DEV-BETA-2026";

        var code = BetaInviteNormalizer.Normalize(raw);
        if (context.BetaInvites.Any(i => i.Code == code))
            return;

        context.BetaInvites.Add(new BetaInvite
        {
            Code = code,
            Label = "Convite inicial beta",
            MaxUses = 10_000,
            UsesCount = 0,
            ExpiresAt = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed"
        });

        context.SaveChanges();
    }

    private static void EnsureUserSubscriptions(WoodyDbContext context)
    {
        var now = DateTime.UtcNow;
        var userIds = context.Users.Select(u => u.Id).ToList();
        var existing = context.UserSubscriptions.Select(s => s.UserId).ToHashSet();
        foreach (var id in userIds.Where(id => !existing.Contains(id)))
        {
            context.UserSubscriptions.Add(new UserSubscription
            {
                UserId = id,
                Plan = SubscriptionPlan.Free,
                Status = SubscriptionStatus.Active,
                PlanCode = BillingPlanCodes.Free,
                BillingProvider = BillingProvider.None,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (context.ChangeTracker.HasChanges())
            context.SaveChanges();
    }

    private static void EnsureCommunitySubscriptions(WoodyDbContext context)
    {
        var now = DateTime.UtcNow;
        var communityIds = context.Communities.Select(c => c.Id).ToList();
        var existing = context.CommunitySubscriptions.Select(s => s.CommunityId).ToHashSet();
        foreach (var id in communityIds.Where(id => !existing.Contains(id)))
        {
            context.CommunitySubscriptions.Add(new CommunitySubscription
            {
                CommunityId = id,
                Plan = CommunityPlan.Free,
                Status = SubscriptionStatus.Active,
                PlanCode = CommunityBillingPlanCodes.Free,
                BillingProvider = BillingProvider.None,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (context.ChangeTracker.HasChanges())
            context.SaveChanges();
    }

    /// <summary>Comunidade de exemplo com plano premium ativo (gates de staff + premium no cliente).</summary>
    private static void SeedCommunityPremiumDemo(WoodyDbContext context)
    {
        var community = context.Communities.AsNoTracking().FirstOrDefault(c => c.Slug == "mulheres-tech");
        if (community == null)
            return;

        var sub = context.CommunitySubscriptions.FirstOrDefault(s => s.CommunityId == community.Id);
        if (sub == null)
            return;

        var now = DateTime.UtcNow;
        sub.Plan = CommunityPlan.Premium;
        sub.Status = SubscriptionStatus.Active;
        sub.PlanCode = CommunityBillingPlanCodes.PremiumMonthly;
        sub.BillingProvider = BillingProvider.None;
        sub.CurrentPeriodStart = now;
        sub.CurrentPeriodEnd = now.AddDays(30);
        sub.CancelAtPeriodEnd = false;
        sub.UpdatedAt = now;
        context.SaveChanges();
    }

    /// <summary>
    /// Conta <c>admin</c> como <c>admin</c> da comunidade premium de demo (além do owner seed), para testar dashboard e boosts sem usar a owner.
    /// </summary>
    private static void EnsureAdminStaffOnPremiumDemoCommunity(WoodyDbContext context)
    {
        var admin = context.Users.FirstOrDefault(u => u.Username == "admin");
        var community = context.Communities.FirstOrDefault(c => c.Slug == "mulheres-tech");
        if (admin == null || community == null)
            return;

        var membership = context.CommunityMemberships.FirstOrDefault(m =>
            m.UserId == admin.Id && m.CommunityId == community.Id && m.Status == "active");
        if (membership == null)
            return;
        if (membership.Role == "owner")
            return;
        membership.Role = "admin";
        context.SaveChanges();
    }

    /// <summary>Um post em <c>mulheres-tech</c> com impulsionamento activo (feed e painel admin).</summary>
    private static void SeedCommunityPostBoostDemo(WoodyDbContext context)
    {
        var community = context.Communities.AsNoTracking().FirstOrDefault(c => c.Slug == "mulheres-tech");
        if (community == null)
            return;

        var post = context.Posts.AsNoTracking()
            .Where(p => p.CommunityId == community.Id)
            .OrderBy(p => p.Id)
            .FirstOrDefault();
        if (post == null)
            return;

        var now = DateTime.UtcNow;
        if (context.CommunityPostBoosts.Any(b =>
                b.PostId == post.Id && b.CancelledAtUtc == null && b.EndsAtUtc > now))
            return;

        context.CommunityPostBoosts.Add(new CommunityPostBoost
        {
            PostId = post.Id,
            CommunityId = community.Id,
            StartedAtUtc = now.AddHours(-2),
            EndsAtUtc = now.AddDays(5),
            CancelledAtUtc = null,
            CreatedAtUtc = now
        });
        context.SaveChanges();
    }

    /// <summary>
    /// Preenche <see cref="CommunityDailyRollup"/> + posts/comentários/gostos em <c>mulheres-tech</c> ao longo de ~3 meses
    /// para o painel premium (gráfico diário, top posts, tags). Idempotente.
    /// </summary>
    private static void SeedMulheresTechDashboardDemo(WoodyDbContext context)
    {
        var tech = context.Communities.AsNoTracking().FirstOrDefault(c => c.Slug == "mulheres-tech");
        if (tech == null)
            return;

        const string postPrefix = "[seed-dashboard] ";
        var techId = tech.Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rnd = new Random(90210);

        for (var dayOffset = 0; dayOffset < 120; dayOffset++)
        {
            var day = today.AddDays(-dayOffset);
            if (context.CommunityDailyRollups.Any(r => r.CommunityId == techId && r.DayUtc == day))
                continue;
            context.CommunityDailyRollups.Add(new CommunityDailyRollup
            {
                CommunityId = techId,
                DayUtc = day,
                PageViews = 8 + (dayOffset * 11) % 48,
                MemberLeaves = (dayOffset * 5) % 6
            });
        }

        context.SaveChanges();

        /* StartsWith(prefix, StringComparison) não traduz no Npgsql; usar uma sobrecarga traduzível. */
        if (context.Posts.Any(p => p.CommunityId == techId && p.Content.StartsWith(postPrefix)))
            return;

        var memberIds = context.CommunityMemberships.AsNoTracking()
            .Where(m => m.CommunityId == techId && m.Status == "active")
            .OrderBy(m => m.UserId)
            .Select(m => m.UserId)
            .Take(18)
            .ToList();
        if (memberIds.Count < 4)
            return;

        var tagPool = new[] { "carreira", "tech", "mentoria", "salário", "entrevista", "onboarding" };
        var postsBatch = new List<Post>();
        for (var dayOffset = 0; dayOffset < 96; dayOffset++)
        {
            if (dayOffset >= 30 && dayOffset % 3 == 2)
                continue;

            var day = today.AddDays(-dayOffset);
            var atUtc = new DateTime(day.Year, day.Month, day.Day, 13, 20, 0, DateTimeKind.Utc)
                .AddMinutes(rnd.Next(-110, 110));
            var authorId = memberIds[dayOffset % memberIds.Count];
            postsBatch.Add(new Post
            {
                PublicId = PostPublicIdGenerator.Generate(),
                UserId = authorId,
                CommunityId = techId,
                PublicationContext = PostPublicationContext.Community,
                Content =
                    $"{postPrefix}[{day:yyyy-MM-dd}] Post de seed para o painel premium: comentários e gostos sintéticos ao longo da timeline.",
                ImageUrl = dayOffset % 9 == 0 ? $"https://picsum.photos/seed/mtdash{dayOffset}/720/400" : null,
                CreatedAt = atUtc,
                UpdatedAt = null,
                DeletedAt = null
            });
        }

        context.Posts.AddRange(postsBatch);
        context.SaveChanges();

        var createdPosts = context.Posts.AsNoTracking()
            .Where(p => p.CommunityId == techId && p.Content.StartsWith(postPrefix))
            .OrderBy(p => p.CreatedAt)
            .ToList();

        foreach (var post in createdPosts)
        {
            var tagCount = 1 + rnd.Next(0, 3);
            for (var t = 0; t < tagCount; t++)
            {
                var tag = tagPool[(post.Id + t) % tagPool.Length];
                if (context.PostTags.Any(pt => pt.PostId == post.Id && pt.Tag == tag))
                    continue;
                context.PostTags.Add(new PostTag { PostId = post.Id, Tag = tag });
            }

            var authorExcluded = post.UserId;
            var commentators = memberIds.Where(id => id != authorExcluded).OrderBy(_ => rnd.Next()).Take(8).ToList();
            if (commentators.Count == 0)
                continue;
            var cAt = post.CreatedAt.AddMinutes(18);
            for (var c = 0; c < 3 + rnd.Next(0, 4); c++)
            {
                context.Comments.Add(new Comment
                {
                    PostId = post.Id,
                    AuthorId = commentators[c % commentators.Count],
                    ParentCommentId = null,
                    Content = $"Comentário seed #{c + 1} — partilha sobre {tagPool[c % tagPool.Length]}.",
                    CreatedAt = cAt.AddMinutes(4 * c),
                    DeletedAt = null,
                    HiddenByPostAuthorAt = null
                });
            }

            var likers = memberIds.Where(id => id != authorExcluded).OrderBy(_ => rnd.Next())
                .Take(Math.Min(memberIds.Count - 1, 11))
                .ToList();
            var lAt = post.CreatedAt.AddMinutes(6);
            foreach (var uid in likers)
            {
                if (context.Likes.Any(l =>
                        l.UserId == uid && l.TargetType == LikeTargetType.Post && l.TargetId == post.Id))
                    continue;
                context.Likes.Add(new Like
                {
                    UserId = uid,
                    TargetType = LikeTargetType.Post,
                    TargetId = post.Id,
                    CreatedAt = lAt.AddMinutes(rnd.Next(0, 200))
                });
            }
        }

        context.SaveChanges();
    }

    private static void SeedProDemoSubscription(WoodyDbContext context)
    {
        var now = DateTime.UtcNow;

        // user7: Pro ativo (caso principal – consegue criar comunidades, badge visível).
        PromoteToPro(context, "user7", SubscriptionStatus.Active, now.AddDays(30), cancelAtPeriodEnd: false, now);

        // user2: Pro a cancelar no fim do período (ainda com benefícios até CurrentPeriodEnd).
        PromoteToPro(context, "user2", SubscriptionStatus.Canceling, now.AddDays(10), cancelAtPeriodEnd: true, now);

        // user3: Pro expirado (sem benefícios, mas com histórico de Pro).
        PromoteToPro(context, "user3", SubscriptionStatus.Expired, now.AddDays(-5), cancelAtPeriodEnd: false, now.AddDays(-30));

        context.SaveChanges();
    }

    private static void PromoteToPro(
        WoodyDbContext context,
        string username,
        SubscriptionStatus status,
        DateTime? periodEnd,
        bool cancelAtPeriodEnd,
        DateTime updatedAt)
    {
        var user = context.Users.FirstOrDefault(x => x.Username == username);
        if (user == null)
            return;

        var sub = context.UserSubscriptions.FirstOrDefault(s => s.UserId == user.Id);
        if (sub == null)
            return;

        sub.Plan = SubscriptionPlan.Pro;
        sub.Status = status;
        sub.PlanCode = BillingPlanCodes.ProMonthly;
        sub.BillingProvider = BillingProvider.None;
        sub.CurrentPeriodEnd = periodEnd;
        sub.CancelAtPeriodEnd = cancelAtPeriodEnd;
        sub.UpdatedAt = updatedAt;
    }

    private static string Pic(int n) => $"https://picsum.photos/seed/woodyu{n}/128/128";

    private static string PicCover(int n) => $"https://picsum.photos/seed/woodyc{n}/800/240";

    private sealed record CommunitySeed(
        string Slug,
        string Name,
        string Description,
        string Category,
        string Rules,
        string Visibility,
        int OwnerIndex,
        string[] Tags,
        string? AvatarUrl,
        string? CoverUrl);

    private static void SeedCommunitiesAndMemberships(WoodyDbContext context)
    {
        var users = context.Users.OrderBy(u => u.Id).ToList();
        if (users.Count == 0)
            return;

        User ByIndex(int i) => users[Math.Clamp(i, 0, users.Count - 1)];

        var seeds = new[]
        {
            new CommunitySeed("geral", "Geral", "Espaço aberto para conversas transversais na Woody.", "outro",
                "Respeito mútuo, sem assédio nem discurso de ódio.", "public", 0,
                new[] { "boas-vindas", "anúncios" }, PicComm(1), PicCoverComm(1)),
            new CommunitySeed("mulheres-tech", "Mulheres na Tech", "Carreira, salários, entrevistas e apoio entre pares em tecnologia.",
                "carreira", "Partilhem experiências reais; evitem spam comercial.", "public", 7,
                new[] { "carreira", "tecnologia", "mentoria" }, PicComm(2), PicCoverComm(2)),
            new CommunitySeed("maternidade-acolhida", "Maternidade acolhida", "Gestação, pós-parto e parentalidade sem julgamentos.",
                "bemestar", "Acolhimento primeiro; nada de conselhos não solicitados agressivos.", "public", 2,
                new[] { "maternidade", "família" }, PicComm(3), PicCoverComm(3)),
            new CommunitySeed("viagem-sozinha", "Viagem sozinha sem medo", "Dicas, destinos e segurança para quem viaja a solo.",
                "cultura", "Foco em partilha segura de experiências.", "public", 4,
                new[] { "viagem", "solo", "segurança" }, PicComm(4), PicCoverComm(4)),
            new CommunitySeed("lgbtqia-espacos", "LGBTQIA+ nos espaços", "Visibilidade, direitos e comunidade.",
                "outro", "Confidencialidade e zero outing.", "public", 8,
                new[] { "lgbtqia", "direitos" }, PicComm(5), PicCoverComm(5)),
            new CommunitySeed("relacionamentos-limites", "Relacionamentos e limites", "Comunicação afetiva e autoestima.",
                "bemestar", "Sem terapia substituta — partilha entre pares.", "public", 9,
                new[] { "relacionamentos", "limites" }, PicComm(6), PicCoverComm(6)),
            new CommunitySeed("livros-historias", "Livros & histórias", "Leituras, autores e recomendações.",
                "cultura", "Spoiler só com aviso claro.", "public", 6,
                new[] { "livros", "leitura" }, PicComm(7), PicCoverComm(7)),
            new CommunitySeed("saude-mental-leve", "Saúde mental no dia a dia", "Ansiedade, rotinas e pequenos cuidados.",
                "bemestar", "Crise aguda: procure ajuda profissional de emergência.", "public", 5,
                new[] { "saúde mental", "bem-estar" }, PicComm(8), PicCoverComm(8)),
            new CommunitySeed("financas-pessoais", "Finanças para nós", "Orçamento, investimento básico e desmistificar dinheiro.",
                "carreira", "Sem conselhos financeiros individualizados ilegais.", "public", 10,
                new[] { "finanças", "independência" }, PicComm(9), PicCoverComm(9)),
            new CommunitySeed("empreendedorismo", "Mulheres a empreender", "Ideias, MVP e rede de apoio.",
                "carreira", "Promoção moderada; foco em valor.", "public", 11,
                new[] { "empreendedorismo", "negócios" }, PicComm(10), PicCoverComm(10)),
            new CommunitySeed("clube-leitura-privado", "Clube de leitura (convite)", "Grupo fechado para discussões mensais.",
                "cultura", "Apenas membros aprovados.", "private", 6,
                new[] { "livros", "privado" }, PicComm(11), PicCoverComm(11)),
            new CommunitySeed("mentoras-vip", "Rede de mentoras", "Match informal entre mentoras e mentees.",
                "carreira", "Pedidos respeitosos; sem spam.", "private", 7,
                new[] { "mentoria", "carreira" }, PicComm(12), PicCoverComm(12))
        };

        var now = DateTime.UtcNow;
        foreach (var s in seeds)
        {
            if (context.Communities.Any(c => c.Slug == s.Slug))
                continue;

            var owner = ByIndex(s.OwnerIndex);
            var c = new Community
            {
                Slug = s.Slug,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                Rules = s.Rules,
                Visibility = s.Visibility,
                OwnerUserId = owner.Id,
                MemberCount = 0,
                AvatarUrl = s.AvatarUrl,
                CoverUrl = s.CoverUrl,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Communities.Add(c);
            context.SaveChanges();

            foreach (var t in s.Tags)
            {
                context.CommunityTags.Add(new CommunityTag { CommunityId = c.Id, Tag = t });
            }

            context.SaveChanges();
        }

        // Filiações: todas as utilizadoras nas públicas; nas privadas só owner + algumas membros
        var allCommunities = context.Communities.Include(c => c.Tags).ToList();
        foreach (var c in allCommunities)
        {
            var memberUserIds = context.CommunityMemberships.Where(m => m.CommunityId == c.Id).Select(m => m.UserId)
                .ToHashSet();
            IEnumerable<User> toAdd = c.Visibility == "public"
                ? users
                : users.Where(u => u.Id == c.OwnerUserId || u.Id % 3 == 0 || u.Id % 5 == 0);

            foreach (var u in toAdd)
            {
                if (memberUserIds.Contains(u.Id))
                    continue;
                var role = u.Id == c.OwnerUserId ? "owner" : "member";
                context.CommunityMemberships.Add(new CommunityMembership
                {
                    UserId = u.Id,
                    CommunityId = c.Id,
                    Role = role,
                    Status = "active",
                    JoinedAt = now
                });
                memberUserIds.Add(u.Id);
            }
        }

        context.SaveChanges();
    }

    private static string PicComm(int n) => $"https://picsum.photos/seed/woodycom{n}/96/96";

    private static string PicCoverComm(int n) => $"https://picsum.photos/seed/woodycomc{n}/720/200";

    private static void SeedPosts(WoodyDbContext context)
    {
        const int minPosts = 55;
        if (context.Posts.Count() >= minPosts)
            return;

        var users = context.Users.OrderBy(u => u.Id).ToList();
        var communities = context.Communities.OrderBy(c => c.Id).ToList();
        if (users.Count == 0 || communities.Count == 0)
            return;

        var templates = new (string Hook, string Body, string[] Tags)[]
        {
            ("Bem-vindas a todas 💬", "Este é um espaço para nos apoiarmos. Como estão hoje?",
                new[] { "boas-vindas" }),
            ("Dica de produtividade suave", "Trabalho remoto: pausas curtas fazem diferença. O que funciona para vocês?",
                new[] { "carreira", "rotina" }),
            ("Livro da semana", "Acabei de ler uma narrativa incrível — sem spoilers, só vibes.",
                new[] { "livros" }),
            ("Primeira viagem a solo", "Estou a planear e sinto um misto de medo e emoção. Conselhos?",
                new[] { "viagem", "solo" }),
            ("Salário na tech", "Podemos falar de faixas salariais de forma anónima? Ajudem uma júnior.",
                new[] { "carreira", "tech" }),
            ("Limite com família", "Como dizer não sem culpa — partilhem frases que vos funcionam.",
                new[] { "limites" }),
            ("Receita rápida", "Sopa de legumes em 20 minutos que salva o jantar.", new[] { "bem-estar" }),
            ("Evento online gratuito", "Encontrei um webinar sobre literacia financeira — link nos comentários.",
                new[] { "finanças" }),
            ("Representação importa", "Ver histórias LGBTQIA+ bem contadas muda tudo.", new[] { "cultura" }),
            ("Burnout leve", "Sinais de que precisamos de pausa antes do crash.", new[] { "saúde mental" })
        };

        var rnd = new Random(42);
        var toCreate = minPosts - context.Posts.Count();
        var baseTime = DateTime.UtcNow.AddDays(-45);

        for (var i = 0; i < toCreate; i++)
        {
            var com = communities[rnd.Next(communities.Count)];
            var author = users[rnd.Next(users.Count)];
            if (!context.CommunityMemberships.Any(m =>
                    m.CommunityId == com.Id && m.UserId == author.Id && m.Status == "active"))
            {
                var memberId = context.CommunityMemberships
                    .Where(m => m.CommunityId == com.Id && m.Status == "active")
                    .Select(m => m.UserId)
                    .FirstOrDefault();
                if (memberId == default)
                    continue;
                author = users.First(x => x.Id == memberId);
            }

            var tpl = templates[i % templates.Length];
            var content =
                $"{tpl.Hook}\n\n{tpl.Body}\n\n(Seed #{i + 1} · comunidade {com.Name}.)";
            var post = new Post
            {
                PublicId = PostPublicIdGenerator.Generate(),
                UserId = author.Id,
                CommunityId = com.Id,
                PublicationContext = PostPublicationContext.Community,
                Content = content,
                ImageUrl = i % 7 == 0 ? $"https://picsum.photos/seed/woodypost{i}/720/400" : null,
                CreatedAt = baseTime.AddHours(i * 3 + rnd.Next(0, 4)),
                UpdatedAt = null
            };
            context.Posts.Add(post);
        }

        context.SaveChanges();

        // Tags em posts (após IDs)
        var postsNoTags = context.Posts
            .Include(p => p.Tags)
            .Where(p => !p.Tags.Any())
            .OrderBy(p => p.Id)
            .Take(120)
            .ToList();
        var tagPool = new[] { "dúvida", "dica", "debate", "recurso", "celebração" };
        foreach (var p in postsNoTags)
        {
            var tpl = templates[p.Id % templates.Length];
            var added = 0;
            foreach (var t in tpl.Tags)
            {
                if (added >= 3) break;
                context.PostTags.Add(new PostTag { PostId = p.Id, Tag = t });
                added++;
            }

            if (added < 3 && rnd.Next(0, 2) == 0)
                context.PostTags.Add(new PostTag { PostId = p.Id, Tag = tagPool[p.Id % tagPool.Length] });
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Posts com 2–3 fotos pela conta <c>admin</c> (login dev: <c>admin</c> / <c>admin123</c>):
    /// dois na comunidade <c>geral</c> e um só de perfil. Idempotente (prefixo no texto <c>[Demo Woody]</c>).
    /// </summary>
    private static void SeedAdminGalleryDemoPosts(WoodyDbContext context)
    {
        const string demoPrefix = "[Demo Woody]";
        if (context.Posts.Any(p => p.Content.StartsWith(demoPrefix)))
            return;

        var admin = context.Users.FirstOrDefault(u => u.Username == "admin");
        var geral = context.Communities.FirstOrDefault(c => c.Slug == "geral");
        if (admin == null || geral == null)
            return;

        var createdAtBase = DateTime.UtcNow.AddMinutes(-45);

        static string Shot(int seed) =>
            $"https://picsum.photos/seed/woody-gallery-demo-{seed}/720/480";

        void AddTags(int postId, params string[] tags)
        {
            foreach (var t in tags)
            {
                if (context.PostTags.Any(pt => pt.PostId == postId && pt.Tag == t))
                    continue;
                context.PostTags.Add(new PostTag { PostId = postId, Tag = t });
            }
        }

        var p1 = new Post
        {
            PublicId = PostPublicIdGenerator.Generate(),
            UserId = admin.Id,
            CommunityId = geral.Id,
            PublicationContext = PostPublicationContext.Community,
            Content =
                $"{demoPrefix} Galeria · 3 fotos\n\nSeed da base de dados: três imagens no mesmo post (media_attachments). Testa o mosaico no feed, o perfil da admin e o bloco «Fotos & Vídeos».",
            ImageUrl = null,
            CreatedAt = createdAtBase,
            UpdatedAt = null,
            DeletedAt = null
        };
        context.Posts.Add(p1);
        context.SaveChanges();
        AddPostImageAttachments(context, p1.Id, createdAtBase, Shot(31), Shot(32), Shot(33));
        AddTags(p1.Id, "demo", "galeria");

        var p2 = new Post
        {
            PublicId = PostPublicIdGenerator.Generate(),
            UserId = admin.Id,
            CommunityId = geral.Id,
            PublicationContext = PostPublicationContext.Community,
            Content =
                $"{demoPrefix} Duo · 2 fotos lado a lado\n\nSegundo exemplo com duas imagens. Útil para validar layout em ecrãs estreitos e o lightbox.",
            ImageUrl = null,
            CreatedAt = createdAtBase.AddMinutes(12),
            UpdatedAt = null,
            DeletedAt = null
        };
        context.Posts.Add(p2);
        context.SaveChanges();
        AddPostImageAttachments(context, p2.Id, p2.CreatedAt, Shot(41), Shot(42));
        AddTags(p2.Id, "demo", "galeria");

        var p3 = new Post
        {
            PublicId = PostPublicIdGenerator.Generate(),
            UserId = admin.Id,
            CommunityId = null,
            PublicationContext = PostPublicationContext.Profile,
            Content =
                $"{demoPrefix} Álbum apenas no perfil\n\nPublicação com contexto de perfil (sem comunidade). Aparece no teu perfil e no grid de fotos.",
            ImageUrl = null,
            CreatedAt = createdAtBase.AddMinutes(24),
            UpdatedAt = null,
            DeletedAt = null
        };
        context.Posts.Add(p3);
        context.SaveChanges();
        AddPostImageAttachments(context, p3.Id, p3.CreatedAt, Shot(51), Shot(52), Shot(53));
        AddTags(p3.Id, "demo", "perfil");

        context.SaveChanges();
    }

    private static void AddPostImageAttachments(
        WoodyDbContext context,
        int postId,
        DateTime createdAt,
        params string[] urls)
    {
        for (var i = 0; i < urls.Length; i++)
        {
            context.MediaAttachments.Add(new MediaAttachment
            {
                OwnerType = MediaOwnerType.Post,
                OwnerId = postId,
                PostId = postId,
                Url = urls[i],
                MediaKind = MediaKind.Image,
                DisplayOrder = i,
                CreatedAt = createdAt
            });
        }

        context.SaveChanges();
    }

    private static void SeedComments(WoodyDbContext context)
    {
        const int minComments = 85;
        if (context.Comments.Count() >= minComments)
            return;

        var posts = context.Posts.OrderByDescending(p => p.CreatedAt).Take(22).ToList();
        var users = context.Users.OrderBy(u => u.Id).ToList();
        if (posts.Count == 0 || users.Count < 3)
            return;

        var bodies = new[]
        {
            "Concordo plenamente, obrigada por partilhar.",
            "Também passei por algo parecido — melhoras!",
            "Tens algum link ou recurso que recomendes?",
            "Aqui na minha cidade funciona assim…",
            "Acho importante normalizar esta conversa.",
            "Enviei mensagem privada 💜",
            "Marquei para ler com calma mais tarde."
        };

        var rnd = new Random(99);
        var t = DateTime.UtcNow.AddDays(-20);
        foreach (var post in posts)
        {
            var n = 3 + rnd.Next(0, 4);
            Comment? threadRoot = null;
            for (var i = 0; i < n; i++)
            {
                var author = users[rnd.Next(users.Count)];
                if (author.Id == post.UserId && users.Count > 1)
                    author = users[(rnd.Next(users.Count) + 1) % users.Count];

                var c = new Comment
                {
                    PostId = post.Id,
                    AuthorId = author.Id,
                    ParentCommentId = threadRoot != null && i % 3 == 2 ? threadRoot.Id : null,
                    Content = bodies[(post.Id + i) % bodies.Length],
                    CreatedAt = t
                };
                context.Comments.Add(c);
                context.SaveChanges();
                if (i == 0)
                    threadRoot = c;
                t = t.AddMinutes(rnd.Next(5, 80));
            }
        }
    }

    private static void SeedFollows(WoodyDbContext context)
    {
        var users = context.Users.OrderBy(u => u.Id).ToList();
        if (users.Count < 4)
            return;

        var pairs = new HashSet<(int A, int B)>();
        void TryAdd(int following, int followed)
        {
            if (following == followed)
                return;
            if (pairs.Contains((following, followed)))
                return;
            if (context.Follows.Any(f => f.FollowingUserId == following && f.FollowedUserId == followed))
                return;
            pairs.Add((following, followed));
            context.Follows.Add(new Follow
            {
                FollowingUserId = following,
                FollowedUserId = followed,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Grafo denso mas sem duplicar
        for (var i = 0; i < users.Count; i++)
        {
            for (var j = 1; j <= 6; j++)
            {
                var target = users[(i + j) % users.Count];
                TryAdd(users[i].Id, target.Id);
            }
        }

        // Cenários de QA: listagens longas e contagens marcantes (user1 = 2.ª conta seed = Beatriz).
        if (users.Count >= 12)
        {
            var beatriz = users[1];
            var camila = users[2];
            // Muitos seguidores em Beatriz — GET /followers + paginação no cliente.
            for (var i = 2; i < users.Count; i++)
                TryAdd(users[i].Id, beatriz.Id);
            // Beatriz segue quase todas as outras — GET /following preenchido.
            for (var i = 2; i < users.Count; i++)
                TryAdd(beatriz.Id, users[i].Id);
            // Camila: rede média (perfil alternativo para testar modais).
            for (var i = 3; i < Math.Min(users.Count, 14); i++)
                TryAdd(users[i].Id, camila.Id);
            for (var i = 3; i < Math.Min(users.Count, 12); i++)
                TryAdd(camila.Id, users[i].Id);
            // Última conta seed: poucos follows (estado “quase vazio” em A seguir).
            var yara = users[^1];
            if (yara.Id != beatriz.Id)
            {
                TryAdd(yara.Id, beatriz.Id);
                TryAdd(yara.Id, camila.Id);
            }
        }

        context.SaveChanges();
    }

    private static void SeedLikes(WoodyDbContext context)
    {
        var users = context.Users.OrderBy(u => u.Id).ToList();
        var posts = context.Posts.OrderBy(p => p.Id).ToList();
        var comments = context.Comments.OrderBy(c => c.Id).Take(40).ToList();
        if (users.Count == 0 || posts.Count == 0)
            return;

        var rnd = new Random(7);
        var t = DateTime.UtcNow.AddDays(-10);
        var pendingPostLikes = new HashSet<(int UserId, int PostId)>();
        var pendingCommentLikes = new HashSet<(int UserId, int CommentId)>();

        void TryLikePost(int userId, int postId)
        {
            if (!pendingPostLikes.Add((userId, postId)))
                return;
            if (context.Likes.Any(l =>
                    l.UserId == userId && l.TargetType == LikeTargetType.Post && l.TargetId == postId))
                return;
            context.Likes.Add(new Like
            {
                UserId = userId,
                TargetType = LikeTargetType.Post,
                TargetId = postId,
                CreatedAt = t
            });
        }

        void TryLikeComment(int userId, int commentId)
        {
            if (!pendingCommentLikes.Add((userId, commentId)))
                return;
            if (context.Likes.Any(l =>
                    l.UserId == userId && l.TargetType == LikeTargetType.Comment && l.TargetId == commentId))
                return;
            context.Likes.Add(new Like
            {
                UserId = userId,
                TargetType = LikeTargetType.Comment,
                TargetId = commentId,
                CreatedAt = t
            });
        }

        foreach (var p in posts)
        {
            var likers = users.OrderBy(_ => rnd.Next()).Take(rnd.Next(4, Math.Min(users.Count, 14))).ToList();
            foreach (var u in likers)
                TryLikePost(u.Id, p.Id);
        }

        foreach (var c in comments)
        {
            var likers = users.OrderBy(_ => rnd.Next()).Take(rnd.Next(0, 6)).ToList();
            foreach (var u in likers)
                TryLikeComment(u.Id, c.Id);
        }

        context.SaveChanges();
    }

    private static void SeedUserInterestsAndSocialLinks(WoodyDbContext context)
    {
        var labels = new[]
        {
            "Carreira", "Bem-estar", "Leitura", "Viagens", "Tech", "Finanças", "Maternidade", "Arte",
            "Empreendedorismo", "Saúde mental"
        };

        foreach (var u in context.Users.OrderBy(x => x.Id).ToList())
        {
            if (context.UserInterests.Any(i => i.UserId == u.Id))
                continue;

            var start = u.Id % labels.Length;
            for (var k = 0; k < 3; k++)
                context.UserInterests.Add(new UserInterest
                {
                    UserId = u.Id,
                    Label = labels[(start + k) % labels.Length]
                });
        }

        foreach (var u in context.Users.Where(x => x.Id % 4 == 0).OrderBy(x => x.Id).ToList())
        {
            if (context.UserSocialLinks.Any(s => s.UserId == u.Id))
                continue;
            context.UserSocialLinks.Add(new UserSocialLink
            {
                UserId = u.Id,
                Platform = "instagram",
                Label = "Instagram",
                Url = "https://instagram.com/example",
                Handle = u.Username
            });
        }

        context.SaveChanges();
    }

    private static void SeedJoinRequests(WoodyDbContext context)
    {
        var privateComms = context.Communities.Where(c => c.Visibility == "private").ToList();
        var users = context.Users.OrderBy(u => u.Id).ToList();
        if (privateComms.Count == 0 || users.Count < 8)
            return;

        foreach (var c in privateComms)
        {
            var candidates = users.Where(u =>
                !context.CommunityMemberships.Any(m =>
                    m.CommunityId == c.Id && m.UserId == u.Id && m.Status == "active")).Take(4).ToList();
            foreach (var u in candidates)
            {
                if (context.JoinRequests.Any(j => j.CommunityId == c.Id && j.UserId == u.Id && j.Status == "pending"))
                    continue;
                context.JoinRequests.Add(new JoinRequest
                {
                    CommunityId = c.Id,
                    UserId = u.Id,
                    Status = "pending",
                    RequestedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                });
            }
        }

        context.SaveChanges();
    }

    private static void SeedContentReports(WoodyDbContext context)
    {
        if (context.ContentReports.Any())
            return;

        var posts = context.Posts.OrderBy(p => p.Id).Take(2).ToList();
        var users = context.Users.OrderBy(u => u.Id).ToList();
        if (posts.Count == 0 || users.Count < 2)
            return;

        context.ContentReports.Add(new ContentReport
        {
            ReporterUserId = users[1].Id,
            TargetType = "post",
            PostId = posts[0].Id,
            CommentId = null,
            ReasonCode = "spam",
            Details = "Seed: denúncia de teste num post.",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        var comment = context.Comments.FirstOrDefault();
        if (comment != null)
        {
            context.ContentReports.Add(new ContentReport
            {
                ReporterUserId = users[2].Id,
                TargetType = "comment",
                PostId = comment.PostId,
                CommentId = comment.Id,
                ReasonCode = "harassment",
                Details = "Seed: denúncia de teste num comentário.",
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            });
        }

        context.SaveChanges();
    }

    private static void RecalculateCommunityMemberCounts(WoodyDbContext context)
    {
        foreach (var c in context.Communities.ToList())
        {
            c.MemberCount = context.CommunityMemberships.Count(m => m.CommunityId == c.Id && m.Status == "active");
            c.UpdatedAt = DateTime.UtcNow;
        }

        context.SaveChanges();
    }
}
