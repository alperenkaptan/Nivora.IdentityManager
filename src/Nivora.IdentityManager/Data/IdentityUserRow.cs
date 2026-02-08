namespace Nivora.IdentityManager.Data;

public class IdentityUserRow
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public bool IsDisabled { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
