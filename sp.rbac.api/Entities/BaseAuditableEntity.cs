namespace SP.RBAC.API.Entities;

public abstract class BaseAuditableEntity : BaseEntity
{
    public int Version { get; set; } = 1;
    public string? LastModifiedReason { get; set; }
    public byte[] RowVersion { get; set; } = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
}
