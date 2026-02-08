using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Persistence;

namespace Nivora.IdentityManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddNivoraIdentitySchema(o =>
        {
            o.UsersTableName = "Users";
            o.RefreshTokensTableName = "Tokens";
            o.PasswordResetTokensTableName = "PasswordResetTokens";
            o.EmailConfirmationTokensTableName = "EmailConfirmationTokens";
            o.RolesTableName = "Roles";
            o.UserRolesTableName = "UserRoles";
            o.ExternalLoginsTableName = "ExternalLogins";
            o.PhoneVerificationTokensTableName = "PhoneVerificationTokens";
            o.TwoFactorChallengesTableName = "TwoFactorChallenges";
        });
        modelBuilder.Entity<IdentityUserRow>().HasNoKey().ToView(null);
    }
}
