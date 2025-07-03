using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SP.RBAC.API.Data;
using SP.RBAC.API.Services;
using SP.RBAC.API.Middleware;
using SP.RBAC.API.Swagger;
using Serilog;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/sp-rbac-api-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting SP.RBAC.API application");
    
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "SP.RBAC.API - Rollebasert tilgangskontroll plattform",
            Version = "v1.0.0",
            Description = @"
## Oversikt

En omfattende .NET 9 Web API for administrasjon av rollebasert tilgangskontroll (RBAC) med støtte for:

- **Integrasjonssystemer**: Eksterne systemtilkoblinger (HR, EMR, CRM, Active Directory)
- **Entitetshåndtering**: Dynamiske entitetsdefinisjoner og instanser med fleksible egenskapssystemer
- **Tilgangskontroll**: Avanserte regler, tildelinger og automatiserte arbeidsflyter
- **Revisjon og compliance**: Komplette revisjonsspor og compliance-rapportering
- **Testdata**: Konfigurerbar testdata-seeding for utvikling og testing

## Autentisering

For øyeblikket konfigurert for utvikling uten autentisering. I produksjonsmiljøer skal 
JWT Bearer token-autentisering eller OAuth2/OpenID Connect implementeres.

## Ratebegrensning

Ingen ratebegrensning er implementert for øyeblikket. Vurder å implementere ratebegrensning for produksjonsbruk.

## Feilhåndtering

Alle endepunkter returnerer standardiserte feilsvar med passende HTTP-statuskoder:
- **400**: Ugyldig forespørsel (valideringsfeil)
- **404**: Ikke funnet
- **500**: Intern serverfeil

## Paginering

Liste-endepunkter støtter paginering med disse standardparameterne:
- `pageNumber`: Sidenummer (standard: 1, minimum: 1)
- `pageSize`: Elementer per side (standard: 10, maksimum: 100)

## Kom i gang

1. Alle endepunkter er tilgjengelige umiddelbart med forhåndsinnlastet testdata
2. Bruk Swagger UI 'Try it out'-funksjonen for å teste endepunkter
3. Sjekk `/health`-endepunktet for å verifisere systemstatus
4. Utforsk integrasjonssystemer først, deretter entitetsdefinisjoner og instanser

## Support

For API-støtte og dokumentasjonsoppdateringer, kontakt utviklingsteamet.
",
            Contact = new OpenApiContact
            {
                Name = "SP.RBAC.API Utviklingsteam",
                Email = "dev-team@company.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT Lisens",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });
        
        // Include XML comments for better API documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }

        // Configure Swagger UI settings
        c.EnableAnnotations();
        c.DocumentFilter<SwaggerDocumentFilter>();
        
        // Add examples for better documentation
        c.SchemaFilter<SwaggerSchemaExampleFilter>();
        
        // Group endpoints by tags
        c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
        c.DocInclusionPredicate((name, api) => true);
        
        // Configure response examples
        c.OperationFilter<SwaggerResponseExamplesFilter>();
    });

    // Add AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Add Entity Framework
    builder.Services.AddDbContext<RbacDbContext>(options =>
    {
        // Use In-Memory database for development/testing
        // In production, replace with SQL Server connection string
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Use In-Memory database as fallback
            options.UseInMemoryDatabase("RbacDb");
        }
        else
        {
            options.UseSqlServer(connectionString);
        }
    });

    // Register audit service
    builder.Services.AddScoped<IAuditService, AuditService>();

    // Register test data seeder
    builder.Services.Configure<TestDataSettings>(builder.Configuration.GetSection("TestData"));
    builder.Services.AddScoped<ITestDataSeeder, TestDataSeeder>();
    builder.Services.AddScoped<TestDataSettings>(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var settings = new TestDataSettings();
        configuration.GetSection("TestData").Bind(settings);
        return settings;
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<RbacDbContext>();

    var app = builder.Build();

// Seed the database with comprehensive test data
using (var scope = app.Services.CreateScope())
{
    var testDataSeeder = scope.ServiceProvider.GetRequiredService<ITestDataSeeder>();
    await testDataSeeder.SeedAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SP.RBAC.API v1");
        c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
}    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    
    // Add audit middleware before authorization
    app.UseAuditLogging();
    
    app.UseAuthorization();
    app.MapControllers();
    
    // Map health check endpoint
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
