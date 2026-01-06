using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

Env.Load(); // load .env

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=/data/Ecommerce.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Controllers + Views
builder.Services.AddControllersWithViews();

// Email Settings
var emailSettings = new EmailSettings
{
    SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com",
    SmtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587"),
    SmtpUser = Environment.GetEnvironmentVariable("EMAIL_USER") ?? "",
    SmtpPass = Environment.GetEnvironmentVariable("EMAIL_PASS") ?? "",
    FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? "",
    FromName = Environment.GetEnvironmentVariable("FROM_NAME") ?? "BAZARIO"
};

builder.Services.Configure<EmailSettings>(opt =>
{
    opt.SmtpHost = emailSettings.SmtpHost;
    opt.SmtpPort = emailSettings.SmtpPort;
    opt.SmtpUser = emailSettings.SmtpUser;
    opt.SmtpPass = emailSettings.SmtpPass;
    opt.FromEmail = emailSettings.FromEmail;
    opt.FromName = emailSettings.FromName;
});

// Register EmailService
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Railway port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
