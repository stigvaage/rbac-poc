using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Data;

public class RbacDbContext : DbContext
{
    public RbacDbContext(DbContextOptions<RbacDbContext> options) : base(options)
    {
    }

    public DbSet<IntegrationSystem> IntegrationSystems { get; set; }
    public DbSet<EntityDefinition> EntityDefinitions { get; set; }
    public DbSet<PropertyDefinition> PropertyDefinitions { get; set; }
    public DbSet<EntityInstance> EntityInstances { get; set; }
    public DbSet<PropertyValue> PropertyValues { get; set; }
    public DbSet<AccessRule> AccessRules { get; set; }
    public DbSet<AccessAssignment> AccessAssignments { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure IntegrationSystem
        modelBuilder.Entity<IntegrationSystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SystemType).HasMaxLength(50);
            entity.Property(e => e.ConnectionString).HasMaxLength(1000);
            entity.Property(e => e.AuthenticationType).HasConversion<string>();
            entity.Property(e => e.LastSyncStatus).HasConversion<string>();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure EntityDefinition
        modelBuilder.Entity<EntityDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.PrimaryKeyField).HasMaxLength(100);
            
            entity.HasOne(e => e.IntegrationSystem)
                  .WithMany(i => i.EntityDefinitions)
                  .HasForeignKey(e => e.IntegrationSystemId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.IntegrationSystemId, e.Name }).IsUnique();
        });

        // Configure PropertyDefinition
        modelBuilder.Entity<PropertyDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DataType).HasConversion<string>();
            entity.Property(e => e.SourceField).HasMaxLength(100);
            
            entity.HasOne(e => e.EntityDefinition)
                  .WithMany(ed => ed.PropertyDefinitions)
                  .HasForeignKey(e => e.EntityDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.EntityDefinitionId, e.Name }).IsUnique();
        });

        // Configure EntityInstance
        modelBuilder.Entity<EntityInstance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SyncStatus).HasConversion<string>();
            
            entity.HasOne(e => e.EntityDefinition)
                  .WithMany(ed => ed.EntityInstances)
                  .HasForeignKey(e => e.EntityDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.EntityDefinitionId, e.ExternalId }).IsUnique();
        });

        // Configure PropertyValue
        modelBuilder.Entity<PropertyValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired();
            
            entity.HasOne(e => e.EntityInstance)
                  .WithMany(ei => ei.PropertyValues)
                  .HasForeignKey(e => e.EntityInstanceId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.PropertyDefinition)
                  .WithMany(pd => pd.PropertyValues)
                  .HasForeignKey(e => e.PropertyDefinitionId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasIndex(e => new { e.EntityInstanceId, e.PropertyDefinitionId }).IsUnique();
        });

        // Configure AccessRule
        modelBuilder.Entity<AccessRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TriggerType).HasConversion<string>();
            entity.Property(e => e.ActionType).HasConversion<string>();
            
            entity.HasOne(e => e.IntegrationSystem)
                  .WithMany(i => i.AccessRules)
                  .HasForeignKey(e => e.IntegrationSystemId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure AccessAssignment
        modelBuilder.Entity<AccessAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssignmentType).HasConversion<string>();
            
            entity.HasOne(e => e.User)
                  .WithMany(ei => ei.UserAccessAssignments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Role)
                  .WithMany(ei => ei.RoleAccessAssignments)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.TargetSystem)
                  .WithMany(i => i.AccessAssignments)
                  .HasForeignKey(e => e.TargetSystemId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // Many-to-many with AccessRule
            entity.HasMany(e => e.AccessRules)
                  .WithMany(ar => ar.AccessAssignments);
        });

        // Configure SyncLog
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasOne(e => e.IntegrationSystem)
                  .WithMany(i => i.SyncLogs)
                  .HasForeignKey(e => e.IntegrationSystemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure soft delete filter
        modelBuilder.Entity<IntegrationSystem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EntityDefinition>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PropertyDefinition>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EntityInstance>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PropertyValue>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AccessRule>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AccessAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncLog>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedBy = "system"; // TODO: Get from current user context
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = "system"; // TODO: Get from current user context
            }

            if (entity is BaseAuditableEntity auditableEntity)
            {
                auditableEntity.Version++;
                auditableEntity.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
            }
        }
    }
}
