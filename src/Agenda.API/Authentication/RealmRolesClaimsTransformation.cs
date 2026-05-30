using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Agenda.API.Authentication;

/// <summary>
/// Flattens the Keycloak <c>realm_access.roles</c> JSON claim into individual
/// <see cref="ClaimTypes.Role"/> claims on the principal's primary identity.
/// </summary>
internal sealed class RealmRolesClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ClaimsIdentity identity = principal.Identity as ClaimsIdentity;
        bool canTransform = identity is { IsAuthenticated: true };

        if (canTransform)
        {
            Claim realmAccessClaim = principal.FindFirst(RealmAccessClaimType);

            if (realmAccessClaim is not null)
            {
                AddRoleClaims(identity, realmAccessClaim.Value);
            }
        }

        return Task.FromResult(principal);
    }

    private static void AddRoleClaims(ClaimsIdentity identity, string realmAccessJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(realmAccessJson);

            bool hasRoles = document.RootElement.ValueKind == JsonValueKind.Object
                            && document.RootElement.TryGetProperty("roles", out JsonElement rolesElement)
                            && rolesElement.ValueKind == JsonValueKind.Array;

            if (hasRoles)
            {
                foreach (JsonElement roleElement in document.RootElement.GetProperty("roles").EnumerateArray())
                {
                    string role = roleElement.GetString();
                    bool roleIsValid = !string.IsNullOrWhiteSpace(role)
                                       && !identity.HasClaim(ClaimTypes.Role, role);

                    if (roleIsValid)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed realm_access claim — skip role flattening.
        }
    }
}
