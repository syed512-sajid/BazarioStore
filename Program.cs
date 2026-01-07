using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
// DATA PROTECTION (Fix Session Cookie Errors)
// ===============================
var dataProtectionPath = Path.Combine(dbPath, "DataProtection-Keys");
if (!Directory.Exists(dataProtectionPath)) Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("EcommerceStore");
// ===============================
// CONTROLLERS + VIEWS
// ===============================
builder.Services.AddControllersWithViews();

// ===============================
// EMAIL SETTINGS
// ===============================


// ===============================
// EMAIL SERVICES (NEW)
// ===============================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<BackgroundEmailService>();

// ===============================
// BUILD APP
// ===============================
var app = builder.Build();

// ===============================
// APPLY MIGRATIONS AND SEED ADMIN USER
// ===============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

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
        if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, "Admin");
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

app.Run();
