import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';

// Public Pages
import { LandingPage } from './pages/landing/landing';
import { Login } from './pages/auth/login/login';
import { Register } from './pages/auth/register/register';
import { ForgotPassword } from './pages/auth/forgot-password/forgot-password';
import { ResetPassword } from './pages/auth/reset-password/reset-password';
import { Logout } from './pages/logout/logout';

// Dashboard Layout Wrapper
import { DashboardLayout } from './shared/components/layouts/dashboard-layout';

// Super Admin Pages
import { Dashboard as AdminDashboard } from './pages/super-admin/dashboard/dashboard';
import { Tenants } from './pages/super-admin/tenants/tenants';
import { Users as AdminUsers } from './pages/super-admin/users/users';
import { SubscriptionPlans } from './pages/super-admin/subscription-plans/subscription-plans';
import { Revenue } from './pages/super-admin/revenue/revenue';
import { SystemLogs } from './pages/super-admin/system-logs/system-logs';
import { Reports as AdminReports } from './pages/super-admin/reports/reports';
import { Settings as AdminSettings } from './pages/super-admin/settings/settings';

// Tenant Pages
import { Dashboard as TenantDashboard } from './pages/tenant/dashboard/dashboard';
import { Users as TenantUsers } from './pages/tenant/users/users';
import { Projects } from './pages/tenant/projects/projects';
import { Tasks } from './pages/tenant/tasks/tasks';
import { Reports as TenantReports } from './pages/tenant/reports/reports';
import { Billing } from './pages/tenant/billing/billing';
import { Notifications } from './pages/tenant/notifications/notifications';
import { Settings as TenantSettings } from './pages/tenant/settings/settings';

export const routes: Routes = [
  // Public Routes
  { path: '', component: LandingPage },
  { path: 'login', component: Login },
  { path: 'register-tenant', component: Register },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'reset-password', component: ResetPassword },
  { path: 'logout', component: Logout },

  // Super Admin Routes (Protected)
  {
    path: 'admin',
    component: DashboardLayout,
    canActivate: [authGuard],
    data: { roles: ['Admin'] },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: AdminDashboard },
      { path: 'tenants', component: Tenants },
      { path: 'users', component: AdminUsers },
      { path: 'subscription-plans', component: SubscriptionPlans },
      { path: 'revenue', component: Revenue },
      { path: 'system-logs', component: SystemLogs },
      { path: 'reports', component: AdminReports },
      { path: 'settings', component: AdminSettings }
    ]
  },

  // Tenant Routes (Protected)
  {
    path: 'tenant',
    component: DashboardLayout,
    canActivate: [authGuard],
    data: { roles: ['TenantAdmin', 'Member'] },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: TenantDashboard },
      { path: 'users', component: TenantUsers },
      { path: 'projects', component: Projects },
      { path: 'tasks', component: Tasks },
      { path: 'reports', component: TenantReports },
      { path: 'billing', component: Billing },
      { path: 'notifications', component: Notifications },
      { path: 'settings', component: TenantSettings }
    ]
  },

  // Fallback Redirect
  { path: '**', redirectTo: '' }
];
