// ============================================================
// FILE   : Data/AppDbContext.cs
// LAYER  : Model (MVC: M) — persistence / EF Core context
// PURPOSE: Single gateway between the application and PostgreSQL.
//          Declares tables, keys, indexes and relationships.
// MODULES: M1, M3, M4, M5, M6
// NFR    : NFR13 Data Integrity
// ============================================================
using AiInnovationHub.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ---- TABLES ----
    public DbSet<User> Users => Set<User>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<IdeaLike> IdeaLikes => Set<IdeaLike>();
    public DbSet<IdeaBookmark> IdeaBookmarks => Set<IdeaBookmark>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();

    // ---- M7 Community ----
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMember> CommunityMembers => Set<CommunityMember>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<PostUpvote> PostUpvotes => Set<PostUpvote>();

    // ---- M9 Challenges ----
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeSubmission> ChallengeSubmissions => Set<ChallengeSubmission>();

    // ---- M10 Mentor & Investor ----
    public DbSet<MentorshipRequest> MentorshipRequests => Set<MentorshipRequest>();
    public DbSet<InvestmentInterest> InvestmentInterests => Set<InvestmentInterest>();

    // ---- M12 Notifications ----
    public DbSet<Notification> Notifications => Set<Notification>();

    // ---- M13 Badges ----
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    // ---- M14 Moderation ----
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ================= M1 : USER =================
        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();   // no two accounts share an email
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.Role).IsRequired().HasMaxLength(30);
        });

        // ================= M5 : IDEA =================
        b.Entity<Idea>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Title).IsRequired().HasMaxLength(200);
            e.Property(i => i.Category).HasMaxLength(80);
            e.Property(i => i.Tags).HasMaxLength(300);

            e.HasOne(i => i.Author).WithMany(u => u.Ideas)
             .HasForeignKey(i => i.AuthorId).OnDelete(DeleteBehavior.Cascade);

            // The feed sorts published ideas by recency and by upvotes,
            // so both get an index.
            e.HasIndex(i => new { i.IsPublished, i.PublishedAt });
            e.HasIndex(i => i.Upvotes);
        });

        // ================= M4 : LIKE =================
        b.Entity<IdeaLike>(e =>
        {
            e.HasKey(l => l.Id);
            // One user may upvote a given idea exactly once.
            e.HasIndex(l => new { l.UserId, l.IdeaId }).IsUnique();

            e.HasOne(l => l.Idea).WithMany(i => i.Likes)
             .HasForeignKey(l => l.IdeaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.User).WithMany()
             .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M4 : BOOKMARK =================
        b.Entity<IdeaBookmark>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.IdeaId }).IsUnique();

            e.HasOne(x => x.Idea).WithMany(i => i.Bookmarks)
             .HasForeignKey(x => x.IdeaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M4 : COMMENT =================
        b.Entity<Comment>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Content).IsRequired().HasMaxLength(2000);

            e.HasOne(c => c.Idea).WithMany(i => i.Comments)
             .HasForeignKey(c => c.IdeaId).OnDelete(DeleteBehavior.Cascade);
            // Restrict, not Cascade: deleting a user must not silently
            // erase discussion history other people replied to.
            e.HasOne(c => c.Author).WithMany()
             .HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M6 : PROJECT =================
        b.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).IsRequired().HasMaxLength(200);
            e.Property(p => p.Status).HasMaxLength(30);

            e.HasOne(p => p.Owner).WithMany()
             .HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);

            // SetNull: deleting the source idea should not delete the
            // project built from it — it just loses its origin link.
            e.HasOne(p => p.SourceIdea).WithMany()
             .HasForeignKey(p => p.SourceIdeaId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================= M6 : MEMBER =================
        b.Entity<ProjectMember>(e =>
        {
            e.HasKey(m => m.Id);
            // A user appears at most once per project.
            e.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();

            e.HasOne(m => m.Project).WithMany(p => p.Members)
             .HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany()
             .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M6 : TASK =================
        b.Entity<ProjectTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);

            e.HasOne(t => t.Project).WithMany(p => p.Tasks)
             .HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
            // SetNull: removing a member leaves their tasks in the project
            // as unassigned rather than destroying them.
            e.HasOne(t => t.Assignee).WithMany()
             .HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================= M6 : MILESTONE =================
        b.Entity<Milestone>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).IsRequired().HasMaxLength(200);
            e.HasOne(m => m.Project).WithMany(p => p.Milestones)
             .HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M6 : FILE =================
        b.Entity<ProjectFile>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.FileName).IsRequired().HasMaxLength(260);
            e.Property(f => f.StoredName).IsRequired().HasMaxLength(100);

            e.HasOne(f => f.Project).WithMany(p => p.Files)
             .HasForeignKey(f => f.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.UploadedBy).WithMany()
             .HasForeignKey(f => f.UploadedById).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M7 : COMMUNITY =================
        b.Entity<Community>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(120);
            e.HasIndex(c => c.Name).IsUnique();   // community names are unique
            e.HasOne(c => c.CreatedBy).WithMany()
             .HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CommunityMember>(e =>
        {
            e.HasKey(m => m.Id);
            // A user joins a community at most once.
            e.HasIndex(m => new { m.CommunityId, m.UserId }).IsUnique();
            e.HasOne(m => m.Community).WithMany(c => c.Members)
             .HasForeignKey(m => m.CommunityId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany()
             .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CommunityPost>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).IsRequired().HasMaxLength(200);
            e.Property(p => p.Content).IsRequired().HasMaxLength(5000);
            e.HasOne(p => p.Community).WithMany(c => c.Posts)
             .HasForeignKey(p => p.CommunityId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Author).WithMany()
             .HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostComment>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Content).IsRequired().HasMaxLength(2000);
            e.HasOne(c => c.Post).WithMany(p => p.Comments)
             .HasForeignKey(c => c.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Author).WithMany()
             .HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PostUpvote>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.PostId, u.UserId }).IsUnique();
        });

        // ================= M9 : CHALLENGES =================
        b.Entity<Challenge>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Title).IsRequired().HasMaxLength(200);
            e.HasOne(c => c.CreatedBy).WithMany()
             .HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChallengeSubmission>(e =>
        {
            e.HasKey(s2 => s2.Id);
            // One idea may only be entered into a given challenge once.
            e.HasIndex(s2 => new { s2.ChallengeId, s2.IdeaId }).IsUnique();
            e.HasOne(s2 => s2.Challenge).WithMany(c => c.Submissions)
             .HasForeignKey(s2 => s2.ChallengeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s2 => s2.Idea).WithMany()
             .HasForeignKey(s2 => s2.IdeaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s2 => s2.User).WithMany()
             .HasForeignKey(s2 => s2.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M10 : MENTOR / INVESTOR =================
        b.Entity<MentorshipRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Message).HasMaxLength(2000);
            // Restrict both sides: EF cannot cascade two paths to Users.
            e.HasOne(r => r.Mentor).WithMany()
             .HasForeignKey(r => r.MentorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Requester).WithMany()
             .HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<InvestmentInterest>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Message).HasMaxLength(2000);
            e.Property(i => i.Amount).HasPrecision(18, 2);
            e.HasOne(i => i.Investor).WithMany()
             .HasForeignKey(i => i.InvestorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Project).WithMany()
             .HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M12 : NOTIFICATIONS =================
        b.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Message).IsRequired().HasMaxLength(400);
            // The bell queries unread-per-user constantly, so index it.
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasOne(n => n.User).WithMany()
             .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M13 : BADGES =================
        b.Entity<Badge>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).IsRequired().HasMaxLength(60);
        });

        b.Entity<UserBadge>(e =>
        {
            e.HasKey(x => x.Id);
            // A badge is earned once per user.
            e.HasIndex(x => new { x.UserId, x.BadgeId }).IsUnique();
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Badge).WithMany()
             .HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ================= M14 : MODERATION =================
        b.Entity<ContentReport>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            e.HasIndex(r => r.Status);
            e.HasOne(r => r.Reporter).WithMany()
             .HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================= M3 : ACTIVITY LOG =================
        b.Entity<ActivityLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Description).IsRequired().HasMaxLength(300);
            e.HasOne(a => a.User).WithMany(u => u.Activities)
             .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
