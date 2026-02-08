using Microsoft.EntityFrameworkCore;
using Nivora.Identity.Abstractions;
using Nivora.Identity.Endpoints;
using Nivora.IdentityManager.Data;
using Nivora.IdentityManager.Helpers;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Nivora Identity
builder.Services.AddNivoraIdentity<AppDbContext>(
    builder.Configuration.GetSection("NivoraIdentity"));

// Razor Pages + Session
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// HttpClient for IdentityApiClient (calls back into this app)
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IdentityApiClient>();

var app = builder.Build();

// Seed admin user
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
app.MapNivoraIdentityEndpoints();
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
        return;

    await admin.CreateUserAsync(email, password, confirmEmail: true);
    app.Logger.LogInformation("Admin user {Email} seeded.", email);
}

