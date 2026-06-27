import { LogLevel, OpenIdConfiguration } from 'angular-auth-oidc-client';

type RuntimeAuthConfig = {
  authority?: string;
  clientId?: string;
  scope?: string;
};

declare global {
  interface Window {
    __agendaAuth?: RuntimeAuthConfig;
  }
}

type RuntimeWindow = Window & {
  __agendaAuth?: RuntimeAuthConfig;
};

function readRuntimeConfig(): RuntimeAuthConfig {
  const runtimeWindow = window as RuntimeWindow;
  return runtimeWindow.__agendaAuth ?? {};
}

export function createOidcConfig(): OpenIdConfiguration {
  const runtimeConfig = readRuntimeConfig();
  const authority = runtimeConfig.authority ?? 'http://localhost:8080/realms/agenda';
  const clientId = runtimeConfig.clientId ?? 'agenda-frontend';
  const scope = runtimeConfig.scope ?? 'openid profile email agenda-audience';

  return {
    authority,
    clientId,
    scope,
    responseType: 'code',
    checkRedirectUrlWhenCheckingIfIsCallback: true,
    redirectUrl: `${window.location.origin}/auth/callback`,
    postLogoutRedirectUri: `${window.location.origin}/login`,
    silentRenew: true,
    useRefreshToken: true,
    secureRoutes: ['/api'],
    logLevel: LogLevel.Warn
  };
}
