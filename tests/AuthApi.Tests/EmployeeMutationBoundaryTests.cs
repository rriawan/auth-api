using AuthApi.Domain.Entities.Employees;
using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

public sealed class EmployeeMutationBoundaryTests
{
    [Fact]
    public async Task ProductionRunner_RejectsNonSqlServerProviderBeforeInvokingCallback()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("runner-provider-test-" + Guid.NewGuid().ToString("N")).Options);
        var runner = new SqlServerEmployeeMutationRunner(db);
        var called = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync(_ =>
        {
            called = true;
            return Task.FromResult(0);
        }));
        Assert.False(called);
        Assert.False(runner.IsActive);
    }

    [Fact]
    public void RootCannotBeMarkedOutsideTransactionAndSqlConnectionRemainsClosed()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=RunnerMetadataOnly;Integrated Security=True").Options);
        var runner = new SqlServerEmployeeMutationRunner(db);
        Assert.Throws<InvalidOperationException>(() => runner.MarkEmployeeChanged(new Employee()));
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }
}
