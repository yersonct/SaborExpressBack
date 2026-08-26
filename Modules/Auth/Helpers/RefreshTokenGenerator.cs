using System.Security.Cryptography;

namespace SaborExpress.Modules.Auth.Helpers
{
    public static class RefreshTokenGenerator
    {
        public static string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }
    }
}
