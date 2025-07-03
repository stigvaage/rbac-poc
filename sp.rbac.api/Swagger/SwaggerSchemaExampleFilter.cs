using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SP.RBAC.API.DTOs;

namespace SP.RBAC.API.Swagger;

/// <summary>
/// Schema filter to add examples to request and response models in Swagger
/// </summary>
public class SwaggerSchemaExampleFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Integration System DTOs
        if (context.Type == typeof(CreateIntegrationSystemDto))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("ERP_System"),
                ["displayName"] = new OpenApiString("Enterprise Resource Planning System"),
                ["description"] = new OpenApiString("Core ERP system managing business processes and data"),
                ["systemType"] = new OpenApiString("ERP"),
                ["systemVersion"] = new OpenApiString("2024.2"),
                ["connectionString"] = new OpenApiString("Server=erp-db;Database=ERPSystem;Integrated Security=true;"),
                ["authenticationType"] = new OpenApiInteger(1),
                ["isActive"] = new OpenApiBoolean(true),
                ["configuration"] = new OpenApiString("{\"batchSize\": 1000, \"timeout\": 300}")
            };
        }
        
        if (context.Type == typeof(UpdateIntegrationSystemDto))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("HR_System_Updated"),
                ["displayName"] = new OpenApiString("Human Resources Management System - Updated"),
                ["description"] = new OpenApiString("Updated core HR system with enhanced features"),
                ["systemType"] = new OpenApiString("HR"),
                ["systemVersion"] = new OpenApiString("2024.3"),
                ["connectionString"] = new OpenApiString("Server=hr-db-new;Database=HRSystem;Integrated Security=true;"),
                ["authenticationType"] = new OpenApiInteger(2),
                ["isActive"] = new OpenApiBoolean(true),
                ["configuration"] = new OpenApiString("{\"batchSize\": 2000, \"timeout\": 600, \"retryAttempts\": 5}"),
                ["lastModifiedReason"] = new OpenApiString("Updated connection string and performance settings")
            };
        }

        // Entity Definition DTOs
        if (context.Type == typeof(CreateEntityDefinitionDto))
        {
            schema.Example = new OpenApiObject
            {
                ["integrationSystemId"] = new OpenApiString("123e4567-e89b-12d3-a456-426614174000"),
                ["name"] = new OpenApiString("Employee"),
                ["displayName"] = new OpenApiString("Employee Records"),
                ["description"] = new OpenApiString("Employee entity from HR system"),
                ["tableName"] = new OpenApiString("Employees"),
                ["primaryKeyField"] = new OpenApiString("EmployeeId"),
                ["isActive"] = new OpenApiBoolean(true),
                ["sortOrder"] = new OpenApiInteger(1),
                ["metadata"] = new OpenApiString("{\"syncFrequency\": \"daily\", \"batchSize\": 500}")
            };
        }

        // Property Definition DTOs
        if (context.Type == typeof(CreatePropertyDefinitionDto))
        {
            schema.Example = new OpenApiObject
            {
                ["entityDefinitionId"] = new OpenApiString("456e7890-e89b-12d3-a456-426614174001"),
                ["name"] = new OpenApiString("FirstName"),
                ["displayName"] = new OpenApiString("First Name"),
                ["description"] = new OpenApiString("Employee's first name"),
                ["dataType"] = new OpenApiInteger(0),
                ["sourceField"] = new OpenApiString("first_name"),
                ["isRequired"] = new OpenApiBoolean(true),
                ["isUnique"] = new OpenApiBoolean(false),
                ["isSearchable"] = new OpenApiBoolean(true),
                ["isDisplayed"] = new OpenApiBoolean(true),
                ["isEditable"] = new OpenApiBoolean(true),
                ["sortOrder"] = new OpenApiInteger(1),
                ["defaultValue"] = new OpenApiString(""),
                ["validationRules"] = new OpenApiString("{\"minLength\": 2, \"maxLength\": 50}"),
                ["uiMetadata"] = new OpenApiString("{\"inputType\": \"text\", \"placeholder\": \"Enter first name\"}")
            };
        }

        // Entity Instance DTOs
        if (context.Type == typeof(CreateEntityInstanceDto))
        {
            schema.Example = new OpenApiObject
            {
                ["entityDefinitionId"] = new OpenApiString("456e7890-e89b-12d3-a456-426614174001"),
                ["externalId"] = new OpenApiString("EMP001"),
                ["displayName"] = new OpenApiString("John Doe"),
                ["isActive"] = new OpenApiBoolean(true),
                ["rawData"] = new OpenApiString("{\"employee_id\": \"EMP001\", \"department\": \"Engineering\", \"location\": \"Seattle\"}")
            };
        }

        // Common response DTOs
        if (context.Type == typeof(IntegrationSystemDto))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiString("123e4567-e89b-12d3-a456-426614174000"),
                ["name"] = new OpenApiString("HR_System"),
                ["displayName"] = new OpenApiString("Human Resources System"),
                ["description"] = new OpenApiString("Core HR system managing employee records"),
                ["systemType"] = new OpenApiString("HR"),
                ["systemVersion"] = new OpenApiString("2024.1"),
                ["connectionString"] = new OpenApiString("Server=hr-db;Database=HRSystem;Integrated Security=true;"),
                ["authenticationType"] = new OpenApiInteger(0),
                ["isActive"] = new OpenApiBoolean(true),
                ["lastSync"] = new OpenApiString("2025-07-03T17:35:05Z"),
                ["lastSyncStatus"] = new OpenApiInteger(1),
                ["configuration"] = new OpenApiString("{\"batchSize\": 1000, \"timeout\": 300}"),
                ["createdAt"] = new OpenApiString("2025-07-03T17:35:05Z"),
                ["createdBy"] = new OpenApiString("system"),
                ["lastModifiedAt"] = new OpenApiString("2025-07-03T17:35:05Z"),
                ["lastModifiedBy"] = new OpenApiString("system")
            };
        }

        // Paged result examples
        if (context.Type.IsGenericType && context.Type.GetGenericTypeDefinition() == typeof(PagedResult<>))
        {
            var itemType = context.Type.GetGenericArguments()[0];
            if (itemType == typeof(IntegrationSystemDto))
            {
                schema.Example = new OpenApiObject
                {
                    ["items"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString("123e4567-e89b-12d3-a456-426614174000"),
                            ["name"] = new OpenApiString("HR_System"),
                            ["displayName"] = new OpenApiString("Human Resources System"),
                            ["systemType"] = new OpenApiString("HR"),
                            ["isActive"] = new OpenApiBoolean(true)
                        }
                    },
                    ["totalCount"] = new OpenApiInteger(25),
                    ["pageNumber"] = new OpenApiInteger(1),
                    ["pageSize"] = new OpenApiInteger(10),
                    ["totalPages"] = new OpenApiInteger(3),
                    ["hasNextPage"] = new OpenApiBoolean(true),
                    ["hasPreviousPage"] = new OpenApiBoolean(false)
                };
            }
        }
    }
}
