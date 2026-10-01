// Production configuration (used automatically by `ng build`).
// Uses Netlify /api reverse proxy to eliminate browser Mixed-Content (HTTPS -> HTTP) and CORS blocks.
export const environment = {
  production: true,
  apiUrl: '/api',
  apiOrigin: '',
};
