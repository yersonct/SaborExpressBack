using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Modules.UserPreferences.Constants;
using SaborExpress.Modules.UserPreferences.Interfaces;
using SaborExpress.Shared.Localization;

namespace SaborExpress.Shared.Filters;

public sealed class ApiMessageLocalizationFilter : IAsyncResultFilter
{
    private readonly IApiMessageLocalizer _localizer;
    private readonly IUserPreferenceRepository _preferences;
    private readonly IMemoryCache _cache;

    public ApiMessageLocalizationFilter(
        IApiMessageLocalizer localizer,
        IUserPreferenceRepository preferences,
        IMemoryCache cache)
    {
        _localizer = localizer;
        _preferences = preferences;
        _cache = cache;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is Microsoft.AspNetCore.Mvc.ObjectResult { Value: not null } result)
        {
            var language = context.HttpContext.Request.Headers.AcceptLanguage.ToString();
            var userIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var userId))
            {
                try
                {
                    var preference = await _cache.GetOrCreateAsync(
                        UserLanguagePreferenceCache.CacheKey(userId),
                        async entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                            var saved = await _preferences.GetByUserScreenKeyAsync(
                                userId,
                                LanguageOptions.LanguageScreen,
                                LanguageOptions.LanguageKey);
                            return new UserLanguagePreferenceSnapshot(saved?.Value, saved is not null);
                        });
                    if (preference is { IsConfigured: true, Language: not null }
                        && LanguageOptions.Allowed.Contains(preference.Language, StringComparer.OrdinalIgnoreCase))
                    {
                        // The authenticated user's saved language is authoritative.
                        language = preference.Language;
                    }
                }
                catch
                {
                    // A language preference lookup must not fail an API response.
                }
            }

            var isErrorResponse = result.StatusCode >= 400;
            result.Value = _localizer.LocalizePayload(result.Value, language, isErrorResponse);
        }

        await next();
    }

}
