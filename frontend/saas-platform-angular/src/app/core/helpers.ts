// Small shared helpers used by forms and API error handling.

export function isValidEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test((email || '').trim());
}

// Builds a user-friendly message from an HTTP/API error.
export function getErrorMessage(err: any, fallback: string): string {
  const body = err?.error;

  // API error body: { success: false, message: '...' }
  if (body && typeof body.message === 'string' && body.message) {
    return body.message;
  }

  // ASP.NET model validation body: { title: '...', errors: { Name: ['...'] } }
  if (body && body.errors && typeof body.errors === 'object') {
    const key = Object.keys(body.errors)[0];
    if (key && body.errors[key]?.length > 0) {
      return body.errors[key][0];
    }
    if (body.title) return body.title;
  }

  // Plain text error body (skip HTML error pages)
  if (typeof body === 'string' && body && !body.startsWith('<')) {
    return body;
  }

  if (err?.status === 401) return 'Your session has expired. Please log in again.';
  if (err?.status === 403) return 'You do not have permission to do this.';
  if (err?.status === 404) return 'The requested item was not found.';
  if (err?.status === 0) return 'Cannot reach the server. Please try again.';
  if (err?.status >= 500) return 'Something went wrong on the server. Please try again.';

  // Plain Error objects thrown in the app code
  if (err?.message && (err?.status === undefined || err?.status === null)) {
    return err.message;
  }

  return fallback;
}
