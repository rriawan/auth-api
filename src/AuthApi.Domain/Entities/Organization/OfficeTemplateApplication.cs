namespace AuthApi.Domain.Entities.Organization;

public sealed class OfficeTemplateApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfficeId { get; set; }
    public Guid TemplateId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? AppliedByUserId { get; set; }

    public Office Office { get; set; } = null!;
    public StructureTemplate Template { get; set; } = null!;
}
