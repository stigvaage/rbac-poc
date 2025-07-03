using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SP.RBAC.API.DTOs;

namespace SP.RBAC.API.Swagger;

/// <summary>
/// Operation filter to add response examples to Swagger operations
/// </summary>
public class SwaggerResponseExamplesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add examples for common error responses
        if (operation.Responses.ContainsKey("400") && 
            operation.Responses["400"].Content.ContainsKey("application/json"))
        {
            operation.Responses["400"].Content["application/json"].Example = new OpenApiObject
            {
                ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.5.1"),
                ["title"] = new OpenApiString("One or more validation errors occurred."),
                ["status"] = new OpenApiInteger(400),
                ["errors"] = new OpenApiObject
                {
                    ["pageSize"] = new OpenApiArray
                    {
                        new OpenApiString("Page size must be between 1 and 100")
                    },
                    ["pageNumber"] = new OpenApiArray
                    {
                        new OpenApiString("Page number must be greater than 0")
                    }
                }
            };
        }

        if (operation.Responses.ContainsKey("404") && 
            operation.Responses["404"].Content.ContainsKey("application/json"))
        {
            operation.Responses["404"].Content["application/json"].Example = new OpenApiObject
            {
                ["message"] = new OpenApiString("Integration system not found"),
                ["details"] = new OpenApiString("No integration system exists with the specified ID"),
                ["timestamp"] = new OpenApiString("2025-07-03T15:30:00Z"),
                ["correlationId"] = new OpenApiString("abc123def456")
            };
        }

        if (operation.Responses.ContainsKey("500") && 
            operation.Responses["500"].Content.ContainsKey("application/json"))
        {
            operation.Responses["500"].Content["application/json"].Example = new OpenApiObject
            {
                ["message"] = new OpenApiString("An internal server error occurred"),
                ["details"] = new OpenApiString("Please try again later or contact support if the problem persists"),
                ["timestamp"] = new OpenApiString("2025-07-03T15:30:00Z"),
                ["correlationId"] = new OpenApiString("xyz789abc123")
            };
        }

        // Add specific examples for certain operations
        if (context.MethodInfo.Name == "GetIntegrationSystems" && 
            operation.Responses.ContainsKey("200") && 
            operation.Responses["200"].Content.ContainsKey("application/json"))
        {
            operation.Responses["200"].Content["application/json"].Example = new OpenApiObject
            {
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("006bd22c-e49f-4635-8c31-7281e9246df7"),
                        ["name"] = new OpenApiString("HR_System"),
                        ["displayName"] = new OpenApiString("Human Resources System"),
                        ["description"] = new OpenApiString("Core HR system managing employee records, departments, and organizational structure"),
                        ["systemType"] = new OpenApiString("HR"),
                        ["systemVersion"] = new OpenApiString("2024.1"),
                        ["connectionString"] = new OpenApiString("Server=hr-db;Database=HRSystem;Integrated Security=true;"),
                        ["authenticationType"] = new OpenApiInteger(0),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["lastSync"] = new OpenApiString("2025-07-02T17:35:05Z"),
                        ["configuration"] = new OpenApiString("{\"batchSize\": 1000, \"timeout\": 300, \"retryAttempts\": 3}"),
                        ["createdAt"] = new OpenApiString("2025-07-03T17:35:05Z"),
                        ["createdBy"] = new OpenApiString("system")
                    },
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("77d723af-e778-448a-b08c-8759269a6963"),
                        ["name"] = new OpenApiString("EMR_System"),
                        ["displayName"] = new OpenApiString("Electronic Medical Records"),
                        ["description"] = new OpenApiString("Electronic Medical Records system managing patient data, providers, and clinical information"),
                        ["systemType"] = new OpenApiString("EMR"),
                        ["systemVersion"] = new OpenApiString("3.2.1"),
                        ["connectionString"] = new OpenApiString("Server=emr-db;Database=EMRSystem;Integrated Security=true;"),
                        ["authenticationType"] = new OpenApiInteger(2),
                        ["isActive"] = new OpenApiBoolean(true),
                        ["lastSync"] = new OpenApiString("2025-07-03T16:35:05Z"),
                        ["configuration"] = new OpenApiString("{\"batchSize\": 500, \"timeout\": 600, \"retryAttempts\": 5, \"includePatientData\": false}"),
                        ["createdAt"] = new OpenApiString("2025-07-03T17:35:05Z"),
                        ["createdBy"] = new OpenApiString("system")
                    }
                },
                ["totalCount"] = new OpenApiInteger(3),
                ["pageNumber"] = new OpenApiInteger(1),
                ["pageSize"] = new OpenApiInteger(10),
                ["totalPages"] = new OpenApiInteger(1),
                ["hasNextPage"] = new OpenApiBoolean(false),
                ["hasPreviousPage"] = new OpenApiBoolean(false)
            };
        }

        // Add test connection response example
        if (context.MethodInfo.Name == "TestConnection" && 
            operation.Responses.ContainsKey("200") && 
            operation.Responses["200"].Content.ContainsKey("application/json"))
        {
            operation.Responses["200"].Content["application/json"].Example = new OpenApiObject
            {
                ["systemId"] = new OpenApiString("123e4567-e89b-12d3-a456-426614174000"),
                ["systemName"] = new OpenApiString("HR_System"),
                ["success"] = new OpenApiBoolean(true),
                ["message"] = new OpenApiString("Connection successful"),
                ["responseTime"] = new OpenApiInteger(150),
                ["timestamp"] = new OpenApiString("2025-07-03T15:30:00Z"),
                ["details"] = new OpenApiObject
                {
                    ["version"] = new OpenApiString("2024.1"),
                    ["endpoint"] = new OpenApiString("https://hr-api.company.com/v1/status"),
                    ["features"] = new OpenApiArray
                    {
                        new OpenApiString("employee-sync"),
                        new OpenApiString("department-hierarchy"),
                        new OpenApiString("role-assignments")
                    }
                }
            };
        }
    }
}
