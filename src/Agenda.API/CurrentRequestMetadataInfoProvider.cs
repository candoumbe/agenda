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
}