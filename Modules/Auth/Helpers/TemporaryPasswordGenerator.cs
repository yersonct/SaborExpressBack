using System.Security.Cryptography;

namespace SaborExpress.Modules.Auth.Helpers
{
    public static class TemporaryPasswordGenerator
    {
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";

        public static string Generate(int length = 12)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = Chars[bytes[i] % Chars.Length];

            return new string(chars);
        }
    }
}
