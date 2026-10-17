using Archive.Core.Authorization;
using Archive.Core.Entities.Authentication;
using Archive.Core.Entities.Authentication.Permissible;
using Archive.Core.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.UserRepository;

internal sealed partial class UserRepository
{
    public async Task<UserListPage> SearchAsync(string? query, ArchiveRole? role, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var users = dataContext.Users.AsNoTracking().Where(user => user.RegistrationCompletedAt != null);
        if (role.HasValue) users = users.Where(user => user.Role == role.Value);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            var matchesUser = "user".Contains(term, StringComparison.Ordinal);
            var matchesAdmin = "admin".Contains(term, StringComparison.Ordinal);
            users = users.Where(user => user.Handle!.ToLower().Contains(term) || user.DisplayName!.ToLower().Contains(term) ||
                matchesUser && user.Role == ArchiveRole.User || matchesAdmin && user.Role == ArchiveRole.Admin);
        }
        var total = await users.CountAsync(cancellationToken);
        var items = await users.OrderBy(user => user.Handle).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new UserListEntry(user.Handle!, user.DisplayName!, user.ProfileImage != null && user.ProfileImage.Length > 0, user.Role))
            .ToArrayAsync(cancellationToken);
        return new(items, total, page, pageSize);
    }

    public Task<ArchiveUser?> GetAccessAsync(string handle, CancellationToken cancellationToken = default) => dataContext.Users
        .AsNoTracking().Include(user => user.PermissionOverrides)
        .FirstOrDefaultAsync(user => user.Handle == handle && user.RegistrationCompletedAt != null, cancellationToken);

    public async Task<UserAccessUpdateResult> UpdateAccessAsync(Guid actorId, string handle, string expectedTag, UserAccessUpdate update, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dataContext.Database.BeginTransactionAsync(cancellationToken);
        // Serialize administration writes across processes, including the last-administrator check.
        // Profile updates still coordinate through the target row lock and the access fingerprint.
        await dataContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(734901826)", cancellationToken);
        var actor = await dataContext.Users.AsNoTracking().Include(user => user.PermissionOverrides)
            .FirstOrDefaultAsync(user => user.Id == actorId, cancellationToken);
        if (!CanManage(actor, update)) return new UserAccessUpdateResult.Forbidden();

        var target = await dataContext.Users.FromSqlInterpolated($"SELECT * FROM authentication.users WHERE handle = {handle} FOR UPDATE")
            .Include(user => user.PermissionOverrides).FirstOrDefaultAsync(cancellationToken);
        if (target is null || !target.IsRegistrationComplete) return new UserAccessUpdateResult.NotFound();
        if (target.Id == actorId) return new UserAccessUpdateResult.SelfChange();
        if (!string.Equals(expectedTag, UserAccessTag.Create(target), StringComparison.Ordinal)) return new UserAccessUpdateResult.Stale();

        if (update.Role.HasValue) target.Role = update.Role.Value;
        foreach (var (key, effect) in update.PermissionOverrides)
        {
            var existing = target.PermissionOverrides.FirstOrDefault(item => item.Permission == key);
            if (effect is null)
            {
                if (existing is not null) { target.PermissionOverrides.Remove(existing); dataContext.UserPermissionOverrides.Remove(existing); }
            }
            else if (existing is not null) existing.Effect = effect.Value;
            else target.PermissionOverrides.Add(new() { UserId = target.Id, Permission = key, Effect = effect.Value });
        }

        // Track all current admins so the target's unsaved state participates in this check.
        var administrators = await dataContext.Users.Include(user => user.PermissionOverrides)
            .Where(user => user.Role == ArchiveRole.Admin && user.RegistrationCompletedAt != null).ToListAsync(cancellationToken);
        if (!administrators.Append(target).Any(IsFullAdministrator)) return new UserAccessUpdateResult.LastAdministrator();

        target.UpdatedAt = DateTimeOffset.UtcNow;
        await dataContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new UserAccessUpdateResult.Updated(target);
    }

    private static bool CanManage(ArchiveUser? actor, UserAccessUpdate update) =>
        PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AccessAdminPortal) &&
        PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AccessAdminUsers) &&
        (!update.Role.HasValue || PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AssignUserRoles)) &&
        (update.PermissionOverrides.Count == 0 || PermissionEvaluator.IsAllowed(actor, ArchivePermissions.AssignUserPermissions));

    private static bool IsFullAdministrator(ArchiveUser user) => user.Role == ArchiveRole.Admin &&
        PermissionEvaluator.IsAllowed(user, ArchivePermissions.AccessAdminPortal) &&
        PermissionEvaluator.IsAllowed(user, ArchivePermissions.AccessAdminUsers) &&
        PermissionEvaluator.IsAllowed(user, ArchivePermissions.AssignUserRoles) &&
        PermissionEvaluator.IsAllowed(user, ArchivePermissions.AssignUserPermissions);
}
