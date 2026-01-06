using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// LOGGING
// ===============================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ===============================
// DATABASE - SQLite (Railway volume)
// ===============================
var dbPath = "/data";
if (!Directory.Exists(dbPath))
    Directory.CreateDirectory(dbPath);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=/data/Ecommerce.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// ===============================
// IDENTITY
// ===============================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ===============================
// SESSION
// ===============================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===============================
// MVC
// ===============================
builder.Services.AddControllersWithViews();

// ===============================
// EMAIL SERVICE
// ===============================
builder.Services.AddScoped<IEmailService, EmailService>();

// ===============================
// BUILD
// ===============================
var app = builder.Build();

// ===============================
// STARTUP LOGS
// ===============================
var logger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Program");

logger.LogInformation("===========================================");
logger.LogInformation("📧 EMAIL CONFIGURATION CHECK");
logger.LogInformation("===========================================");
logger.LogInformation("SMTP Host: {Host}", Environment.GetEnvironmentVariable("SMTP_HOST"));
logger.LogInformation("SMTP Port: {Port}", Environment.GetEnvironmentVariable("SMTP_PORT"));
logger.LogInformation("SMTP User: {User}",
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EMAIL_USER"))
        ? "❌ NOT SET"
        : "✅ SET");
logger.LogInformation("From Name: {Name}", Environment.GetEnvironmentVariable("FROM_NAME"));
logger.LogInformation("===========================================");

// ===============================
// MIGRATION + ADMIN SEED (SAFE)
// ===============================
using (var scope = app.Services.CreateScope())
{
    try
    {
        logger.LogInformation("🔄 Applying database migrations...");
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("✅ Database ready");

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogInformation("✅ Admin role created");
        }

        const string adminEmail = "sajidabbas6024@gmail.com";
        const string adminPassword = "sajid@6024";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("✅ Admin user created");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ Startup failed");
        throw;
    }
}

// ===============================
// MIDDLEWARE
// ===============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ❌ DO NOT USE HTTPS REDIRECT ON RAILWAY
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ===============================
// ROUTES
// ===============================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

logger.LogInformation("🚀 Application started");
logger.LogInformation("🌐 Environment: {Env}", app.Environment.EnvironmentName);

app.Run();
