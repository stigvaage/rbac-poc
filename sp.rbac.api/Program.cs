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
            Title = "SP.RBAC.API - Role-Based Access Control Platform",
            Version = "v1.0.0",
            Description = @"
## Overview

A comprehensive .NET 9 Web API for managing role-based access control (RBAC) with support for:

- **Integration Systems**: External system connections (HR, EMR, CRM, Active Directory)
- **Entity Management**: Dynamic entity definitions and instances with flexible property systems
- **Access Control**: Advanced rules, assignments, and automated workflows
- **Audit & Compliance**: Complete audit trails and compliance reporting
- **Test Data**: Configurable test data seeding for development and testing

## Authentication

Currently configured for development without authentication. In production environments, 
implement JWT Bearer token authentication or OAuth2/OpenID Connect.

## Rate Limiting

No rate limiting is currently implemented. Consider implementing rate limiting for production use.

## Error Handling

All endpoints return standardized error responses with appropriate HTTP status codes:
- **400**: Bad Request (validation errors)
- **404**: Not Found
- **500**: Internal Server Error

## Pagination

List endpoints support pagination with these standard parameters:
- `pageNumber`: Page number (default: 1, minimum: 1)
- `pageSize`: Items per page (default: 10, maximum: 100)

## Getting Started

1. All endpoints are available immediately with pre-seeded test data
2. Use the Swagger UI 'Try it out' feature to test endpoints
3. Check the `/health` endpoint to verify system status
4. Explore integration systems first, then entity definitions and instances

## Support

For API support and documentation updates, contact the development team.
",
            Contact = new OpenApiContact
            {
                Name = "SP.RBAC.API Development Team",
                Email = "dev-team@company.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
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
