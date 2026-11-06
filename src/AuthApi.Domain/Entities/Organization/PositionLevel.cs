namespace AuthApi.Domain.Entities.Organization;

public sealed class PositionLevel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public ICollection<PositionVersion> PositionVersions { get; set; } = [];
}
