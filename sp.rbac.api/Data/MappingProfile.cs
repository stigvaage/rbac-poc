using AutoMapper;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Data;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // IntegrationSystem mappings
        CreateMap<IntegrationSystem, IntegrationSystemDto>();
        CreateMap<CreateIntegrationSystemDto, IntegrationSystem>();
        CreateMap<UpdateIntegrationSystemDto, IntegrationSystem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // EntityDefinition mappings
        CreateMap<EntityDefinition, EntityDefinitionDto>()
            .ForMember(dest => dest.IntegrationSystemName, opt => opt.MapFrom(src => src.IntegrationSystem.Name))
            .ForMember(dest => dest.PropertyDefinitionsCount, opt => opt.MapFrom(src => src.PropertyDefinitions.Count))
            .ForMember(dest => dest.EntityInstancesCount, opt => opt.MapFrom(src => src.EntityInstances.Count));
        CreateMap<CreateEntityDefinitionDto, EntityDefinition>();
        CreateMap<UpdateEntityDefinitionDto, EntityDefinition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // PropertyDefinition mappings
        CreateMap<PropertyDefinition, PropertyDefinitionDto>()
            .ForMember(dest => dest.EntityDefinitionName, opt => opt.MapFrom(src => src.EntityDefinition.Name));
        CreateMap<CreatePropertyDefinitionDto, PropertyDefinition>();
        CreateMap<UpdatePropertyDefinitionDto, PropertyDefinition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // EntityInstance mappings
        CreateMap<EntityInstance, EntityInstanceDto>()
            .ForMember(dest => dest.EntityDefinitionName, opt => opt.MapFrom(src => src.EntityDefinition.Name));
        CreateMap<CreateEntityInstanceDto, EntityInstance>();
        CreateMap<UpdateEntityInstanceDto, EntityInstance>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyValues, opt => opt.Ignore()); // Handle separately

        // PropertyValue mappings
        CreateMap<PropertyValue, PropertyValueDto>()
            .ForMember(dest => dest.PropertyDefinitionName, opt => opt.MapFrom(src => src.PropertyDefinition.Name))
            .ForMember(dest => dest.PropertyDataType, opt => opt.MapFrom(src => src.PropertyDefinition.DataType));
        CreateMap<CreatePropertyValueDto, PropertyValue>();
        CreateMap<UpdatePropertyValueDto, PropertyValue>()
            .ForMember(dest => dest.EntityInstanceId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // AccessRule mappings
        CreateMap<AccessRule, AccessRuleDto>()
            .ForMember(dest => dest.IntegrationSystemName, opt => opt.MapFrom(src => src.IntegrationSystem != null ? src.IntegrationSystem.Name : null));
        CreateMap<CreateAccessRuleDto, AccessRule>();
        CreateMap<UpdateAccessRuleDto, AccessRule>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.LastExecuted, opt => opt.Ignore())
            .ForMember(dest => dest.LastExecutionResult, opt => opt.Ignore());

        // AccessAssignment mappings
        CreateMap<AccessAssignment, AccessAssignmentDto>()
            .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
            .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.Role.DisplayName))
            .ForMember(dest => dest.TargetSystemName, opt => opt.MapFrom(src => src.TargetSystem.Name));
        CreateMap<CreateAccessAssignmentDto, AccessAssignment>();
        CreateMap<UpdateAccessAssignmentDto, AccessAssignment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // SyncLog mappings
        CreateMap<SyncLog, SyncLogDto>()
            .ForMember(dest => dest.IntegrationSystemName, opt => opt.MapFrom(src => src.IntegrationSystem.Name));
        CreateMap<CreateSyncLogDto, SyncLog>();
        CreateMap<UpdateSyncLogDto, SyncLog>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IntegrationSystemId, opt => opt.Ignore())
            .ForMember(dest => dest.Operation, opt => opt.Ignore())
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());

        // Enhanced PropertyValue mappings for standalone controller
        CreateMap<PropertyValue, PropertyValueDto>()
            .ForMember(dest => dest.EntityInstanceDisplayName, opt => opt.MapFrom(src => src.EntityInstance.DisplayName))
            .ForMember(dest => dest.PropertyDefinitionName, opt => opt.MapFrom(src => src.PropertyDefinition.Name))
            .ForMember(dest => dest.PropertyDataType, opt => opt.MapFrom(src => src.PropertyDefinition.DataType))
            .ForMember(dest => dest.EntityDefinitionName, opt => opt.MapFrom(src => src.EntityInstance.EntityDefinition.Name));
    }
}
