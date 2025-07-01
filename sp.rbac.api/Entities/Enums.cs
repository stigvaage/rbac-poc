namespace SP.RBAC.API.Entities;

public enum DataType
{
    String,
    Integer,
    Decimal,
    Boolean,
    DateTime,
    Date,
    Time,
    Email,
    Phone,
    Url,
    List,
    Json
}

public enum AssignmentType
{
    Direct,
    Inherited,
    Automatic,
    Temporary
}

public enum SyncStatus
{
    Pending,
    InProgress,
    Success,
    Failed,
    Cancelled
}

public enum AuthenticationType
{
    Database,
    LDAP,
    OAuth2,
    SAML,
    JWT,
    ApiKey
}

public enum TriggerType
{
    PropertyChange,
    NewEntity,
    EntityUpdate,
    EntityDelete,
    Schedule,
    Manual
}

public enum ActionType
{
    AssignRole,
    RemoveRole,
    UpdateProperty,
    CreateEntity,
    DeleteEntity,
    SendNotification
}
