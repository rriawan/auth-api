namespace AuthApi.Domain.Entities.Organization;

public sealed class OrgUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfficeId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public Office Office { get; set; } = null!;
    public ICollection<OrgUnitVersion> Versions { get; set; } = [];
    public ICollection<Position> Positions { get; set; } = [];
}
