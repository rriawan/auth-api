using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Security;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string plainPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);
        return _passwordHasher.HashPassword(user, plainPassword);
    }

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword, out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            needsRehash = true;
            return true;
        }

        return result == PasswordVerificationResult.Success;
    }
}

