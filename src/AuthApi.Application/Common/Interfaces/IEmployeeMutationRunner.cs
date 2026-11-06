using AuthApi.Domain.Entities.Employees;

namespace AuthApi.Application.Common.Interfaces;

/// <summary>
/// One serialized organization/employee write transaction, shared with the scoped
/// DbContext and audit service. Every module writer must use this boundary.
/// Implementations must roll back and clear tracked changes on failure.
/// </summary>
public interface IEmployeeMutationRunner
{
    bool IsActive { get; }
    void MarkEmployeeChanged(Employee employee);
    /// <summary>Forces an UPDATE on an Office, OrgUnit or Position so its rowversion rotates when only child versions changed.</summary>
    void MarkMasterChanged(object master);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}

