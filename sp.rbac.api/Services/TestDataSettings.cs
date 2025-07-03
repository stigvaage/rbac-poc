namespace SP.RBAC.API.Services;

public class TestDataSettings
{
    public bool EnableSeeding { get; set; } = false;
    public bool IncludeIntegrationSampleData { get; set; } = false;
    public int SampleUsersCount { get; set; } = 10;
    public int SampleDepartmentsCount { get; set; } = 5;
    public int SampleProvidersCount { get; set; } = 20;
}
