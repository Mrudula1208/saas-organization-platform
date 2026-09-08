// Production configuration (used automatically by `ng build`).
// Assumes the frontend and the API are served from the same origin behind a
// reverse proxy that forwards /api (and /uploads) to the backend.
// If your API is hosted on another origin, put its full URL here,
// e.g. apiUrl: 'https://api.example.com/api', apiOrigin: 'https://api.example.com'.
export const environment = {
  production: true,
  apiUrl: '/api',
  apiOrigin: '',
};
