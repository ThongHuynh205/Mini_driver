using System.Security.Cryptography;
using System.Text;

namespace Mini_driver.Server.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        using SHA256 sha =
            SHA256.Create();

        byte[] bytes =
            sha.ComputeHash(
                Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes);
    }
}