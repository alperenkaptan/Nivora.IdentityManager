using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Abstractions;
using Nivora.IdentityManager.Auth;
using Nivora.IdentityManager.Data;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Nivora Identity (facade + admin service registered automatically)
builder.Services.AddNivoraIdentity<AppDbContext>(
    builder.Configuration.GetSection("NivoraIdentity"));

// Cookie Authentication (overrides JWT Bearer as default scheme)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.AccessDeniedPath = "/Login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });

// Ensure cookie scheme wins even if AddNivoraIdentity set JWT as default
builder.Services.PostConfigure<AuthenticationOptions>(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization();

// Role claims transformation — refreshes roles from DB with short cache TTL
builder.Services.AddMemoryCache();
builder.Services.AddTransient<IClaimsTransformation, RoleClaimsTransformation>();

// Razor Pages + Session (session kept for 2FA challenge flow)
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
