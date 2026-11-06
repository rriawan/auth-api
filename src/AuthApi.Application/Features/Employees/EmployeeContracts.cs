using System.Text.Json.Serialization;
using AuthApi.Domain.Enums.Employees;

namespace AuthApi.Application.Features.Employees;

public sealed record EmployeeActor(Guid UserId, string? IpAddress = null, string? UserAgent = null);
/// <summary>
/// Proof that <see cref="EmployeeMutationSupport.AuthorizeAsync"/> succeeded for this mutation.
/// The constructor is internal so code outside this assembly cannot forge an authorization.
/// </summary>
public sealed record AuthorizedEmployeeMutation
{
    internal AuthorizedEmployeeMutation(Guid actorUserId, Guid adminClientAppId, DateOnly businessDate,
        DateTime nowUtc, string? ipAddress, string? userAgent)
    {
        ActorUserId = actorUserId; AdminClientAppId = adminClientAppId; BusinessDate = businessDate;
        NowUtc = nowUtc; IpAddress = ipAddress; UserAgent = userAgent;
    }

    public Guid ActorUserId { get; }
    public Guid AdminClientAppId { get; }
    public DateOnly BusinessDate { get; }
    public DateTime NowUtc { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
}
public sealed record EmployeeSummary(Guid Id, string EmployeeNumber, string EmployeeFullName,
    string Version, [property: JsonConverter(typeof(JsonStringEnumConverter))] EmployeeStatusCode? CurrentStatus, Guid? LinkedUserId);
public sealed record EmployeeMutationResult(EmployeeSummary Employee, Guid? RecordId = null, Guid? SecondaryRecordId = null);

public sealed record CreateEmployeeCommand(string EmployeeNumber, string EmployeeFullName, DateOnly EffectiveFrom, string Reason);
public sealed record UpdateEmployeeIdentityCommand(string EmployeeNumber, string EmployeeFullName, string Reason);
public sealed record ChangeEmployeeStatusCommand(EmployeeStatusCode StatusCode, DateOnly EffectiveFrom, string Reason, string? DecisionReference = null);
public sealed record CreateDefinitiveAssignmentCommand(Guid PositionId, bool IsPrimary, DateOnly EffectiveFrom,
    DateOnly? EffectiveToExclusive, string Reason, string? DecisionReference = null);
public sealed record TransferEmployeeCommand(Guid SourceRecordId, Guid TargetPositionId, DateOnly EffectiveFrom,
    string Reason, string? DecisionReference = null);
public sealed record EndDefinitiveAssignmentCommand(Guid RecordId, DateOnly EffectiveToExclusive, string Reason);
public sealed record CreateActingAssignmentCommand(Guid SourceRecordId, Guid TargetPositionId, ActingAssignmentType AssignmentType,
    DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive, string DecisionReference, string Reason);
public sealed record EndActingAssignmentCommand(Guid RecordId, DateOnly EffectiveToExclusive, string Reason);

public interface IEmployeeProfileService
{
    Task<EmployeeMutationResult> CreateAsync(CreateEmployeeCommand command, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> UpdateIdentityAsync(Guid employeeId, UpdateEmployeeIdentityCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> ChangeStatusAsync(Guid employeeId, ChangeEmployeeStatusCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> LinkUserAsync(Guid employeeId, Guid userId, string reason, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> UnlinkUserAsync(Guid employeeId, Guid userId, string reason, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
}

public interface IEmployeePlacementService
{
    Task<EmployeeMutationResult> AssignDefinitiveAsync(Guid employeeId, CreateDefinitiveAssignmentCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> TransferAsync(Guid employeeId, TransferEmployeeCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> EndDefinitiveAsync(Guid employeeId, EndDefinitiveAssignmentCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> AssignActingAsync(Guid employeeId, CreateActingAssignmentCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<EmployeeMutationResult> EndActingAsync(Guid employeeId, EndActingAssignmentCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
}

/// <summary>Shared schedule operations; only callable inside the mutation runner.</summary>
public interface IEmployeeScheduleWriter
{
    Task ValidateAsync(Guid employeeId, AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken);
    Task CloseForResignationAsync(Guid employeeId, DateOnly date, string reason,
        AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken);
}

public sealed class EmployeePreconditionException(bool isMissing)
    : Exception(isMissing ? "An exact employee version is required." : "The employee version is stale.")
{
    public bool IsMissing { get; } = isMissing;
}
