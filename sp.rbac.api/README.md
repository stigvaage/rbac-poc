# SP.RBAC.API - Role-Based Access Control API

A comprehensive .NET 9 Web API for managing role-based access control with support for integration systems, entity definitions, property definitions, and entity instances.

## Features

- **Integration Systems Management**: CRUD operations for external systems (HR, EMR, CRM, etc.)
- **Entity Definitions**: Define data structures for entities like Users, Roles, Departments
- **Property Definitions**: Define properties/attributes for entities with various data types
- **Entity Instances**: Manage actual entity records with property values (EAV pattern)
- **Pagination Support**: All list endpoints support pagination
- **In-Memory Database**: Uses Entity Framework Core with In-Memory database for easy testing
- **AutoMapper Integration**: Automatic mapping between entities and DTOs
- **Swagger Documentation**: Interactive API documentation at root URL
- **Sample Data**: Pre-seeded with sample integration systems and entity data

## Technology Stack

- **.NET 9**: Latest .NET framework
- **ASP.NET Core Web API**: RESTful API framework
- **Entity Framework Core**: ORM with In-Memory database provider
- **AutoMapper**: Object-to-object mapping
- **Swagger/OpenAPI**: API documentation
- **CORS**: Cross-origin resource sharing enabled

## Project Structure

```
SP.RBAC.API/
├── Controllers/           # API controllers
│   ├── IntegrationSystemsController.cs
│   ├── EntityDefinitionsController.cs
│   ├── PropertyDefinitionsController.cs
│   └── EntityInstancesController.cs
├── Data/                  # Data context and mappings
│   ├── RbacDbContext.cs
│   └── MappingProfile.cs
├── DTOs/                  # Data Transfer Objects
│   ├── CommonDTOs.cs
│   ├── IntegrationSystemDTOs.cs
│   ├── EntityDefinitionDTOs.cs
│   ├── PropertyDefinitionDTOs.cs
│   └── EntityInstanceDTOs.cs
├── Entities/              # Domain entities
│   ├── BaseEntity.cs
│   ├── BaseAuditableEntity.cs
│   ├── Enums.cs
│   ├── IntegrationSystem.cs
│   ├── EntityDefinition.cs
│   ├── PropertyDefinition.cs
│   ├── EntityInstance.cs
│   ├── PropertyValue.cs
│   ├── AccessRule.cs
│   ├── AccessAssignment.cs
│   └── SyncLog.cs
└── Program.cs            # Application configuration and startup
```

## API Endpoints

### Integration Systems
- `GET /api/IntegrationSystems` - List integration systems with pagination
- `GET /api/IntegrationSystems/{id}` - Get specific integration system
- `POST /api/IntegrationSystems` - Create new integration system
- `PUT /api/IntegrationSystems/{id}` - Update integration system
- `DELETE /api/IntegrationSystems/{id}` - Delete integration system (soft delete)
- `POST /api/IntegrationSystems/{id}/test-connection` - Test system connection

### Entity Definitions
- `GET /api/EntityDefinitions` - List entity definitions with pagination
- `GET /api/EntityDefinitions/{id}` - Get specific entity definition
- `POST /api/EntityDefinitions` - Create new entity definition
- `PUT /api/EntityDefinitions/{id}` - Update entity definition
- `DELETE /api/EntityDefinitions/{id}` - Delete entity definition (soft delete)
- `GET /api/EntityDefinitions/{id}/property-definitions` - Get property definitions for entity

### Property Definitions
- `GET /api/PropertyDefinitions` - List property definitions with pagination
- `GET /api/PropertyDefinitions/{id}` - Get specific property definition
- `POST /api/PropertyDefinitions` - Create new property definition
- `PUT /api/PropertyDefinitions/{id}` - Update property definition
- `DELETE /api/PropertyDefinitions/{id}` - Delete property definition (soft delete)
- `GET /api/PropertyDefinitions/data-types` - Get available data types

### Entity Instances
- `GET /api/EntityInstances` - List entity instances with pagination
- `GET /api/EntityInstances/{id}` - Get specific entity instance
- `POST /api/EntityInstances` - Create new entity instance
- `PUT /api/EntityInstances/{id}` - Update entity instance
- `DELETE /api/EntityInstances/{id}` - Delete entity instance (soft delete)

## Data Model

### Core Entities

1. **IntegrationSystem**: External systems to integrate with
2. **EntityDefinition**: Defines structure of entities within a system
3. **PropertyDefinition**: Defines properties/attributes for entities
4. **EntityInstance**: Actual instances of entities
5. **PropertyValue**: Values for entity properties (EAV pattern)
6. **AccessRule**: Business rules for access assignment
7. **AccessAssignment**: User-role-system mappings
8. **SyncLog**: Synchronization activity logs

### Supported Data Types

- String, Integer, Decimal, Boolean
- DateTime, Date, Time
- Email, Phone, Url
- List, Json

### Authentication Types

- Database, LDAP, OAuth2, SAML, JWT, ApiKey

## Getting Started

### Prerequisites

- .NET 9 SDK
- IDE (Visual Studio, VS Code, JetBrains Rider)

### Running the Application

1. Clone or navigate to the project directory:
   ```bash
   cd SP.RBAC.API
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. Open your browser and navigate to `http://localhost:5109` to access the Swagger UI

### Sample Data

The application automatically seeds sample data on startup:

- **HR System**: Human Resources integration system
- **EMR System**: Electronic Medical Records system
- **User Entity Definition**: With properties like EmployeeId, FirstName, LastName, Email, Department
- **Sample Users**: John Doe and Jane Smith with property values

## API Usage Examples

### Create Integration System

```bash
curl -X POST "http://localhost:5109/api/IntegrationSystems" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "CRM_System",
    "displayName": "Customer Relationship Management",
    "description": "Sales and customer management system",
    "systemType": "CRM",
    "systemVersion": "1.5.2",
    "connectionString": "Server=crm-db;Database=CRM;Trusted_Connection=true;",
    "authenticationType": 2,
    "isActive": true,
    "configuration": "{\"syncInterval\": \"weekly\"}"
  }'
```

### Get Integration Systems

```bash
curl "http://localhost:5109/api/IntegrationSystems?pageNumber=1&pageSize=10"
```

### Create Entity Definition

```bash
curl -X POST "http://localhost:5109/api/EntityDefinitions" \
  -H "Content-Type: application/json" \
  -d '{
    "integrationSystemId": "your-integration-system-id",
    "name": "Customer",
    "displayName": "Customer Record",
    "description": "Customer information from CRM",
    "tableName": "customers",
    "primaryKeyField": "customer_id",
    "isActive": true,
    "sortOrder": 1
  }'
```

## Configuration

### Database

By default, the application uses Entity Framework Core In-Memory database for easy testing. To use SQL Server:

1. Add connection string to `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=RBAC;Trusted_Connection=true;"
     }
   }
   ```

2. The application will automatically use SQL Server when a connection string is provided.

### CORS

CORS is enabled with an "AllowAll" policy for development. Update the CORS configuration in `Program.cs` for production use.

## Features & Capabilities

- **Soft Delete**: All entities support soft delete (IsDeleted flag)
- **Audit Trail**: Track creation/modification timestamps and users
- **Optimistic Concurrency**: Version control with RowVersion
- **Validation**: Entity validation and business rule enforcement
- **Pagination**: Consistent pagination across all list endpoints
- **Filtering**: Search and filter capabilities on list endpoints
- **Error Handling**: Comprehensive error handling with appropriate HTTP status codes
- **Logging**: Structured logging throughout the application

## Future Enhancements

- Authentication/Authorization (JWT tokens)
- Real integration system connectors
- Background synchronization services
- Advanced access rule engine
- Audit log endpoints
- Bulk operations
- Export/Import functionality
- Real-time notifications

## Contributing

This is a demonstration project showcasing:
- Clean architecture principles
- RESTful API design
- Entity Framework Core best practices
- AutoMapper usage
- Comprehensive CRUD operations
- Proper error handling and validation

The project serves as a foundation for building more complex role-based access control systems with integration capabilities.
