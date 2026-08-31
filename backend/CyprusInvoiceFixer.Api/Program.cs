using CyprusInvoiceFixer.Core.Data;
using CyprusInvoiceFixer.Core.Services;
using CyprusInvoiceFixer.Core.Services.AI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ========== Serilog ==========
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ========== Database ==========
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbUrl));

// ========== Redis ==========
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisUrl));

// ========== JWT ==========
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT_SECRET is required");
var jwtIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "CyprusInvoiceFixer";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CyprusInvoiceFixerUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            NameClaimType            = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });
builder.Services.AddAuthorization();

// ========== CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return uri.Host is "localhost" or "127.0.0.1" or "::1";
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ========== Services ==========
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IStripeService, StripeService>();

var aiProvider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "openai";
if (aiProvider == "ollama")
    builder.Services.AddScoped<IAiExtractorService, OllamaExtractorService>();
else
    builder.Services.AddScoped<IAiExtractorService, OpenAiExtractorService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// ========== Controllers + Swagger ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Cyprus Invoice Fixer API",
        Version     = "v1",
        Description = "AI-powered Cyprus VAT invoice checker and fixer"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In          = ParameterLocation.Header,
        Description = "Enter: Bearer {token}",
        Name        = "Authorization",
        Type        = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ========== Database bootstrap with retry ==========
// No Migrations folder exists in this repo, so we use EnsureCreatedAsync which
// creates all tables directly from the EF model. It is idempotent: if the tables
// already exist it does nothing.
// TODO: once you run `dotnet ef migrations add InitialCreate` and commit the
//       Migrations/ folder, switch this back to MigrateAsync().
var maxRetries = 10;
for (var attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        Log.Information("Ensuring database schema (attempt {Attempt}/{Max})...", attempt, maxRetries);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        Log.Information("Database schema ready.");
        break;
    }
    catch (Exception ex) when (attempt < maxRetries)
    {
        Log.Warning(ex, "DB bootstrap attempt {Attempt} failed. Retrying in 3s...", attempt);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}

// Global exception handler — must be first so CORS headers survive 500s
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    Log.Error(ex, "Unhandled exception");
    await ctx.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
}));

app.UseSwagger();
app.UseSwaggerUI();
app.UseSerilogRequestLogging();

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
