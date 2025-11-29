using EventManagementSystem.Models;
using EventManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

Env.Load(); // Load .env first

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------
// 🔹 Load Environment Variables Manually
// --------------------------------------
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL") ??
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    "Host=localhost;Database=eventmanagement;Username=postgres;Password=yourpassword";

builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Email Environment Overrides
builder.Configuration["EmailSettings:SmtpServer"] = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? builder.Configuration["EmailSettings:SmtpServer"];
builder.Configuration["EmailSettings:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT") ?? builder.Configuration["EmailSettings:Port"];
builder.Configuration["EmailSettings:SenderName"] = Environment.GetEnvironmentVariable("SENDER_NAME") ?? builder.Configuration["EmailSettings:SenderName"];
builder.Configuration["EmailSettings:SenderEmail"] = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? builder.Configuration["EmailSettings:SenderEmail"];
builder.Configuration["EmailSettings:Password"] = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? builder.Configuration["EmailSettings:Password"];

// --------------------------------------
// 🔹 Services
// --------------------------------------
builder.Services.AddControllersWithViews();

// DbContext Registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(warnings =>
             warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning)
           ));

// Email Service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EmailService>();

// Other Services
builder.Services.AddScoped<VenueService>();
builder.Services.AddScoped<BookingDbService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FoodService>();
builder.Services.AddScoped<FlowerService>();
builder.Services.AddScoped<LightService>();
builder.Services.AddScoped<EquipmentService>();

// --------------------------------------
// 🔹 Authentication
// --------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToAccessDenied = context =>
            {
                if (context.HttpContext.User.IsInRole("Registrar"))
                {
                    context.Response.Redirect("/Registrar/Index");
                    return Task.CompletedTask;
                }

                context.Response.Redirect(options.AccessDeniedPath);
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// --------------------------------------
// 🔹 Middleware Pipeline
// --------------------------------------
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

// --------------------------------------
// 🔹 Auto-Apply Migrations
// --------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error migrating database.");
    }
}

// --------------------------------------
// 🔹 Routing
// --------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
