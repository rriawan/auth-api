namespace AuthApi.Domain.Entities.Organization;

public sealed class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrgUnitId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public OrgUnit OrgUnit { get; set; } = null!;
    public ICollection<PositionVersion> Versions { get; set; } = [];
}
