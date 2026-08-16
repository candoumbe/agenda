# ADR-002: Frontend Authentication Implementation with Keycloak

## Status

Superseded by [ADR-003](003-frontend-authentication-implementation.md)

## Date

2026-05-29

## Context

[ADR-001](001-authentication-provider.md) established Keycloak as the identity provider for Agenda. The Angular frontend now needs a concrete authentication implementation strategy that:

- Uses OpenID Connect Authorization Code Flow with PKCE.
- Integrates cleanly with Keycloak realms, clients, and standard JWT access tokens.
- Preserves an open, self-hostable architecture with no proprietary dependency.
- Supports route protection, token renewal, logout, and role/claim-based UI behavior.
- Keeps implementation and operational complexity proportional to the current product scope.

## Options Considered

### 1. angular-auth-oidc-client (recommended in [ADR-001](001-authentication-provider.md) rationale)

A mature Angular OIDC client focused on standards-based integration with providers such as Keycloak.

**Pros:**

- Native fit for Angular applications and modern Angular versions.
- First-class support for Authorization Code + PKCE and silent renew.
- Good balance between flexibility and opinionated defaults.
- Works directly with Keycloak discovery metadata and standard endpoints.
- Well documented for route guards, interceptors, and multi-environment config.

**Cons:**

- Requires learning library-specific configuration conventions.
- Some advanced Keycloak-specific behaviors still require custom glue code.
- Library upgrades can require periodic adaptation in app bootstrap/auth module wiring.

### 2. angular-oauth2-oidc

Another widely used Angular OIDC/OAuth2 library, very configurable and close to protocol details.

**Pros:**

- Proven library with long community usage.
- Very explicit OAuth2/OIDC configuration model.
- Fine control over token validation and session behavior.
- Good interoperability with Keycloak.

**Cons:**

- Lower-level API can lead to more boilerplate than option 1.
- More configuration freedom can increase inconsistency across teams.
- Requires careful hardening to avoid subtle misconfiguration.

### 3. keycloak-js with keycloak-angular wrapper

Use Keycloak's official JavaScript adapter, optionally wrapped by keycloak-angular.

**Pros:**

- Vendor-native compatibility with Keycloak features.
- Good support for Keycloak-specific capabilities and lifecycle hooks.
- Direct mapping to Keycloak session semantics.

**Cons:**

- Tighter coupling to Keycloak APIs and adapter behavior.
- Weaker portability if provider abstraction becomes necessary later.
- Historically sensitive to framework/runtime changes and adapter evolution.

### 4. Better Auth as frontend auth framework (with BFF)

Use Better Auth as an authentication layer in front of Angular, typically through a Backend-for-Frontend (BFF) Node service that federates with Keycloak.

**Pros:**

- Modern developer experience, strong extensibility, and plugin model.
- Session-first model can reduce token handling complexity in the browser.
- Useful if Agenda wants to centralize auth/session logic across multiple frontend apps.

**Cons:**

- Better Auth is not a native Angular SPA OIDC client and usually implies introducing a BFF tier.
- Adds significant architecture and operational complexity (extra service, deployment, monitoring).
- Requires additional federation design from Better Auth to Keycloak.
- Potential overlap with capabilities already provided directly by Keycloak + standard OIDC clients.
- Higher integration risk for the current scope, where only Angular + Keycloak is required.

### 5. Custom implementation on top of oidc-client-ts (or raw protocol)

Build an in-house Angular auth layer using lower-level OIDC primitives.

**Pros:**

- Maximum control over behavior and abstraction.
- Can be tailored to exact project constraints.

**Cons:**

- Highest maintenance and security risk.
- Reinvents commodity features already solved by established libraries.
- Longer delivery time and harder upgrades.

## Decision

We choose **angular-auth-oidc-client** for the Angular frontend, using Authorization Code Flow with PKCE against Keycloak.

## Rationale

1. **Alignment with [ADR-001](001-authentication-provider.md):** [ADR-001](001-authentication-provider.md) already identifies this library as a good ecosystem fit for Angular with Keycloak.
2. **Standards-first interoperability:** It supports OIDC standards expected by Keycloak without introducing provider lock-in at the frontend integration layer.
3. **Complexity control:** It avoids introducing a new BFF/auth platform unless clear future requirements justify it.
4. **Security posture:** It supports secure SPA practices (PKCE, token renewal patterns, route-guard integration) with established community usage.
5. **Delivery speed:** It offers a shorter path to production-ready authentication than custom implementations.

## Consequences

- The frontend will integrate OIDC directly with Keycloak using angular-auth-oidc-client.
- Angular route guards and HTTP interceptors will be implemented through this library's recommended patterns.
- Keycloak client configuration must include SPA settings (redirect URIs, post-logout redirect URIs, PKCE-enabled public client).
- If future requirements demand centralized session management, SSO orchestration across heterogeneous clients, or non-SPA-first auth flows, a follow-up ADR can evaluate Better Auth + BFF as a targeted evolution.

## Open questions

1. Which frontend security baseline must be mandatory at go-live (PKCE-only, strict redirect URI policy, CSP, token lifetime constraints, clock skew tolerance)?
2. What is the target session model (idle timeout, absolute timeout, silent renew policy, and behavior when renewal fails)?
3. How should Keycloak realms be structured across environments and tenants (one realm per environment, per organization, or hybrid)?
4. If Agenda evolves to multiple frontends, when does a BFF become mandatory instead of direct SPA-to-Keycloak integration?
5. How do we keep frontend auth decoupled from Keycloak-specific implementation details while Keycloak remains imposed by [ADR-001](001-authentication-provider.md)?
6. What is the canonical mapping strategy between Keycloak roles/claims and frontend authorization rules?
7. Where should tokens be stored in the browser context (memory only, session storage, local storage) given the XSS and UX trade-offs?
8. What logout semantics are required (local app logout, Keycloak SSO logout, back-channel revocation expectations)?
9. What is the minimum automated test strategy for authentication behavior (unit, integration, E2E, contract tests)?
10. Which objective criteria should trigger a future reevaluation of Better Auth in front of Keycloak?
11. What are the top implementation risks and mitigation plans (misconfiguration, token leakage, drift between realms, outage handling)?
12. What weighted decision matrix should be used to compare implementation evolutions over time and avoid ad-hoc decisions?
