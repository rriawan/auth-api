namespace AuthApi.Domain.Entities.Organization;

public sealed class Office
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OfficeCode { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public ICollection<OfficeVersion> Versions { get; set; } = [];
    public ICollection<OrgUnit> OrgUnits { get; set; } = [];
    public ICollection<OfficeTemplateApplication> TemplateApplications { get; set; } = [];
}
