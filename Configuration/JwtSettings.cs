namespace SaborExpress.Configuration
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;
        public int RefreshTokenExpirationHours { get; set; } = 12; 

        public int ExpirationInMinutes { get; set; }
    }
}