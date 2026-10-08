namespace SaborExpress.Modules.Auth.Exceptions
{
    public class EmailNotConfirmedException : Exception
    {
        public EmailNotConfirmedException()
            : base("Debes confirmar tu correo antes de iniciar sesion.")
        {
        }
    }
}