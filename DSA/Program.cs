using DSA.API.Extensions;
using DSA.API.Middleware;
using DSA.Infrastructure;
using DSA.Infrastructure.Content.Sources;
using DSA.Infrastructure.Content;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using DSA.Core.Interfaces;
using DSA.Infrastructure.Services;
using System;
using DSA.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Register repositories
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IUserProgressRepository, UserProgressRepository>();

// Register services
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<RankingService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJsApp", builder =>
    {
        var origins = new[]
        {
            "https://dsa-frontend-nextjs-pkh4.vercel.app",
            "http://localhost:3000",
            "http://localhost:5178" // Dodaj port developerski
        };

        builder.WithOrigins(origins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()
               .SetPreflightMaxAge(TimeSpan.FromMinutes(10))
               .WithExposedHeaders("Content-Disposition", "Authorization");
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DSA Learning API",
        Version = "v1",
        Description = "API dla aplikacji w stylu Duolingo do nauki Data Structures and Algorithms",
        Contact = new OpenApiContact
        {
            Name = "DSA Learning Team",
            Email = "contact@dsalearning.com",
            Url = new Uri("https://dsalearning.com")
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
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
            new string[] {}
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
var herokuDatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(herokuDatabaseUrl))
{
    var databaseUri = new Uri(herokuDatabaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
}
Console.WriteLine($"Using connection string: {connectionString}");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Auth services
builder.Services.AddAuthServices(builder.Configuration);

// Configure authentication with cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddSingleton<ContentProvider>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        var contentProvider = services.GetRequiredService<ContentProvider>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var jsonFileLogger = loggerFactory.CreateLogger<JsonFileContentSource>();
        contentProvider.RegisterContentSource(new JsonFileContentSource(Path.Combine(builder.Environment.ContentRootPath, "SeedData"), jsonFileLogger));
        var characterEncodingLogger = services.GetRequiredService<ILogger<CharacterEncodingAdapter>>();
        contentProvider.RegisterContentSource(new CharacterEncodingAdapter(characterEncodingLogger));
        var stackQueueLogger = services.GetRequiredService<ILogger<StackQueueAdapter>>();
        contentProvider.RegisterContentSource(new StackQueueAdapter(stackQueueLogger));

        var contentContext = new ContentContext(dbContext);
        await contentProvider.LoadAllContentAsync(contentContext);

        if (contentContext.ValidationReport.HasErrors)
        {
            logger.LogWarning("Content loading completed with errors.");
            foreach (var issue in contentContext.ValidationReport.Issues.Where(i => i.Severity == ContentIssueSeverity.Error))
            {
                logger.LogError($"[{issue.Source}] {issue.Message}");
            }
        }
        else
        {
            logger.LogInformation("Successfully loaded all educational content");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during ContentProvider initialization");
    }
}

// Middleware pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DSA Learning API v1");
    c.RoutePrefix = string.Empty;
    c.OAuthUsePkce();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
});

app.UseHttpsRedirection();


app.UseRouting();
app.UseCors("AllowNextJsApp"); // Moved before UseRouting, removed custom CorsMiddleware

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SecureHeadersMiddleware>();

app.MapControllers();

// Auto migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();