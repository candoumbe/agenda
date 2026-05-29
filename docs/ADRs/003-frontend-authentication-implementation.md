# ADR-003: Frontend Authentication Implementation

## Status

Accepted

## Date

2026-05-29

## Supersedes

[ADR-002](002-frontend-authentication-implementation.md)

## Context

[ADR-001](001-authentication-provider.md) is accepted and mandates Keycloak as the identity provider.
The first frontend decision version ([ADR-002](002-frontend-authentication-implementation.md)) selected `angular-auth-oidc-client` but left 12 open questions.
The second version provided concrete answers.

This ADR merges both versions into a single, auditable decision.

## Decision

We choose direct Angular SPA -> Keycloak integration through `angular-auth-oidc-client`, using Authorization Code Flow + PKCE.

We do not introduce a BFF or Better Auth at this stage.
Reevaluation is allowed only when the objective criteria defined below are met.

## Questions from v1 and answers

1. **Which frontend security baseline must be mandatory at go-live (PKCE-only, strict redirect URI policy, CSP, token lifetime constraints, clock skew tolerance)?**

   Answer: For a public SPA, Authorization Code + PKCE is mandatory but not sufficient by itself. The baseline requires: PKCE-only public client configuration, strict allowlist for redirect URIs and post-logout redirect URIs, HTTPS outside local development, a baseline CSP, short access-token lifetime (5 to 10 minutes), refresh-token rotation enabled in Keycloak, and maximum clock skew set to 60 seconds.

2. **What is the target session model (idle timeout, absolute timeout, silent renew policy, and behavior when renewal fails)?**

   Answer: Target session policy is a 30-minute idle timeout, 8-hour absolute session lifetime, silent renew while the session remains valid, and mandatory redirect to login when renewal fails. A user-facing "session expired" message is displayed before redirect.

3. **How should Keycloak realms be structured across environments and tenants (one realm per environment, per organization, or hybrid)?**

   Answer: Use one realm per environment (dev, test, prod). Model organizations as groups/claims inside the same realm. Reconsider multi-realm only for tenant-specific IdP federation or strict data-isolation requirements.

4. **If Agenda evolves to multiple frontends, when does a BFF become mandatory instead of direct SPA-to-Keycloak integration?**

   Answer: Direct SPA integration remains valid for a single Angular frontend. A BFF becomes mandatory as soon as at least two independent frontends require shared session handling, centralized token exchange, or cross-channel logout orchestration.

5. **How do we keep frontend auth decoupled from Keycloak-specific implementation details while Keycloak remains imposed by ADR-001?**

   Answer: Implement a frontend auth port (interfaces/adapters) around OIDC operations, isolate Keycloak-specific claim parsing in a dedicated module, and forbid direct Keycloak endpoint usage outside that module.

6. **What is the canonical mapping strategy between Keycloak roles/claims and frontend authorization rules?**

   Answer: Define a canonical, version-controlled AuthzMap (role -> capability), centralize transformation from realm/client roles to normalized application capabilities, and make guards/components depend only on capabilities. Any change to Keycloak role modeling and AuthzMap must be reviewed in the same pull request.

7. **Where should tokens be stored in the browser context (memory only, session storage, local storage) given the XSS and UX trade-offs?**

   Answer: Store access tokens in memory only. If a browser refresh token is unavoidable, use sessionStorage only, with strict CSP and short lifetimes. localStorage is forbidden for bearer tokens. On page refresh or tab restore, if the session cannot be renewed within 5 seconds, force a clean re-authentication while preserving the original navigation target.

8. **What logout semantics are required (local app logout, Keycloak SSO logout, back-channel revocation expectations)?**

   Answer: Implement two-step logout: clear local application state first, then perform OIDC end-session redirect to Keycloak. Require post-logout redirect URI validation and explicit logout error handling with fallback to completed local logout.

9. **What is the minimum automated test strategy for authentication behavior (unit, integration, E2E, contract tests)?**

   Answer: Minimum required test suite: unit tests (auth adapter and claim mapping), integration tests (guards/interceptors), E2E tests (login, renewal failure, logout), plus contract checks for claims expected by UI authorization. These scenarios must pass in CI for every authentication-related change.

10. **Which objective criteria should trigger a future reevaluation of Better Auth in front of Keycloak?**

   Answer: Reevaluate only if at least two conditions are continuously met for one quarter (3 consecutive months), with evidence captured in architecture review notes: multi-frontend session orchestration pain across at least two distinct client applications, two or more auth incidents linked to browser token handling, a validated need for centralized provider brokering not covered by standard Keycloak capabilities, or a measured delivery slowdown of at least 20% on auth-related work versus the previous quarter.

11. **What are the top implementation risks and mitigation plans (misconfiguration, token leakage, drift between realms, outage handling)?**

   Answer: Top risks are configuration drift, token leakage, and inadequate identity-outage handling. Mitigations: startup configuration validation, realm configuration versioning, security-header enforcement on frontend hosting, an IdP incident runbook, and periodic auth threat-model review per release train.

12. **What weighted decision matrix should be used to compare implementation evolutions over time and avoid ad-hoc decisions?**

   Answer: Use a mandatory weighted matrix with these weights: Security 30%, Operational complexity 20%, Delivery speed 15%, Maintainability 15%, User experience 10%, Portability 10%. Score each criterion from 1 to 5 with explicit evidence. Final score = sum(weight x score), with acceptance thresholds of overall score >= 4.0/5 and Security >= 4/5. Run this matrix at each quarterly architecture checkpoint before any auth-direction change.

## Consequences

- Frontend implementation can proceed with an explicit security/session/testing contract.
- The architecture remains Keycloak-centered without diffuse application-level coupling.
- Definition of Done for auth-related changes includes the unit, integration, E2E, and contract checks listed above.
- Any BFF/Better Auth proposal must provide reevaluation trigger evidence and weighted matrix scoring.
