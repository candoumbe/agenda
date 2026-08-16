# ADR-001: Authentication Provider Selection

## Status

Accepted

## Date

2026-05-29

## Context

The Agenda API currently allows anonymous access to all endpoints (see issue [#323](https://github.com/candoumbe/agenda/issues/323)). We need to add authentication with the following constraints:

- The API must support JWT authentication.
- The authentication mechanism must be open and federated.
- User data must not be captured by a third-party proprietary service.
- The identity provider may come from an external authority.
- The solution must be self-hostable and open source.
- No dependency on GAFAM (Google, Apple, Facebook, Amazon, Microsoft) services.

## Options Considered

### 1. Keycloak

An open-source Identity and Access Management solution by Red Hat (CNCF project), supporting OpenID Connect, OAuth 2.0, SAML 2.0, and LDAP federation.

**Pros:**

- Most mature and battle-tested open-source IAM solution (10+ years).
- Full OpenID Connect and OAuth 2.0 certification.
- Built-in admin console and account management UI.
- Native support for identity federation (external OIDC providers, SAML IdPs, LDAP/AD).
- Multi-tenancy via realms — suitable for multi-organization scenarios.
- Extensive theme and extension system.
- Large community, active development, well-documented.
- Container-ready (official Docker image), integrates easily with .NET Aspire.
- Fine-grained authorization services (RBAC, ABAC, UMA).

**Cons:**

- Heavyweight — requires a database (PostgreSQL recommended) and significant memory (~512 MB minimum).
- Admin UI can be complex for simple use cases.
- Upgrade path between major versions can require migration effort.
- Java-based — debugging or extending requires JVM expertise.
- Initial configuration is verbose for a simple JWT-only setup.

### 2. Zitadel

A cloud-native identity management platform written in Go, offering OIDC and SAML support with a modern API-first design.

**Pros:**

- Modern, API-first architecture — everything configurable via gRPC/REST.
- Built-in multi-tenancy without realm complexity.
- Lightweight compared to Keycloak (~200 MB memory).
- Written in Go — single binary deployment, fast startup.
- Supports OIDC, OAuth 2.0, SAML, and passkeys/WebAuthn natively.
- Actions system (JavaScript hooks) for custom logic.
- Self-hosted or managed cloud option.

**Cons:**

- Younger project (2019) — smaller community and fewer integrations.
- Less ecosystem support (fewer adapters, plugins, and third-party guides).
- CockroachDB or PostgreSQL required as datastore.
- Limited LDAP federation compared to Keycloak.
- Documentation less comprehensive for advanced scenarios.
- Fewer enterprise deployments — less battle-tested at scale.

### 3. Ory Hydra

A headless OAuth 2.0 and OpenID Connect provider, designed as a minimal, composable building block.

**Pros:**

- Extremely lightweight and focused — does one thing (OAuth/OIDC) well.
- No UI bundled — full control over login/consent UX.
- Written in Go — fast, low resource usage.
- Composable with other Ory components (Kratos for identity, Oathkeeper for proxy).
- Cloud Native Computing Foundation (CNCF) sandbox project.
- Strong security posture — minimal attack surface.

**Cons:**

- No built-in admin UI — requires building or integrating one.
- No built-in user management — must pair with Ory Kratos or custom identity store.
- Higher integration effort — multiple components to deploy and configure.
- Identity federation requires additional setup (not turnkey).
- Smaller community than Keycloak.
- More suited for teams with strong infrastructure expertise.

### 4. Authentik

A modern open-source identity provider with a polished UI, supporting OIDC, SAML, LDAP, and SCIM.

**Pros:**

- Clean, modern admin interface — lower learning curve than Keycloak.
- Supports OIDC, OAuth 2.0, SAML, LDAP outpost, and SCIM provisioning.
- Flow-based authentication — visual editor for login/registration flows.
- Proxy authentication mode — can protect legacy apps without code changes.
- Active development and growing community.
- Python/Go-based — easier to extend for Python-familiar teams.

**Cons:**

- Younger than Keycloak (2020) — less proven at enterprise scale.
- Not OpenID certified (as of 2026).
- Requires PostgreSQL and Redis as dependencies.
- Limited multi-tenancy support (tenants are a newer feature).
- Fewer enterprise case studies and third-party integrations.
- Documentation gaps for complex federation scenarios.

### 5. Authelia

A lightweight Single Sign-On and two-factor authentication portal, designed as a companion to reverse proxies.

**Pros:**

- Very lightweight — minimal resource requirements.
- Excellent as an authentication gateway for reverse proxy setups (Traefik, NGINX).
- Built-in 2FA (TOTP, WebAuthn, Duo).
- Simple YAML-based configuration.
- Written in Go — single binary.
- OIDC provider capabilities added in recent versions.

**Cons:**

- Primarily designed for reverse-proxy authentication — not a full-featured IdP.
- OIDC support is newer and less mature than dedicated solutions.
- No SAML support.
- Limited identity federation — no external IdP brokering.
- No built-in user provisioning or SCIM.
- Not suitable as a standalone identity authority for API-first architectures.
- Small team — slower feature development.

## Decision

We choose **Keycloak** as the identity provider for the Agenda application.

## Rationale

1. **Maturity and reliability:** Keycloak is the most proven solution with the largest deployment base among open-source IAM tools.
2. **Federation support:** Native support for external identity providers (OIDC, SAML, LDAP) aligns with the requirement that identity may come from another system.
3. **OpenID Connect certification:** Guarantees standard-compliant JWT tokens that .NET can validate out of the box.
4. **Aspire integration:** The official Keycloak Docker image integrates naturally with the existing .NET Aspire setup (`Agenda.AppHost`).
5. **No vendor lock-in:** Self-hosted, Apache 2.0 licensed, no proprietary dependencies.
6. **Ecosystem:** The Angular frontend can use certified OIDC client libraries (`angular-auth-oidc-client`) against Keycloak without custom adapters.

The resource overhead is acceptable given that Keycloak will run as a container alongside the existing infrastructure managed by Aspire.

## Consequences

- The `Agenda.AppHost` project will provision a Keycloak container.
- The API will validate JWT Bearer tokens issued by Keycloak using standard .NET authentication middleware.
- The Angular frontend will implement the Authorization Code flow with PKCE.
- A Keycloak realm configuration (realm export JSON) will be version-controlled for reproducible environments.
- Integration tests will need a Keycloak instance (or mock token issuer) in their test infrastructure.
