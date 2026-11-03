using AuthApi.Domain.Entities;

namespace AuthApi.Application.Common.Interfaces;

public interface IPasswordHasherService
{
    string HashPassword(User user, string plainPassword);
    bool VerifyPassword(User user, string hashedPassword, string providedPassword, out bool needsRehash);
}

