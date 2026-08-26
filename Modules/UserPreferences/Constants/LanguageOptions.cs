// Modules/UserPreferences/Constants/LanguageOptions.cs
namespace SaborExpress.Modules.UserPreferences.Constants
{
    public static class LanguageOptions
    {
        public const string Spanish = "es";
        public const string English = "en";
        public const string French = "fr";
        public const string Portuguese = "pt";

        public static readonly string[] Allowed = { Spanish, English, French, Portuguese };

        public const string DefaultLanguage = Spanish;

        // Claves fijas para no repetir strings mágicos por todo el código
        public const string LanguageScreen = "Global";
        public const string LanguageKey = "Language";
    }
}