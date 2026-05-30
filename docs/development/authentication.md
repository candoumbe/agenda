# Authentication — local development guide

This document explains how to run the Agenda stack locally with Keycloak, fetch tokens, and call
the API as an authenticated user or service.

See also:

- [docs/ADRs/001-authentication-provider.md](../ADRs/001-authentication-provider.md) — why Keycloak.
- [src/Agenda.AppHost/keycloak/README.md](../../src/Agenda.AppHost/keycloak/README.md) — realm export workflow.
- [docs/feature/aspire-integration.md](../feature/aspire-integration.md) — Aspire orchestration overview.

---

## 1. Stack overview

When the AppHost starts (`dotnet run --project src/Agenda.AppHost`), it provisions a Keycloak
container, imports the `agenda` realm from
[src/Agenda.AppHost/keycloak/agenda-realm.json](../../src/Agenda.AppHost/keycloak/agenda-realm.json),
and wires the API (`agenda-api` audience) to validate RS256 bearer tokens issued by that realm.

- Realm: `agenda`
- Audience: `agenda-api`
- Signing algorithm: `RS256` (validated against the realm JWKS endpoint)
- Realm roles: `agenda-user` (default), `agenda-admin`, `agenda-service-account`

## 2. Seeded dev users

Both users have the password `password`. They exist only for local development and integration
tests — never reuse those credentials anywhere else.

| Username | Password   | Realm roles                   | Purpose                                |
|----------|------------|-------------------------------|----------------------------------------|
| `alice`  | `password` | `agenda-user`                 | Standard authenticated user.           |
| `admin`  | `password` | `agenda-user`, `agenda-admin` | Admin — required for `DELETE` actions. |

## 3. Fetch a user token (Direct Access Grant)

The `agenda-frontend` client has Direct Access Grants enabled in dev to make scripted token
fetches possible. **Do not enable this flag in production.**

```bash
KEYCLOAK_URL="http://localhost:8080"   # adjust to the port Aspire exposes
TOKEN=$(curl -s -X POST \
  "${KEYCLOAK_URL}/realms/agenda/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=agenda-frontend" \
  -d "username=alice" \
  -d "password=password" \
  -d "scope=openid agenda-audience" \
  | jq -r .access_token)

echo "${TOKEN}"
```

Call the API with the bearer token:

```bash
curl -s "http://localhost:5000/appointments" \
  -H "Authorization: Bearer ${TOKEN}" | jq
```

Replace `alice` with `admin` to obtain a token carrying the `agenda-admin` role (required for
`DELETE /appointments/{id}`).

## 4. Fetch a service token (`client_credentials`)

The `agenda-service` confidential client owns the `agenda-service-account` role and is suited for
server-to-server calls (e.g. background jobs). The client secret is part of the realm export and
should be rotated outside the seed for any non-development environment.

```bash
TOKEN=$(curl -s -X POST \
  "${KEYCLOAK_URL}/realms/agenda/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=agenda-service" \
  -d "client_secret=<see realm export>" \
  -d "scope=agenda-audience" \
  | jq -r .access_token)
```

## 5. Inspect a token

Decode the JWT payload to confirm the audience, issuer, and roles before troubleshooting a 401 or
403:

```bash
echo "${TOKEN}" | cut -d. -f2 | base64 -d 2>/dev/null | jq
```

Expected highlights:

- `iss` — `${KEYCLOAK_URL}/realms/agenda`
- `aud` — must contain `agenda-api`
- `realm_access.roles` — must contain `agenda-user` (and `agenda-admin` for admin actions)
- `preferred_username` — used by `CurrentRequestMetadataInfoProvider` to expose the current user
- `sub` — Keycloak user id (parsed as a `Guid` by the API)

## 6. Troubleshooting

| Symptom                                | Likely cause                                                                 |
|----------------------------------------|------------------------------------------------------------------------------|
| `401` with `WWW-Authenticate: invalid_token` and `aud` mismatch | The token was minted for a different client; re-mint with `scope=agenda-audience`. |
| `401` with `invalid_token` and `iss` mismatch | The API and Keycloak see different base URLs; align the issuer between AppHost and `appsettings`. |
| `401` shortly after a working call     | Token expired (default lifetime 5 min); re-mint or refresh.                  |
| `401` with no descriptive header       | Clock skew between the API host and Keycloak; sync system clocks.            |
| `403` on `DELETE /appointments/{id}`   | Token lacks the `agenda-admin` realm role; mint a token for `admin`.         |

If the realm import seems out of date, follow the export/import workflow in
[src/Agenda.AppHost/keycloak/README.md](../../src/Agenda.AppHost/keycloak/README.md).
