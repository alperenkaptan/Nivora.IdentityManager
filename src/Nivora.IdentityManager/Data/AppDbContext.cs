using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Persistence;

namespace Nivora.IdentityManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddNivoraIdentitySchema();
        modelBuilder.Entity<IdentityUserRow>().HasNoKey().ToView(null);
    }
}
