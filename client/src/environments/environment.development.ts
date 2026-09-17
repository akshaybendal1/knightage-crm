export const environment = {
  // Empty so /api calls stay relative to localhost:4200 and go through
  // proxy.conf.json (see angular.json's serve.development.proxyConfig),
  // which forwards them to production server-side -- a direct absolute URL
  // here would make the browser call crm.knightagetech.com cross-origin,
  // which production's CORS policy doesn't allow from a dev origin.
  apiBaseUrl: '',
};
