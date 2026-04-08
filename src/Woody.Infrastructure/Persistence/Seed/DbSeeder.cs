using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Infrastructure.Persistence.Context;
using Woody.Infrastructure.Security;

namespace Woody.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados de desenvolvimento. Idempotente por utilizador (username), comunidade (slug) e contagens mínimas.
/// Para repovoar do zero: apagar a base ou <c>dotnet ef database drop</c> antes de migrar.
/// </summary>
public static class DbSeeder
{
    private const string DevPasswordSuffix = "Woody2026!";

    public static void Seed(WoodyDbContext context)
    {
        SeedUsers(context);
        SeedCommunitiesAndMemberships(context);
        SeedPosts(context);
        SeedComments(context);
        SeedFollows(context);
        SeedLikes(context);
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

        foreach (var d in defs)
        {
            if (context.Users.Any(u => u.Username == d.Username))
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
        }

        context.SaveChanges();
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

        var templates = new (string Title, string Content, string[] Tags)[]
        {
            ("Bem-vindas a todas", "Este é um espaço para nos apoiarmos. Como estão hoje?",
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
            var title = $"{tpl.Title} #{i + 1}";
            var content = $"{tpl.Content}\n\n(Conteúdo de seed para testes — post #{i + 1} na comunidade {com.Name}.)";
            var post = new Post
            {
                UserId = author.Id,
                CommunityId = com.Id,
                Title = title,
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
            foreach (var t in tpl.Tags.Take(2))
                context.PostTags.Add(new PostTag { PostId = p.Id, Tag = t });
            if (rnd.Next(0, 2) == 0)
                context.PostTags.Add(new PostTag { PostId = p.Id, Tag = tagPool[p.Id % tagPool.Length] });
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
                    RequestedAt = DateTime.UtcNow.AddDays(-2)
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
