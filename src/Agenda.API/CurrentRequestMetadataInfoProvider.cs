using System.Collections;
using System.Security.Claims;
using Agenda.Objects;
using Microsoft.Extensions.Primitives;
using NodaTime;

namespace Agenda.API;

/// <summary>
/// Extracts various informations from the incoming from the incoming HTTP request 
/// </summary>
/// <remarks>
/// Builds a new <see cref="CurrentRequestMetadataInfoProvider"/>
/// </remarks>
/// <param name="httpContextAccessor"></param>
/// <param name="logger"></param>
public partial class CurrentRequestMetadataInfoProvider(IHttpContextAccessor httpContextAccessor, ILogger<CurrentRequestMetadataInfoProvider> logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<CurrentRequestMetadataInfoProvider> _logger = logger;

    /// <summary>
    /// The name of the HTTP header used to specify the timezone.
    /// </summary>
    public const string TimeZoneHeaderName = "x-timezone";

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
                LogDetectedTimeZoneFromHeader(_logger, dateTimeZone.Id, TimeZoneHeaderName);
            }
            catch (Exception ex)
            {
                LogErrorWhileExtractingTimeZone(_logger, TimeZoneHeaderName, ex.Message);
                LogInfoUsingUtcTimeZone(_logger, TimeZoneHeaderName);
                _logger.LogWarning(ex, "An error occured while trying to extract {HeaderName}. The UTC timezone will be used instead", TimeZoneHeaderName);
            }
        }

        return dateTimeZone;
    }


    /// <summary>
    /// Gets the current authenticated user name from the <c>preferred_username</c> claim.
    /// </summary>
    /// <returns>The user name or an empty string when no claim is available.</returns>
    public virtual Username GetCurrentUserName()
    {
        Username userName = Username.Empty;
        ClaimsPrincipal user = _httpContextAccessor.HttpContext?.User;
        Claim usernameClaim = null;
        if (user is not null)
        {
            IReadOnlyCollection<object> claims = user.Claims.Select(claim => new { claim.Type, claim.Value }).ToList();
            LogAvailableClaims(_logger, claims);

            // Find user name
            usernameClaim = user.FindFirst(ClaimTypes.Email)
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)
                    ?? user.FindFirst("preferred-name")
                    ?? user.FindFirst(ClaimTypes.Name)
                    ?? user.FindFirst(ClaimTypes.GivenName)
                    ;
        }

        if (usernameClaim is not null)
        {
            userName = Username.FromString(usernameClaim.Value);
        }

        LogUsername(_logger, userName);

        return userName;
    }

    [LoggerMessage(LogLevel.Error, "An error occured while trying to extract timezone from {HeaderName}. The UTC timezone will be used instead.")]
    private static partial void LogErrorWhileExtractingTimeZone(ILogger logger, string headerName, string exceptionMessage);

    [LoggerMessage(LogLevel.Information, "Using UTC timezone because {HeaderName} was not found or invalid")]
    private static partial void LogInfoUsingUtcTimeZone(ILogger logger, string headerName);

    [LoggerMessage(LogLevel.Information, "Detected {TimeZoneId} from {HeaderName}")]
    private static partial void LogDetectedTimeZoneFromHeader(ILogger logger, string timeZoneId, string headerName);

    [LoggerMessage(LogLevel.Trace, "Available claims : {@claims}")]
    private static partial void LogAvailableClaims(ILogger logger, IReadOnlyCollection<object> claims);

    [LoggerMessage(LogLevel.Trace, "Username '{Username}'")]
    private static partial void LogUsername(ILogger logger, string username);
}