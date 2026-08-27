namespace Server.Core.Interfaces;

public interface IPasswordHasher
{
    bool VerifyPassword(string password, string passwordHash);
}
