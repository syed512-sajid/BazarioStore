using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// LOGGING - Enhanced
// ===============================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ===============================
// DATABASE - SQLite
// ===============================
var dbPath = "/data";
if (!Directory.Exists(dbPath)) Directory.CreateDirectory(dbPath);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=/data/Ecommerce.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// ===============================
// IDENTITY
// ===============================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
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
// CONTROLLERS + VIEWS
// ===============================
builder.Services.AddControllersWithViews();

// ===============================
// EMAIL SETTINGS
// ===============================
var emailSettings = new EmailSettings
{
    SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com",
    SmtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587"),
    SmtpUser = Environment.GetEnvironmentVariable("EMAIL_USER") ?? "",
    SmtpPass = Environment.GetEnvironmentVariable("EMAIL_PASS") ?? "",
    FromEmail = Environment.GetEnvironmentVariable("EMAIL_USER") ?? "",
    FromName = Environment.GetEnvironmentVariable("FROM_NAME") ?? "BAZARIO"
};

builder.Services.Configure<EmailSettings>(options =>
{
    options.SmtpHost = emailSettings.SmtpHost;
    options.SmtpPort = emailSettings.SmtpPort;
    options.SmtpUser = emailSettings.SmtpUser;
    options.SmtpPass = emailSettings.SmtpPass;
    options.FromEmail = emailSettings.FromEmail;
    options.FromName = emailSettings.FromName;
});

// ===============================
// EMAIL SERVICE + BACKGROUND SERVICE
// ===============================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<BackgroundEmailService>();

// ===============================
// BUILD APP
// ===============================
var app = builder.Build();

// ===============================
// LOG EMAIL CONFIGURATION
// ===============================
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("===========================================");
logger.LogInformation("📧 EMAIL CONFIGURATION CHECK");
logger.LogInformation("===========================================");
logger.LogInformation("SMTP Host: {Host}", emailSettings.SmtpHost);
logger.LogInformation("SMTP Port: {Port}", emailSettings.SmtpPort);
logger.LogInformation("SMTP User: {User}", string.IsNullOrEmpty(emailSettings.SmtpUser) ? "❌ NOT SET" : $"✅ {emailSettings.SmtpUser}");
logger.LogInformation("SMTP Pass: {Pass}", string.IsNullOrEmpty(emailSettings.SmtpPass) ? "❌ NOT SET" : $"✅ SET (length: {emailSettings.SmtpPass.Length})");
logger.LogInformation("From Email: {From}", emailSettings.FromEmail);
logger.LogInformation("From Name: {Name}", emailSettings.FromName);
logger.LogInformation("===========================================");

// ===============================
// APPLY MIGRATIONS AND SEED ADMIN USER
// ===============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    
    logger.LogInformation("🔄 Applying database migrations...");
    await db.Database.MigrateAsync();
    logger.LogInformation("✅ Database ready");

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        logger.LogInformation("✅ Admin role created");
    }

    string adminEmail = "sajidabbas6024@gmail.com";
    string adminPassword = "sajid@6024";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            logger.LogInformation("✅ Admin user created: {Email}", adminEmail);
        }
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ===============================
// DEFAULT ROUTE
// ===============================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ===============================
// RAILWAY PORT
// ===============================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

logger.LogInformation("🚀 Application starting on port {Port}", port);
logger.LogInformation("🌐 Environment: {Env}", app.Environment.EnvironmentName);

app.Run();
