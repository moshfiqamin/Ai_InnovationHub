// ============================================================
// FILE   : Program.cs
// LAYER  : Application entry point / composition root
// PURPOSE: Wires everything together — database, authentication,
//          CORS, dependency injection and the HTTP pipeline.
// MODULES SERVED: M1 (Authentication), M3 (Dashboard)
// ============================================================
using System.Text;
using AiInnovationHub.Api.Data;
using AiInnovationHub.Api.Models.DTOs;
using AiInnovationHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. DATABASE — EF Core + PostgreSQL  (Model layer)
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================================
// 2. APPLICATION SERVICES (dependency injection)
// Registered against interfaces so implementations can be swapped
// without touching controllers (NFR8, NFR11).
// ============================================================
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();  // M3
builder.Services.AddScoped<IFeedService, FeedService>();            // M4
builder.Services.AddScoped<IIdeaService, IdeaService>();            // M5
builder.Services.AddScoped<IProjectService, ProjectService>();      // M6
builder.Services.AddScoped<ICommunityService, CommunityService>();  // M7
builder.Services.AddScoped<ISearchService, SearchService>();        // M8  F6
builder.Services.AddScoped<IChallengeService, ChallengeService>();  // M9
builder.Services.AddScoped<IEngagementService, EngagementService>();// M10
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();  // M11
builder.Services.AddScoped<INotificationService, NotificationService>(); // M12
builder.Services.AddScoped<IProfileService, ProfileService>();      // M13
builder.Services.AddScoped<IBadgeService, BadgeService>();          // M13 F16
builder.Services.AddScoped<IAdminService, AdminService>();          // M14
builder.Services.AddScoped<IModerationService, ModerationService>();// M14 F20

// ---- AI PROVIDERS ----
// Both concrete providers are registered with their own HttpClient, then
// ResilientAiProvider composes them: Gemini first, Groq on failure.
// Everything downstream depends only on IAiProvider (NFR11).
builder.Services.AddHttpClient<GeminiAiProvider>();
builder.Services.AddHttpClient<GroqAiProvider>();
builder.Services.AddScoped<IAiProvider, ResilientAiProvider>();

// ============================================================
// 3. AUTHENTICATION — JWT bearer tokens  (M1)
// ============================================================
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Rules every incoming token must satisfy before [Authorize] passes.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,   // rejects expired tokens (NFR14)
            ValidateIssuerSigningKey = true,
            ValidIssuer   = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero,         // no grace period past expiry
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// 4. CORS — allow the Vite dev server to call this API
// ============================================================
const string DevCors = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ============================================================
// 5. CONTROLLERS + OpenAPI
// ============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ---- CONSISTENT VALIDATION ERRORS (NFR6) ----
// By default [ApiController] returns RFC9110 problem+json with an "errors"
// dictionary, which does not match the { "message": "..." } shape the React
// layer reads via err.response.data.message. Overriding the factory means a
// failed [Required]/[MinLength] check surfaces its real message in the UI
// instead of a generic fallback.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg))
            ?? "Please check the form and try again.";

        return new BadRequestObjectResult(new ErrorResponse(firstError));
    };
});

var app = builder.Build();

// ============================================================
// 6. AUTO-MIGRATE THE DATABASE ON STARTUP
// Applies any pending EF Core migrations so the developer does not
// have to run 'dotnet ef database update' by hand each time.
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        app.Logger.LogInformation("Database is up to date.");

        // Seed the F16 badge catalogue if it is missing (idempotent).
        var badges = scope.ServiceProvider.GetRequiredService<IBadgeService>();
        badges.SeedAsync().GetAwaiter().GetResult();
        app.Logger.LogInformation("Badge catalogue seeded.");
    }
    catch (Exception ex)
    {
        // Do not crash the API if Postgres is down — log clearly instead (NFR10).
        app.Logger.LogError(ex, "Database migration failed. Is PostgreSQL running?");
    }
}

// ============================================================
// 7. HTTP PIPELINE — order matters
// ============================================================
app.UseCors(DevCors);
app.UseAuthentication();   // who are you?  (must come before authorization)
app.UseAuthorization();    // are you allowed?
app.MapControllers();

// Simple health endpoint so you can confirm the API is alive in a browser.
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "AI_InnovationHub API",
    modules = new[] { "M1 Auth", "M2 Landing", "M3 Dashboard", "M4 Feed", "M5 Ideas", "M6 Projects",
                      "M7 Community", "M8 AI", "M9 Challenges", "M10 Mentor/Investor",
                      "M11 Analytics", "M12 Notifications", "M13 Profile", "M14 Admin" },
    time = DateTime.UtcNow,
}));

app.Run();
