using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SP.RBAC.API.Swagger;

/// <summary>
/// Document filter to enhance the Swagger documentation with additional metadata and examples
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add global tags for better organization
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "Integration Systems",
                Description = "Manage external system connections (HR, EMR, CRM, Active Directory)"
            },
            new OpenApiTag
            {
                Name = "Entity Definitions",
                Description = "Define data structures and schemas for different entity types"
            },
            new OpenApiTag
            {
                Name = "Property Definitions",
                Description = "Define properties and attributes for entities with validation rules"
            },
            new OpenApiTag
            {
                Name = "Entity Instances",
                Description = "Manage actual entity records and their property values"
            },
            new OpenApiTag
            {
                Name = "Access Rules",
                Description = "Configure automated business rules for access management"
            },
            new OpenApiTag
            {
                Name = "Access Assignments",
                Description = "Manage user-role-system assignments and permissions"
            },
            new OpenApiTag
            {
                Name = "Property Values",
                Description = "Handle entity property values with history tracking"
            },
            new OpenApiTag
            {
                Name = "Sync Logs",
                Description = "Monitor synchronization activities and system integration logs"
            },
            new OpenApiTag
            {
                Name = "Health",
                Description = "System health monitoring and status checks"
            }
        };

        // Add servers information
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer
            {
                Url = "http://localhost:5109",
                Description = "Development Server (.NET CLI)"
            },
            new OpenApiServer
            {
                Url = "http://localhost:8080",
                Description = "Development Server (Docker)"
            }
        };

        // Add common response schemas for reuse
        if (swaggerDoc.Components?.Schemas != null)
        {
            swaggerDoc.Components.Schemas.Add("ApiError", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["message"] = new OpenApiSchema { Type = "string", Description = "Error message" },
                    ["details"] = new OpenApiSchema { Type = "string", Description = "Detailed error information" },
                    ["timestamp"] = new OpenApiSchema { Type = "string", Format = "date-time", Description = "Error timestamp" },
                    ["correlationId"] = new OpenApiSchema { Type = "string", Description = "Correlation ID for tracking" }
                },
                Required = new HashSet<string> { "message", "timestamp" }
            });

            swaggerDoc.Components.Schemas.Add("ValidationError", new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["field"] = new OpenApiSchema { Type = "string", Description = "Field name that failed validation" },
                    ["message"] = new OpenApiSchema { Type = "string", Description = "Validation error message" },
                    ["code"] = new OpenApiSchema { Type = "string", Description = "Validation error code" }
                },
                Required = new HashSet<string> { "field", "message" }
            });
        }
    }
}
