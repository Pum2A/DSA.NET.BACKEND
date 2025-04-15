using DSA.API.Extensions;
using DSA.API.Middleware;
using DSA.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJsApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Rozszerzona konfiguracja Swaggera
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

    // Konfiguracja uwierzytelniania JWT w Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\""
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

    // Opcjonalnie: dodanie komentarzy XML do dokumentacji Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));

// Add Auth services
builder.Services.AddAuthServices(builder.Configuration);

// Poprawiona konfiguracja CORS dla obs³ugi HttpOnly cookies

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None; // Dla cross-origin
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Podczas developmentu
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });


var app = builder.Build();

// Configure the HTTP request pipeline


    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DSA Learning API v1");
        c.RoutePrefix = string.Empty; // Ustaw Swagger UI jako stronê g³ówn¹

        // Konfiguracja wygl¹du
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1); // Ukryj sekcjê Models
        c.DisplayRequestDuration(); // Poka¿ czas trwania ¿¹dañ
    });

// Konfiguracja dla œrodowiska developerskiego
// Zakomentuj tê liniê, jeœli masz problemy z certyfikatem
// app.UseHttpsRedirection();



// U¿ywanie wbudowanej obs³ugi CORS zamiast w³asnego middleware
app.UseMiddleware<CorsMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();