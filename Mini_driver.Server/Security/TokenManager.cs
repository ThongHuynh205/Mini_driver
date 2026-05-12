namespace Mini_driver.Server.Security;

public static class TokenManager
{
    public static string Generate()
    {
        return Guid.NewGuid().ToString();
    }
}