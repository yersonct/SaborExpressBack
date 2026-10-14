using Microsoft.Extensions.Caching.Memory;

namespace SaborExpress.Shared.Localization;

public sealed record UserLanguagePreferenceSnapshot(string? Language, bool IsConfigured);

public static class UserLanguagePreferenceCache
{
    public static string CacheKey(int userId) => $"user-language-preference:{userId}";

    public static void Store(IMemoryCache cache, int userId, string language)
    {
        cache.Set(
            CacheKey(userId),
            new UserLanguagePreferenceSnapshot(language, true),
            TimeSpan.FromMinutes(5));
    }

    public static void Invalidate(IMemoryCache cache, int userId) =>
        cache.Remove(CacheKey(userId));
}
