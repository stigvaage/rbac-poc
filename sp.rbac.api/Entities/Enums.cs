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

public enum AuditAction
{
    Insert,
    Update,
    Delete,
    View,
    Export,
    Login,
    Logout,
    AccessGranted,
    AccessDenied
}

/// <summary>
/// Types of relationships between integration systems
/// </summary>
public enum RelationshipType
{
    DataSource,        // Source system provides data to target
    DataTarget,        // Target system receives data from source
    AuthenticationProvider, // Source provides authentication services
    AuthenticationConsumer, // Target consumes authentication services
    ApiProvider,       // Source provides API services
    ApiConsumer,       // Target consumes API services
    MasterData,        // Source is master data provider
    ReferenceData,     // Lookup/reference data relationship
    Backup,            // Backup relationship
    Mirror,            // Mirror/replication relationship
    Federation,        // Federated identity relationship
    Dependency         // General dependency relationship
}

/// <summary>
/// Types of integration documentation
/// </summary>
public enum DocumentType
{
    TechnicalSpecification,    // Technical integration specs
    UserManual,               // End-user documentation
    AdminGuide,               // Administrator guide
    ApiDocumentation,         // API documentation
    DataDictionary,           // Data mapping and field definitions
    ProcessFlow,              // Business process documentation
    SystemDiagram,            // Architecture and system diagrams
    SecuritySpecification,    // Security requirements and procedures
    TestPlan,                // Testing procedures and plans
    TroubleshootingGuide,     // Issue resolution documentation
    ChangeLog,               // Change history and release notes
    ComplianceDocument,       // Regulatory and compliance documentation
    BusinessRequirement,      // Business requirement documentation
    SLA,                     // Service level agreements
    RunBook                  // Operational procedures
}
