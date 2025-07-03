using SP.RBAC.API.Data;
using SP.RBAC.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace SP.RBAC.API.Services;

public interface ITestDataSeeder
{
    Task SeedAsync();
}

public class TestDataSeeder : ITestDataSeeder
{
    private readonly RbacDbContext _context;
    private readonly TestDataSettings _settings;
    private readonly ILogger<TestDataSeeder> _logger;

    public TestDataSeeder(RbacDbContext context, IOptions<TestDataSettings> settings, ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (!_settings.EnableSeeding)
        {
            _logger.LogInformation("Test data seeding is disabled");
            return;
        }

        _logger.LogInformation("Starting test data seeding");

        try
        {
            // Seed basic data
            await SeedIntegrationSystemsAsync();
            await SeedEntityDefinitionsAsync();
            await SeedPropertyDefinitionsAsync();
            await SeedEntityInstancesAsync();

            // Seed sample data if enabled
            if (_settings.IncludeIntegrationSampleData)
            {
                await SeedAuditSampleDataAsync();
            }

            _logger.LogInformation("Test data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during test data seeding");
            throw;
        }
    }

    private async Task SeedIntegrationSystemsAsync()
    {
        _logger.LogDebug("Seeding integration systems");

        if (await _context.IntegrationSystems.AnyAsync())
        {
            _logger.LogDebug("Integration systems already exist, skipping");
            return;
        }

        var systems = new List<IntegrationSystem>
        {
            new IntegrationSystem
            {
                Name = "HR_System",
                DisplayName = "Human Resources System",
                Description = "Core HR system managing employee records, departments, and organizational structure",
                SystemType = "HR",
                ConnectionString = "Server=hr-db;Database=HRSystem;Integrated Security=true;",
                IsActive = true,
                LastSync = DateTime.UtcNow.AddDays(-1),
                AuthenticationType = AuthenticationType.Database,
                Configuration = """{"batchSize": 1000, "timeout": 300, "retryAttempts": 3}"""
            },
            new IntegrationSystem
            {
                Name = "EMR_System",
                DisplayName = "Electronic Medical Records",
                Description = "Electronic Medical Records system managing patient data, providers, and clinical information",
                SystemType = "EMR",
                ConnectionString = "Server=emr-db;Database=EMRSystem;Integrated Security=true;",
                IsActive = true,
                LastSync = DateTime.UtcNow.AddHours(-1),
                AuthenticationType = AuthenticationType.OAuth2,
                Configuration = """{"batchSize": 500, "timeout": 600, "retryAttempts": 5, "includePatientData": false}"""
            },
            new IntegrationSystem
            {
                Name = "Active_Directory",
                DisplayName = "Active Directory",
                Description = "Microsoft Active Directory for user authentication and group management",
                SystemType = "Identity",
                ConnectionString = "LDAP://hospital.internal",
                IsActive = true,
                LastSync = DateTime.UtcNow.AddHours(-2),
                AuthenticationType = AuthenticationType.LDAP,
                Configuration = """{"searchBase": "DC=hospital,DC=internal", "pageSize": 1000, "attributes": ["displayName", "mail", "memberOf"]}"""
            }
        };

        _context.IntegrationSystems.AddRange(systems);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} integration systems", systems.Count);
    }

    private async Task SeedEntityDefinitionsAsync()
    {
        _logger.LogDebug("Seeding entity definitions");

        if (await _context.EntityDefinitions.AnyAsync())
        {
            _logger.LogDebug("Entity definitions already exist, skipping");
            return;
        }

        var hrSystem = await _context.IntegrationSystems.FirstAsync(s => s.Name == "HR_System");
        var emrSystem = await _context.IntegrationSystems.FirstAsync(s => s.Name == "EMR_System");
        var adSystem = await _context.IntegrationSystems.FirstAsync(s => s.Name == "Active_Directory");

        var entityDefinitions = new List<EntityDefinition>
        {
            new EntityDefinition
            {
                IntegrationSystemId = hrSystem.Id,
                Name = "Employee",
                DisplayName = "Hospital Employee",
                Description = "Employee records from HR system",
                TableName = "employees",
                PrimaryKeyField = "emp_id",
                SortOrder = 1,
                Metadata = """{"syncTable": "employees", "lastModifiedField": "updated_at", "softDeleteField": "is_active"}"""
            },

            new EntityDefinition
            {
                IntegrationSystemId = hrSystem.Id,
                Name = "Department",
                DisplayName = "Hospital Department",
                Description = "Organizational departments",
                TableName = "departments",
                PrimaryKeyField = "dept_id",
                SortOrder = 2,
                Metadata = """{"syncTable": "departments", "hierarchical": true, "parentField": "parent_dept_id"}"""
            },

            new EntityDefinition
            {
                IntegrationSystemId = emrSystem.Id,
                Name = "Provider",
                DisplayName = "Healthcare Provider",
                Description = "Medical staff and providers",
                TableName = "providers",
                PrimaryKeyField = "provider_id",
                SortOrder = 3,
                Metadata = """{"syncTable": "providers", "licensingRequired": true}"""
            },

            new EntityDefinition
            {
                IntegrationSystemId = adSystem.Id,
                Name = "ADUser",
                DisplayName = "Active Directory User",
                Description = "Users from Active Directory",
                TableName = "users",
                PrimaryKeyField = "objectGUID",
                SortOrder = 4,
                Metadata = """{"ldapFilter": "(&(objectClass=user)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))"}"""
            },

            new EntityDefinition
            {
                IntegrationSystemId = adSystem.Id,
                Name = "ADGroup",
                DisplayName = "Active Directory Group",
                Description = "Security groups from Active Directory",
                TableName = "groups",
                PrimaryKeyField = "objectGUID",
                SortOrder = 5,
                Metadata = """{"ldapFilter": "(objectClass=group)", "includeMembers": true}"""
            }
        };

        _context.EntityDefinitions.AddRange(entityDefinitions);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} entity definitions", entityDefinitions.Count);
    }

    private async Task SeedPropertyDefinitionsAsync()
    {
        _logger.LogDebug("Seeding property definitions");

        if (await _context.PropertyDefinitions.AnyAsync())
        {
            _logger.LogDebug("Property definitions already exist, skipping");
            return;
        }

        var employeeDef = await _context.EntityDefinitions.FirstAsync(e => e.Name == "Employee");
        var departmentDef = await _context.EntityDefinitions.FirstAsync(e => e.Name == "Department");
        var providerDef = await _context.EntityDefinitions.FirstAsync(e => e.Name == "Provider");

        var properties = new List<PropertyDefinition>
        {
            // Employee properties
            new PropertyDefinition
            {
                EntityDefinitionId = employeeDef.Id,
                Name = "EmployeeId",
                DisplayName = "Employee ID",
                Description = "Unique employee identifier",
                DataType = DataType.String,
                SourceField = "emp_id",
                IsRequired = true,
                IsUnique = true,
                IsSearchable = true,
                SortOrder = 1
            },
            new PropertyDefinition
            {
                EntityDefinitionId = employeeDef.Id,
                Name = "FirstName",
                DisplayName = "First Name",
                Description = "Employee's first name",
                DataType = DataType.String,
                SourceField = "first_name",
                IsRequired = true,
                IsSearchable = true,
                SortOrder = 2
            },
            new PropertyDefinition
            {
                EntityDefinitionId = employeeDef.Id,
                Name = "LastName",
                DisplayName = "Last Name",
                Description = "Employee's last name",
                DataType = DataType.String,
                SourceField = "last_name",
                IsRequired = true,
                IsSearchable = true,
                SortOrder = 3
            },
            new PropertyDefinition
            {
                EntityDefinitionId = employeeDef.Id,
                Name = "Email",
                DisplayName = "Email Address",
                Description = "Primary email address",
                DataType = DataType.Email,
                SourceField = "email",
                IsRequired = true,
                IsUnique = true,
                IsSearchable = true,
                SortOrder = 4
            },

            // Department properties
            new PropertyDefinition
            {
                EntityDefinitionId = departmentDef.Id,
                Name = "DepartmentId",
                DisplayName = "Department ID",
                Description = "Unique department identifier",
                DataType = DataType.String,
                SourceField = "dept_id",
                IsRequired = true,
                IsUnique = true,
                IsSearchable = true,
                SortOrder = 1
            },
            new PropertyDefinition
            {
                EntityDefinitionId = departmentDef.Id,
                Name = "DepartmentName",
                DisplayName = "Department Name",
                Description = "Name of the department",
                DataType = DataType.String,
                SourceField = "dept_name",
                IsRequired = true,
                IsSearchable = true,
                SortOrder = 2
            },

            // Provider properties
            new PropertyDefinition
            {
                EntityDefinitionId = providerDef.Id,
                Name = "ProviderId",
                DisplayName = "Provider ID",
                Description = "Unique provider identifier",
                DataType = DataType.String,
                SourceField = "provider_id",
                IsRequired = true,
                IsUnique = true,
                IsSearchable = true,
                SortOrder = 1
            },
            new PropertyDefinition
            {
                EntityDefinitionId = providerDef.Id,
                Name = "NPINumber",
                DisplayName = "NPI Number",
                Description = "National Provider Identifier",
                DataType = DataType.String,
                SourceField = "npi_number",
                IsRequired = true,
                IsUnique = true,
                IsSearchable = true,
                SortOrder = 2
            }
        };

        _context.PropertyDefinitions.AddRange(properties);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} property definitions", properties.Count);
    }

    private async Task SeedEntityInstancesAsync()
    {
        _logger.LogDebug("Seeding entity instances");

        if (await _context.EntityInstances.AnyAsync())
        {
            _logger.LogDebug("Entity instances already exist, skipping");
            return;
        }

        var employeeDef = await _context.EntityDefinitions.FirstAsync(e => e.Name == "Employee");
        var departmentDef = await _context.EntityDefinitions.FirstAsync(e => e.Name == "Department");

        var instances = new List<EntityInstance>
        {
            // Department instances
            new EntityInstance
            {
                EntityDefinitionId = departmentDef.Id,
                ExternalId = "DEPT001",
                DisplayName = "Information Technology",
                SyncStatus = SyncStatus.Success,
                RawData = """{"dept_id": "DEPT001", "dept_name": "Information Technology", "manager_id": "EMP001", "cost_center": "IT-100"}"""
            },
            new EntityInstance
            {
                EntityDefinitionId = departmentDef.Id,
                ExternalId = "DEPT002",
                DisplayName = "Human Resources",
                SyncStatus = SyncStatus.Success,
                RawData = """{"dept_id": "DEPT002", "dept_name": "Human Resources", "manager_id": "EMP002", "cost_center": "HR-200"}"""
            },
            new EntityInstance
            {
                EntityDefinitionId = departmentDef.Id,
                ExternalId = "DEPT003",
                DisplayName = "Emergency Medicine",
                SyncStatus = SyncStatus.Success,
                RawData = """{"dept_id": "DEPT003", "dept_name": "Emergency Medicine", "manager_id": "EMP005", "cost_center": "MED-300"}"""
            },

            // Employee instances
            new EntityInstance
            {
                EntityDefinitionId = employeeDef.Id,
                ExternalId = "EMP001",
                DisplayName = "John Smith",
                SyncStatus = SyncStatus.Success,
                RawData = """{"emp_id": "EMP001", "first_name": "John", "last_name": "Smith", "email": "john.smith@hospital.com", "dept_id": "DEPT001", "job_title": "IT Manager", "status": "Active"}"""
            },
            new EntityInstance
            {
                EntityDefinitionId = employeeDef.Id,
                ExternalId = "EMP002",
                DisplayName = "Sarah Johnson",
                SyncStatus = SyncStatus.Success,
                RawData = """{"emp_id": "EMP002", "first_name": "Sarah", "last_name": "Johnson", "email": "sarah.johnson@hospital.com", "dept_id": "DEPT002", "job_title": "HR Director", "status": "Active"}"""
            },
            new EntityInstance
            {
                EntityDefinitionId = employeeDef.Id,
                ExternalId = "EMP003",
                DisplayName = "Michael Brown",
                SyncStatus = SyncStatus.Success,
                RawData = """{"emp_id": "EMP003", "first_name": "Michael", "last_name": "Brown", "email": "michael.brown@hospital.com", "dept_id": "DEPT001", "job_title": "Software Developer", "status": "Active"}"""
            }
        };

        _context.EntityInstances.AddRange(instances);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} entity instances", instances.Count);
    }

    private async Task SeedAuditSampleDataAsync()
    {
        _logger.LogDebug("Seeding audit sample data");

        if (await _context.AuditLogs.AnyAsync())
        {
            _logger.LogDebug("Audit logs already exist, skipping");
            return;
        }

        var auditLogs = new List<AuditLog>();
        var random = new Random(42);

        for (int i = 0; i < 10; i++)
        {
            var auditLog = AuditLog.Create(
                "Employee",
                $"EMP{i + 1:D3}",
                AuditAction.View,
                "system",
                $"correlation-{Guid.NewGuid():N}",
                oldValues: null,
                newValues: """{"action": "viewed_employee_record"}""",
                justification: "Regular system operation"
            );

            auditLogs.Add(auditLog);
        }

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} audit logs", auditLogs.Count);
    }
}
