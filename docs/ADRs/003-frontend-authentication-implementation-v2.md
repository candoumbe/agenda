# ADR-003: Frontend Authentication Implementation V2

## Status

Proposed

## Date

2026-05-29

## Context

[ADR-001](001-authentication-provider.md) is accepted and imposes Keycloak as the identity provider. [ADR-002](002-frontend-authentication-implementation.md) selected angular-auth-oidc-client for the Angular frontend and identified unresolved questions that impact security, operability, and future evolution.

This ADR answers those questions and defines concrete implementation constraints so delivery can proceed with a testable and auditable target architecture.

## Options considered

### 1. Direct Angular SPA integration with Keycloak via angular-auth-oidc-client

- Authorization Code Flow with PKCE.
- Browser-based token handling in the SPA.
- No additional authentication middleware tier.

### 2. Keycloak with immediate BFF introduction

- Angular delegates auth/session to a dedicated backend-for-frontend.
- Browser does not directly handle access tokens.
- Extra runtime component and operational surface.

### 3. Better Auth in front of Keycloak (BFF-first)

- Better Auth handles app sessions and federates with Keycloak.
- Adds a new platform dependency and integration layer.
- Useful mainly when multi-frontend/session orchestration needs are immediate.

## Decision

Use option 1 now: direct Angular SPA integration with Keycloak through angular-auth-oidc-client, with explicit security guardrails and abstraction boundaries defined in this ADR.

Do not introduce a BFF or Better Auth in the current phase. Reevaluate only when predefined triggers are met (see response 10).

## Responses to Open questions

1. Security baseline at go-live
Answer: For a public SPA, Authorization Code + PKCE is mandatory but not sufficient; misconfigured redirects and weak browser hardening are common breach vectors.
Recommendation: Enforce PKCE-only public client, exact redirect URI allowlist, strict post-logout URI allowlist, HTTPS-only in non-local environments, CSP baseline, short access-token lifetime (5-10 min), refresh token rotation enabled in Keycloak, and maximum clock skew set to 60 seconds.

2. Session model
Answer: UX requires continuity, but security requires bounded sessions and deterministic failure behavior.
Recommendation: Idle timeout 30 minutes, absolute session lifetime 8 hours, silent renew enabled while session is valid, and hard redirect to login when renewal fails. Display a user-facing session-expired message before redirect.

3. Realms strategy
Answer: Realm-per-organization is costly and increases drift; realm-per-environment gives better control for current scope.
Recommendation: Use one realm per environment (dev, test, prod). Model organizations as groups/claims inside the realm. Revisit multi-realm only when tenant-specific IdP federation or hard data isolation is required.

4. Multi-frontend and BFF trigger
Answer: Direct SPA integration is acceptable for one Angular frontend; complexity changes when several frontends share session and policy concerns.
Recommendation: Keep SPA-direct now. Make BFF mandatory when at least two independent frontends require shared session, centralized token exchange, or cross-channel logout orchestration.

5. Decoupling from Keycloak specifics
Answer: [ADR-001](001-authentication-provider.md) fixes provider choice, but code-level coupling should still be constrained to reduce migration risk.
Recommendation: Introduce a frontend auth port (interfaces/adapters) around OIDC operations, isolate provider-specific claim parsing in one module, and forbid direct Keycloak endpoint usage outside that module.

6. Roles/claims mapping
Answer: Authorization drift occurs when UI rules depend on raw token structure spread across the codebase.
Recommendation: Define a canonical version-controlled AuthzMap document (role -> capability), map Keycloak realm/client roles to normalized app capabilities in one transformer, and make guards/components depend only on capabilities. Review AuthzMap changes in the same pull request as any Keycloak role model change.

7. Token storage in browser
Answer: localStorage materially increases token exfiltration risk under XSS; sessionStorage reduces persistence but still exposes tokens to script execution.
Recommendation: Keep access token in memory only. If refresh token in browser is unavoidable, use sessionStorage with strict CSP and short lifetimes. Do not use localStorage for bearer tokens. On page refresh or tab restore, if no valid session can be renewed within 5 seconds, force a clean re-authentication and preserve the original navigation target for post-login return.

8. Logout semantics
Answer: Users expect both app logout and SSO logout; partial logout causes security and support issues.
Recommendation: Implement two-step logout: local state clear, then OIDC end-session redirect to Keycloak. Require post-logout redirect validation and explicit handling of logout errors with fallback to local logout completion.

9. Test strategy
Answer: Auth failures are often integration defects, not unit-only defects.
Recommendation: Minimum suite: unit tests for auth adapter and claim mapping, integration tests for guard/interceptor behavior, and E2E tests for login, token renewal failure, and logout flow. Add contract checks for expected claims used by UI authorization. Definition of done requires these scenarios to pass in CI on every authentication-related change.

10. Better Auth reevaluation criteria
Answer: Re-platforming auth without objective triggers creates churn.
Recommendation: Reevaluate Better Auth only if two or more conditions hold continuously for one quarter (3 consecutive months), with evidence captured in architecture review notes: multi-frontend session orchestration pain on at least two distinct client applications, two or more auth incidents linked to browser token handling, a validated requirement for centralized provider brokering not covered by Keycloak defaults, or a measured delivery slowdown of at least 20% on auth-related features compared to the previous quarter.

11. Top risks and mitigations
Answer: Primary risks are configuration drift, token leakage, and identity outage handling gaps.
Recommendation: Add startup config validation, realm config versioning, security headers enforcement in frontend hosting, incident runbook for IdP outage, and periodic auth threat-model review per release train.

12. Weighted decision matrix
Answer: Future changes need comparable scoring, not narrative-only debate.
Recommendation: Adopt a weighted matrix with these weights: Security 30%, Operational complexity 20%, Delivery speed 15%, Maintainability 15%, User experience 10%, Portability 10%. Score each criterion from 1 to 5 with explicit evidence. Calculate weighted score as sum(weight x score). Require both: (a) at least 4.0/5 total weighted score for the selected option, and (b) Security score >= 4/5. Run this matrix at each quarterly architecture checkpoint before changing auth direction.

## Rationale

This decision keeps strict consistency with [ADR-001](001-authentication-provider.md) by retaining Keycloak as the imposed IdP while reducing ambiguity from [ADR-002](002-frontend-authentication-implementation.md) through explicit policy-level answers.

It optimizes for current product scope (single Angular frontend) and avoids premature platform expansion. It also defines objective conditions for future BFF/Better Auth adoption so evolution remains deliberate and evidence-based.

## Consequences

- Frontend implementation can proceed with a clear security/session/configuration contract.
- Architecture remains Keycloak-centric without uncontrolled provider coupling in application code.
- Testing scope becomes explicit and must be part of definition of done for authentication work.
- Any proposal to add BFF/Better Auth must include matrix scoring and trigger evidence from response 10 and 12, reviewed during a formal quarterly architecture checkpoint.
- Frontend auth implementation must include deterministic re-auth fallback behavior after page refresh when token renewal is not possible.
