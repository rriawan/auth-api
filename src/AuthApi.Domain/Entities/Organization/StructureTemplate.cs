using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Domain.Entities.Organization;

public sealed class StructureTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClassificationCode { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public StructureTemplateState State { get; set; } = StructureTemplateState.Draft;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public OfficeClassification Classification { get; set; } = null!;
    public ICollection<TemplateOrgUnit> OrgUnits { get; set; } = [];
    public ICollection<TemplatePosition> Positions { get; set; } = [];
    public ICollection<OfficeTemplateApplication> OfficeApplications { get; set; } = [];
}
