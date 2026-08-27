// ============================================================
// MODULE : M13 — Profile
// LAYER  : Service
// FEATURE: F16 — Reputation & Badge System
// IMPLEMENTS: bio and profile info, skills/interests, portfolio,
//   achievements, reputation points, badges/levels, activity history.
// ============================================================
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AiInnovationHub.Api.Services;

public interface IProfileService
{
    Task<ProfileDto?> GetAsync(Guid profileId, Guid viewerId, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid userId, ProfileUpdateRequest req, CancellationToken ct = default);
}

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IBadgeService _badges;

    public ProfileService(AppDbContext db, IBadgeService badges)
    {
        _db = db; _badges = badges;
    }

    public async Task<ProfileDto?> GetAsync(Guid profileId, Guid viewerId, CancellationToken ct = default)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileId, ct);
        if (u is null) return null;

        // Re-check badges on view so a profile is never stale.
        await _badges.AwardNewAsync(profileId, ct);

        // Only published ideas appear in the public portfolio; the owner
        // sees their drafts too.
        var ideas = await _db.Ideas.AsNoTracking()
            .Where(i => i.AuthorId == profileId && (i.IsPublished || profileId == viewerId))
            .OrderByDescending(i => i.CreatedAt).Take(6).ToListAsync(ct);

        var activity = await _db.ActivityLogs.AsNoTracking()
            .Where(a => a.UserId == profileId)
            .OrderByDescending(a => a.CreatedAt).Take(8).ToListAsync(ct);

        return new ProfileDto
        {
            Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role,
            Bio = u.Bio, Headline = u.Headline, Location = u.Location, Website = u.Website,
            Skills = u.Skills, Interests = u.Interests,
            Expertise = u.Expertise, InvestmentFocus = u.InvestmentFocus,
            IsAvailableForMentoring = u.IsAvailableForMentoring,

            ReputationPoints = u.ReputationPoints,
            Level = IBadgeService.LevelFor(u.ReputationPoints),
            IdeaCount = await _db.Ideas.CountAsync(i => i.AuthorId == profileId && i.IsPublished, ct),
            ProjectCount = await _db.Projects.CountAsync(p => p.OwnerId == profileId, ct),
            CommentCount = await _db.Comments.CountAsync(c => c.AuthorId == profileId, ct)
                         + await _db.PostComments.CountAsync(c => c.AuthorId == profileId, ct),
            IsMe = profileId == viewerId,

            Badges = await _badges.EvaluateAsync(profileId, ct),
            RecentIdeas = ideas.Select(i => new IdeaCardDto
            {
                Id = i.Id, Title = i.Title, Summary = i.Summary, Category = i.Category,
                Tags = Format.SplitTags(i.Tags), Upvotes = i.Upvotes,
                CommentCount = i.CommentCount, Views = i.Views,
                AuthorName = u.FullName, AuthorRole = u.Role,
                CreatedAt = i.CreatedAt, TimeAgo = Format.TimeAgo(i.CreatedAt),
            }).ToList(),
            RecentActivity = activity.Select(a => new ActivityDto
            {
                Description = a.Description, TimeAgo = Format.TimeAgo(a.CreatedAt),
            }).ToList(),
        };
    }

    // ---- Edit your own profile only ----
    public async Task<bool> UpdateAsync(Guid userId, ProfileUpdateRequest req, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (u is null) return false;

        // Role and email are deliberately NOT editable here — role changes
        // belong to M14 administration, not self-service (NFR4).
        if (!string.IsNullOrWhiteSpace(req.FullName)) u.FullName = req.FullName.Trim();
        u.Bio = req.Bio.Trim();
        u.Headline = req.Headline.Trim();
        u.Location = req.Location.Trim();
        u.Website = req.Website.Trim();
        u.Skills = req.Skills.Trim();
        u.Interests = req.Interests.Trim();
        u.Expertise = req.Expertise.Trim();
        u.InvestmentFocus = req.InvestmentFocus.Trim();
        u.IsAvailableForMentoring = req.IsAvailableForMentoring;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
