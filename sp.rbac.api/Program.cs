using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SP.RBAC.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SP.RBAC.API", Version = "v1" });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

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

var app = builder.Build();

// Seed the database with sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RbacDbContext>();
    await SeedDatabase(context);
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
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

// Seed database with sample data
static async Task SeedDatabase(RbacDbContext context)
{
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Check if data already exists
    if (await context.IntegrationSystems.AnyAsync())
    {
        return; // Database already seeded
    }

    // Seed Integration Systems
    var hrSystem = new SP.RBAC.API.Entities.IntegrationSystem
    {
        Name = "HR_System",
        DisplayName = "Human Resources System",
        Description = "Corporate HR management system",
        SystemType = "HR",
        SystemVersion = "2.1.0",
        ConnectionString = "Server=hr-db;Database=HRMS;Trusted_Connection=true;",
        AuthenticationType = SP.RBAC.API.Entities.AuthenticationType.Database,
        IsActive = true,
        Configuration = """{"syncInterval": "daily", "batchSize": 1000}"""
    };

    var emrSystem = new SP.RBAC.API.Entities.IntegrationSystem
    {
        Name = "EMR_System",
        DisplayName = "Electronic Medical Records",
        Description = "Hospital EMR system",
        SystemType = "EMR",
        SystemVersion = "3.0.1",
        ConnectionString = "Server=emr-db;Database=EMR;Trusted_Connection=true;",
        AuthenticationType = SP.RBAC.API.Entities.AuthenticationType.LDAP,
        IsActive = true,
        Configuration = """{"syncInterval": "hourly", "departments": ["Cardiology", "Emergency"]}"""
    };

    context.IntegrationSystems.AddRange(hrSystem, emrSystem);
    await context.SaveChangesAsync();

    // Seed Entity Definitions
    var userDefinition = new SP.RBAC.API.Entities.EntityDefinition
    {
        IntegrationSystemId = hrSystem.Id,
        Name = "User",
        DisplayName = "System User",
        Description = "Employee/user records from HR system",
        TableName = "employees",
        PrimaryKeyField = "emp_id",
        IsActive = true,
        SortOrder = 1,
        Metadata = """{"syncTable": "employees", "lastModifiedField": "updated_at"}"""
    };

    var roleDefinition = new SP.RBAC.API.Entities.EntityDefinition
    {
        IntegrationSystemId = hrSystem.Id,
        Name = "Role",
        DisplayName = "Job Role",
        Description = "Job roles and positions",
        TableName = "job_roles",
        PrimaryKeyField = "role_id",
        IsActive = true,
        SortOrder = 2,
        Metadata = """{"syncTable": "job_roles", "hierarchical": true}"""
    };

    context.EntityDefinitions.AddRange(userDefinition, roleDefinition);
    await context.SaveChangesAsync();

    // Seed Property Definitions for User
    var userProperties = new[]
    {
        new SP.RBAC.API.Entities.PropertyDefinition
        {
            EntityDefinitionId = userDefinition.Id,
            Name = "EmployeeId",
            DisplayName = "Employee ID",
            Description = "Unique employee identifier",
            DataType = SP.RBAC.API.Entities.DataType.String,
            SourceField = "emp_id",
            IsRequired = true,
            IsUnique = true,
            IsSearchable = true,
            SortOrder = 1
        },
        new SP.RBAC.API.Entities.PropertyDefinition
        {
            EntityDefinitionId = userDefinition.Id,
            Name = "FirstName",
            DisplayName = "First Name",
            Description = "Employee's first name",
            DataType = SP.RBAC.API.Entities.DataType.String,
            SourceField = "first_name",
            IsRequired = true,
            IsSearchable = true,
            SortOrder = 2
        },
        new SP.RBAC.API.Entities.PropertyDefinition
        {
            EntityDefinitionId = userDefinition.Id,
            Name = "LastName",
            DisplayName = "Last Name",
            Description = "Employee's last name",
            DataType = SP.RBAC.API.Entities.DataType.String,
            SourceField = "last_name",
            IsRequired = true,
            IsSearchable = true,
            SortOrder = 3
        },
        new SP.RBAC.API.Entities.PropertyDefinition
        {
            EntityDefinitionId = userDefinition.Id,
            Name = "Email",
            DisplayName = "Email Address",
            Description = "Employee's email address",
            DataType = SP.RBAC.API.Entities.DataType.Email,
            SourceField = "email",
            IsRequired = true,
            IsUnique = true,
            IsSearchable = true,
            SortOrder = 4
        },
        new SP.RBAC.API.Entities.PropertyDefinition
        {
            EntityDefinitionId = userDefinition.Id,
            Name = "Department",
            DisplayName = "Department",
            Description = "Employee's department",
            DataType = SP.RBAC.API.Entities.DataType.String,
            SourceField = "department",
            IsRequired = true,
            IsSearchable = true,
            SortOrder = 5
        }
    };

    context.PropertyDefinitions.AddRange(userProperties);
    await context.SaveChangesAsync();

    // Seed sample Entity Instances
    var johnDoe = new SP.RBAC.API.Entities.EntityInstance
    {
        EntityDefinitionId = userDefinition.Id,
        ExternalId = "EMP001",
        DisplayName = "John Doe",
        IsActive = true,
        SyncStatus = SP.RBAC.API.Entities.SyncStatus.Success,
        LastSyncedAt = DateTime.UtcNow.AddDays(-1),
        RawData = """{"emp_id": "EMP001", "first_name": "John", "last_name": "Doe", "email": "john.doe@hospital.com", "department": "IT"}"""
    };

    var janeDoe = new SP.RBAC.API.Entities.EntityInstance
    {
        EntityDefinitionId = userDefinition.Id,
        ExternalId = "EMP002",
        DisplayName = "Jane Smith",
        IsActive = true,
        SyncStatus = SP.RBAC.API.Entities.SyncStatus.Success,
        LastSyncedAt = DateTime.UtcNow.AddDays(-1),
        RawData = """{"emp_id": "EMP002", "first_name": "Jane", "last_name": "Smith", "email": "jane.smith@hospital.com", "department": "HR"}"""
    };

    context.EntityInstances.AddRange(johnDoe, janeDoe);
    await context.SaveChangesAsync();

    // Seed Property Values for John Doe
    var johnDoeProperties = new[]
    {
        new SP.RBAC.API.Entities.PropertyValue
        {
            EntityInstanceId = johnDoe.Id,
            PropertyDefinitionId = userProperties[0].Id, // EmployeeId
            Value = "EMP001",
            DisplayValue = "EMP001"
        },
        new SP.RBAC.API.Entities.PropertyValue
        {
            EntityInstanceId = johnDoe.Id,
            PropertyDefinitionId = userProperties[1].Id, // FirstName
            Value = "John",
            DisplayValue = "John"
        },
        new SP.RBAC.API.Entities.PropertyValue
        {
            EntityInstanceId = johnDoe.Id,
            PropertyDefinitionId = userProperties[2].Id, // LastName
            Value = "Doe",
            DisplayValue = "Doe"
        },
        new SP.RBAC.API.Entities.PropertyValue
        {
            EntityInstanceId = johnDoe.Id,
            PropertyDefinitionId = userProperties[3].Id, // Email
            Value = "john.doe@hospital.com",
            DisplayValue = "john.doe@hospital.com"
        },
        new SP.RBAC.API.Entities.PropertyValue
        {
            EntityInstanceId = johnDoe.Id,
            PropertyDefinitionId = userProperties[4].Id, // Department
            Value = "IT",
            DisplayValue = "IT Department"
        }
    };

    context.PropertyValues.AddRange(johnDoeProperties);
    await context.SaveChangesAsync();
}
