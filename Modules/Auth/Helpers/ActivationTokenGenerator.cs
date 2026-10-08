using System.Security.Cryptography;

namespace SaborExpress.Modules.Auth.Helpers
{
    public static class ActivationTokenGenerator
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        // 20 caracteres alfanumericos (~119 bits): cabe en la columna code (max 20)
        // y es seguro para ir en una URL sin escapar.
        public static string Generate(int length = 20)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
            return new string(chars);
        }
    }
}