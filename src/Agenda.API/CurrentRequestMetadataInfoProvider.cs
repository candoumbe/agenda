using System;
using System.Security.Claims;
using Agenda.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NodaTime;

namespace Agenda.API;

/// <summary>
/// Extracts various informations from the incoming from the incoming HTTP request 
/// </summary>
public class CurrentRequestMetadataInfoProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentRequestMetadataInfoProvider> _logger;

    /// <summary>
    /// /
    /// </summary>
    public const string TimeZoneHeaderName = "x-timezone";

    /// <summary>
    /// Builds a new <see cref="CurrentRequestMetadataInfoProvider"/>
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    public CurrentRequestMetadataInfoProvider(IHttpContextAccessor httpContextAccessor, ILogger<CurrentRequestMetadataInfoProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Gets the <see cref="DateTimeZone"/> for the current request by reading the HTTP header named <see cref="TimeZoneHeaderName"/>
    /// </summary>
    /// <returns>The current <see cref="DateTimeZone"/> or <see cref="DateTimeZone.Utc"/></returns>
    public DateTimeZone GetCurrentDateTimeZone()
    {
        DateTimeZone dateTimeZone = DateTimeZone.Utc;

        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(TimeZoneHeaderName, out StringValues headers) is true && headers.Count > 0)
        {
            try
            {
                string timeZoneId = headers[0] ?? string.Empty;
                dateTimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) ?? DateTimeZone.Utc;
                _logger.LogTrace("Detected {TimeZoneId} from {HeaderName}", dateTimeZone.Id, TimeZoneHeaderName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occured while trying to extract {HeaderName}. The UTC timezone will be used instead", TimeZoneHeaderName);
            }
        }

        return dateTimeZone;
    }

    /// <summary>
    /// Gets the current authenticated user identifier by parsing the <c>sub</c> claim as a <see cref="Guid"/>.
    /// </summary>
    /// <returns>The user identifier or <see langword="null"/> when the claim is missing or malformed.</returns>
    public Guid? GetCurrentUserId()
    {
        Guid? userId = null;
        ClaimsPrincipal user = _httpContextAccessor.HttpContext?.User;

        if (user is not null)
        {
            Claim subClaim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);

            if (subClaim is not null && Guid.TryParse(subClaim.Value, out Guid parsed))
            {
                userId = parsed;
            }
        }

        return userId;
    }

    /// <summary>
    /// Gets the current authenticated user name from the <c>preferred_username</c> claim.
    /// </summary>
    /// <returns>The user name or an empty string when no claim is available.</returns>
    public virtual Username GetCurrentUserName()
    {
        Username userName = Username.Empty;
        ClaimsPrincipal user = _httpContextAccessor.HttpContext?.User;

        if (user is not null)
        {
            Claim nameClaim = user.FindFirst("preferred_username");

            if (nameClaim is not null && !string.IsNullOrWhiteSpace(nameClaim.Value))
            {
                userName = Username.FromString(nameClaim.Value);
            }
        }

        return userName;
    }
}