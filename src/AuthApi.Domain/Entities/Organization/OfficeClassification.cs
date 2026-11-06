namespace AuthApi.Domain.Entities.Organization;

public sealed class OfficeClassification
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<OfficeVersion> OfficeVersions { get; set; } = [];
    public ICollection<StructureTemplate> StructureTemplates { get; set; } = [];
}
