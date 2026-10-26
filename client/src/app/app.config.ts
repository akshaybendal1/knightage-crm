import { ApplicationConfig, inject, LOCALE_ID, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { PreloadAllModules, provideRouter, withPreloading, TitleStrategy } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeIn from '@angular/common/locales/en-IN';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth-interceptor';
import { apiBaseUrlInterceptor } from './core/api-base-url-interceptor';
import { AppConfig } from './core/app-config';
import { DefaultTitleStrategy } from './core/default-title-strategy';

// Without this, CurrencyPipe falls back to Angular's default en-US locale --
// the ₹ symbol itself would still render (that comes from the ISO code), but
// digit grouping would be wrong (₹1,234,567 instead of the Indian ₹12,34,567
// style). Registered once here since Lead/Product pricing is the first money
// value this app has ever shown.
registerLocaleData(localeIn);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: LOCALE_ID, useValue: 'en-IN' },
    // Preloads every lazy route's JS chunk in the background once the app is
    // idle, instead of only fetching a chunk on first navigation to it --
    // without this, the router-outlet keeps showing the previous route
    // (e.g. the login form) for as long as the target chunk takes to fetch
    // and parse, producing a visible flash right after login.
    provideRouter(routes, withPreloading(PreloadAllModules)),
    { provide: TitleStrategy, useClass: DefaultTitleStrategy },
    provideHttpClient(withInterceptors([apiBaseUrlInterceptor, authInterceptor])),
    provideAppInitializer(() => inject(AppConfig).load()),
  ],
};
