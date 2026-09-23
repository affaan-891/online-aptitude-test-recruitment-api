using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Webster.AptitudePortal.Api.Data;
using Webster.AptitudePortal.Api.Middlewares;
using Webster.AptitudePortal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// 1. DATABASE CONFIGURATION (SQL Server with In-Memory Zero-Config Fallback)
// ----------------------------------------------------------------------------
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AptitudeDbContext>(options =>
{
    if (useInMemory || string.IsNullOrEmpty(connectionString))
    {
        options.UseInMemoryDatabase("WebsterAptitudeDb");
    }
    else
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    }
});

// ----------------------------------------------------------------------------
// 2. DEPENDENCY INJECTION (Services & Repositories)
// ----------------------------------------------------------------------------
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<ICandidateService, CandidateService>();

// ----------------------------------------------------------------------------
// 3. AUTHENTICATION & JWT BEARER CONFIGURATION
// ----------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? "WebsterEnterpriseAptitudeSystemSuperSecretKey2026!WithMinimum256BitsLength";
var issuer = jwtSettings["Issuer"] ?? "Webster.AptitudePortal";
var audience = jwtSettings["Audience"] ?? "Webster.Applicants";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ----------------------------------------------------------------------------
// 4. CONTROLLERS & CORS
// ----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ----------------------------------------------------------------------------
// 5. SWAGGER / OPENAPI SPECIFICATION WITH JWT SUPPORT
// ----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Webster Organisation - Online Aptitude Test & Recruitment API",
        Version = "v1",
        Description = "Enterprise-grade 3-Tier assessment and candidate recruitment portal built with .NET 8, SQL Server 3NF, linear stage gating, and automated HR pipeline transfer.",
        Contact = new OpenApiContact
        {
            Name = "Webster Talent Acquisition",
            Email = "recruitment@webster.org",
            Url = new Uri("https://github.com/affaan-891/online-aptitude-test-recruitment-api")
        }
    });

    // JWT Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token format: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ----------------------------------------------------------------------------
// 6. DATABASE SEEDING & RESILIENCE HOOK
// ----------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AptitudeDbContext>();
        await DbInitializer.InitializeAsync(db);
        logger.LogInformation("Database initialized and seeded with 3 sections, 15 questions, candidates, and HR pipeline.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization encountered an error. If SQL Server is offline, enable 'UseInMemoryDatabase: true' in appsettings.json.");
    }
}

// ----------------------------------------------------------------------------
// 7. REQUEST PIPELINE MIDDLEWARES
// ----------------------------------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Webster Aptitude Portal API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at application root (http://localhost:5000/)
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
