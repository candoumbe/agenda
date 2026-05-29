# Keycloak realm — `agenda`

This folder contains the Keycloak configuration imported by the Aspire AppHost when it
provisions the `keycloak` resource.

## Files

- [agenda-realm.json](agenda-realm.json) — full realm export consumed by `WithRealmImport(...)`.

## Realm summary

- Realm: `agenda`
- Realm roles: `agenda-user` (default), `agenda-admin`, `agenda-service-account`
- Clients:
  - `agenda-frontend` — public, Authorization Code + PKCE (S256), Direct Access Grants enabled (dev only).
  - `agenda-mobile` — public, Authorization Code + PKCE (S256), custom-scheme redirect URI.
  - `agenda-api` — bearer-only, audience target.
  - `agenda-service` — confidential, `client_credentials` (service account) with `agenda-service-account` role.
- Audience and realm-roles flatten mappers are exposed through the `agenda-audience` client scope and
  attached to `agenda-frontend`, `agenda-mobile`, and `agenda-service`.
- Token lifetimes: access token `300s` (5 min), SSO idle `1800s` (30 min), SSO max `28800s` (8 h).

## Dev users (DEV ONLY — passwords are seeded for local development)

| Username | Password   | Realm roles                  |
|----------|------------|------------------------------|
| `alice`  | `password` | `agenda-user`                |
| `admin`  | `password` | `agenda-user`, `agenda-admin`|

> ⚠️ The seeded passwords above must never be reused outside local dev / integration tests.

## Re-export workflow

When the realm is updated through the Keycloak admin console, re-export it to keep this file
as the single source of truth.

1. Open a shell into the running `keycloak` container started by Aspire:

   ```bash
   docker exec -it <keycloak-container> /opt/keycloak/bin/kc.sh export \
     --realm agenda \
     --file /tmp/agenda-realm.json \
     --users realm_file
   ```

2. Copy the export back to this folder:

   ```bash
   docker cp <keycloak-container>:/tmp/agenda-realm.json src/Agenda.AppHost/keycloak/agenda-realm.json
   ```

3. Review the diff carefully — Keycloak adds environment-specific `id` fields and timestamps that
   should be normalized before committing.

## Re-import workflow

The realm is automatically re-imported by Aspire on every AppHost startup through
`WithRealmImport("./keycloak/agenda-realm.json")`. To force a clean re-import locally, delete the
`keycloak-data` volume:

```bash
docker volume rm keycloak-data
```

Then restart the AppHost.
