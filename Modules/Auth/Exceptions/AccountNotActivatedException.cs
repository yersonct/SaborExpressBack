namespace SaborExpress.Modules.Auth.Exceptions
{
    public class AccountNotActivatedException : Exception
    {
        public AccountNotActivatedException()
            : base("Tu cuenta aun no ha sido activada. Ingresa el codigo que recibiste por correo.")
        {
        }
    }
}