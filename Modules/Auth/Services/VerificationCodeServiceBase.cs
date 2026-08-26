using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Services
{
    public abstract class VerificationCodeServiceBase<TCode> where TCode : IVerificationCode
    {
        protected const int MaxCodeAttempts = 5;

        /// <summary>
        /// Valida un codigo contra el ultimo emitido para el usuario.
        /// Lanza UnauthorizedAccessException si es invalido, expirado, o se agotaron los intentos.
        /// Devuelve el codigo ya validado (marcado como IsUsed = true) para que la
        /// clase concreta haga lo que necesite despues (generar ResetToken, activar cuenta, etc.)
        /// </summary>
        protected async Task<TCode> ValidateAndConsumeCodeAsync(
            Func<Task<TCode?>> getLatestCode,
            Func<TCode, Task> updateCode,
            string inputCode)
        {
            var code = await getLatestCode();

            if (code == null || code.CodeExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Codigo invalido o expirado.");

            if (code.Attempts >= MaxCodeAttempts)
                throw new UnauthorizedAccessException("Demasiados intentos. Solicita un nuevo codigo.");

            if (code.IsUsed || code.Code != inputCode)
            {
                code.Attempts++;
                await updateCode(code);

                if (code.Attempts >= MaxCodeAttempts)
                    throw new UnauthorizedAccessException("Demasiados intentos. Solicita un nuevo codigo.");

                throw new UnauthorizedAccessException("Codigo invalido o expirado.");
            }

            code.IsUsed = true;
            await updateCode(code);

            return code;
        }
    }
}