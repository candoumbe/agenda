import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { authInterceptor, provideAuth, withAppInitializerAuthCheck } from 'angular-auth-oidc-client';

import { routes } from './app.routes';
import { createOidcConfig } from './auth/auth.config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor()])),
    provideAuth({
      config: createOidcConfig()
    }, withAppInitializerAuthCheck()),
    provideRouter(routes)
  ]
};
