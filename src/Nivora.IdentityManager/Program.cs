using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Data;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Nivora Identity (facade + admin service registered automatically)
builder.Services.AddNivoraIdentity<AppDbContext>(
    builder.Configuration.GetSection("NivoraIdentity"));

// Razor Pages + Session
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// Seed admin user + Admin role
await SeedAdminAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task SeedAdminAsync(WebApplication app)
{
    var email = app.Configuration["AdminSeed:Email"];
    var password = app.Configuration["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return;

    using var scope = app.Services.CreateScope();
    var admin = scope.ServiceProvider.GetRequiredService<IIdentityAdminService>();
    var existing = await admin.FindByEmailAsync(email);
    if (existing is not null)
    {
        // Ensure Admin role is assigned even if user already exists
        var roles = await admin.GetUserRolesAsync(existing.Id);
        if (!roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            await admin.AssignRoleAsync(existing.Id, "Admin");
        return;
    }

    var user = await admin.CreateUserAsync(email, password, confirmEmail: true);
    await admin.AssignRoleAsync(user.Id, "Admin");
    app.Logger.LogInformation("Admin user {Email} seeded with Admin role.", email);
}

