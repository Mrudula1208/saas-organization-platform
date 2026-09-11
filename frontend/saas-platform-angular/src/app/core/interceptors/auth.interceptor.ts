import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  let token: string | null = null;
  
  if (typeof window !== 'undefined') {
    token = localStorage.getItem('saas_token');
  }

  let headers = req.headers;

  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);

    // Decode token locally to extract TenantId and append as request header
    try {
      const parts = token.split('.');
      if (parts.length === 3) {
        const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
        const tenantId = payload.tenantId || payload.TenantId || payload['TenantId'] || '';
        if (tenantId) {
          headers = headers.set('TenantId', tenantId);
        }
      }
    } catch (e) {
      console.error('Failed to parse JWT token in authInterceptor', e);
    }
  }

  const authReq = req.clone({ headers });
  return next(authReq);
};
