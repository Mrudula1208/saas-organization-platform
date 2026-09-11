import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(Auth);
  const router = inject(Router);

  if (!auth.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  const expectedRoles = route.data ? (route.data['roles'] as string[]) : null;
  if (expectedRoles && !auth.hasRole(expectedRoles)) {
    const user = auth.currentUser();
    if (user?.role === 'Admin') {
      router.navigate(['/admin/dashboard']);
    } else {
      router.navigate(['/tenant/dashboard']);
    }
    return false;
  }

  return true;
};
