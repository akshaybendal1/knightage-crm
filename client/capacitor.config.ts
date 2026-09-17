import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.knightage.crm',
  appName: 'Knightage CRM',
  webDir: '../src/Knightage.Crm.Api/wwwroot',
  // Loads the live production site directly instead of the bundled snapshot
  // in webDir -- the app then always reflects whatever's currently deployed
  // (same FTP-deploy workflow as the web app), with no separate rebuild step
  // needed per release. webDir is kept as a fallback Capacitor still expects
  // for local tooling/asset copying, but isn't what the app actually shows.
  server: {
    url: 'https://crm.knightagetech.com',
    androidScheme: 'https',
  },
  // Native Google Sign-In only -- Google blocks its web-based Identity Services
  // widget inside embedded WebViews (the "wv" user-agent token this app's WebView
  // carries), so the login page's GSI button never renders on Android. Excluding
  // the other providers keeps their SDKs (and Facebook's AD_ID permission) out
  // of the APK entirely.
  plugins: {
    SocialLogin: {
      providers: {
        google: true,
        facebook: false,
        apple: false,
        twitter: false,
      },
    },
  },
};

export default config;
