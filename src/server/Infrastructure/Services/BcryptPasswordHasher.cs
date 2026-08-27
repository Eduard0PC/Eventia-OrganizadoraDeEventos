using Server.Core.Interfaces;

namespace Server.Infrastructure.Services;

public class BcryptPasswordHasher : IPasswordHasher
{
    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
