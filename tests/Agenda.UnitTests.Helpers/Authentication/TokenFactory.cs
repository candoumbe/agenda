using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Agenda.UnitTests.Helpers.Authentication;

/// <summary>
/// Mints RS256-signed JWTs that mimic the shape of Keycloak-issued tokens for tests.
/// </summary>
public sealed class TokenFactory
{
    private static readonly JwtSecurityTokenHandler s_handler = new() { MapInboundClaims = false };

    private readonly RSA _rsa;
    private readonly RsaSecurityKey _signingKey;
    private readonly string _keyId;

    /// <summary>
    /// Default audience used by <see cref="ValidFor"/> when none is provided.
    /// </summary>
    public const string DefaultAudience = "agenda-api";

    /// <summary>
    /// Default issuer used when no explicit issuer is supplied.
    /// </summary>
    public string Issuer { get; }

    /// <summary>
    /// Builds a new <see cref="TokenFactory"/> with a freshly generated RSA key pair.
    /// </summary>
    public TokenFactory(string issuer = "https://test-issuer.local/realms/agenda")
    {
        Issuer = issuer;
        _rsa = RSA.Create(2048);
        _keyId = Guid.NewGuid().ToString("N");
        _signingKey = new RsaSecurityKey(_rsa) { KeyId = _keyId };
    }

    /// <summary>
    /// Mints a token expected to be accepted by an API trusting <see cref="GetJwks"/> / <see cref="Issuer"/>.
    /// </summary>
    public string ValidFor(string sub, IEnumerable<string> roles, string audience = DefaultAudience, string issuer = null)
    {
        DateTime now = DateTime.UtcNow;
        return BuildToken(sub, roles, audience, issuer ?? Issuer, now.AddMinutes(-1), now.AddMinutes(5), tamperSignature: false);
    }

    /// <summary>
    /// Mints a token whose <c>exp</c> is already in the past.
    /// </summary>
    public string Expired(string sub = "alice", IEnumerable<string> roles = null)
    {
        DateTime now = DateTime.UtcNow;
        return BuildToken(sub, roles, DefaultAudience, Issuer, now.AddHours(-2), now.AddHours(-1), tamperSignature: false);
    }

    /// <summary>
    /// Mints a token with an audience the API does not accept.
    /// </summary>
    public string WrongAudience(string sub = "alice")
    {
        DateTime now = DateTime.UtcNow;
        return BuildToken(sub, null, "not-the-agenda-api", Issuer, now.AddMinutes(-1), now.AddMinutes(5), tamperSignature: false);
    }

    /// <summary>
    /// Mints a token issued by an authority the API does not trust.
    /// </summary>
    public string WrongIssuer(string sub = "alice")
    {
        DateTime now = DateTime.UtcNow;
        return BuildToken(sub, null, DefaultAudience, "https://attacker.example/realms/agenda", now.AddMinutes(-1), now.AddMinutes(5), tamperSignature: false);
    }

    /// <summary>
    /// Mints a token whose signature has been mangled after signing.
    /// </summary>
    public string UnsignedOrTampered(string sub = "alice")
    {
        DateTime now = DateTime.UtcNow;
        return BuildToken(sub, null, DefaultAudience, Issuer, now.AddMinutes(-1), now.AddMinutes(5), tamperSignature: true);
    }

    /// <summary>
    /// Returns the public JWKS associated with the factory's signing key.
    /// </summary>
    public JsonWebKeySet GetJwks()
    {
        RSAParameters parameters = _rsa.ExportParameters(includePrivateParameters: false);
        JsonWebKey jwk = new()
        {
            Kty = "RSA",
            Use = "sig",
            Alg = SecurityAlgorithms.RsaSha256,
            Kid = _keyId,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent),
        };

        JsonWebKeySet jwks = new();
        jwks.Keys.Add(jwk);
        return jwks;
    }

    private string BuildToken(string sub, IEnumerable<string> roles, string audience, string issuer, DateTime notBefore, DateTime expires, bool tamperSignature)
    {
        List<Claim> claims =
        [
            new Claim("sub", sub),
            new Claim("preferred_username", sub),
            new Claim("typ", "Bearer")
        ];

        IReadOnlyList<string> realmRoles = roles is null ? [] : [.. roles];
        if (realmRoles.Count > 0)
        {
            string realmAccess = JsonSerializer.Serialize(new { roles = realmRoles });
            claims.Add(new Claim("realm_access", realmAccess, JsonClaimValueTypes.Json));
        }

        SigningCredentials credentials = new(_signingKey, SecurityAlgorithms.RsaSha256);

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        string encoded = s_handler.WriteToken(token);

        if (tamperSignature)
        {
            string[] parts = encoded.Split('.');
            char[] sig = parts[2].ToCharArray();
            sig[0] = sig[0] == 'A' ? 'B' : 'A';
            parts[2] = new string(sig);
            encoded = string.Join('.', parts);
        }

        return encoded;
    }
}
