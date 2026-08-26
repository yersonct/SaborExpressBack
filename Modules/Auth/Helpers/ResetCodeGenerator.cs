using System.Security.Cryptography;

namespace SaborExpress.Modules.Auth.Helpers
{
    public static class ResetCodeGenerator
    {
        public static string GenerateSixDigitCode()
        {
            var number = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return number.ToString("D6"); // siempre 6 digitos, con ceros a la izquierda si hace falta
        }
    }
}
