# SaaS Organization Platform - Web Client

The enterprise web client for the Multi-Tenant SaaS Organization Platform, built with modern standalone **Angular 19/20**, **TypeScript**, and a custom **Burgundy + Warm Beige + Charcoal** corporate design system.

---

## 🏛️ Architecture & Key Concepts

- **Standalone Components**: Eliminates legacy NgModules for faster compilation, tree-shaking, and lazy loading.
- **Reactive State Management**: Driven by Angular Signals and RxJS observables for granular change detection.
- **Role-Based Routing**: Multi-tenant route guards (`auth.guard.ts`) dynamically restrict access based on authenticated user claims (`SuperAdmin`, `TenantAdmin`, `Member`).
- **HTTP Interceptors**: Automatically injects JWT Bearer tokens and handles refresh token rotations and 401/403 responses.
- **Design System**: Fully bespoke CSS variables design system with zero external UI framework bloat (custom tables, modals, cards, badges, Kanban board).

---

## 📂 Directory Structure

```
src/
├── app/
│   ├── core/
│   │   ├── guards/          # Route authorization guards (AuthGuard)
│   │   ├── interceptors/    # JWT token injection & error interceptors
│   │   └── services/        # HTTP API services (Auth, Tenant, Project, Task, Report, Billing)
│   ├── models/              # TypeScript interfaces and DTO models
│   ├── pages/
│   │   ├── auth/            # Login, Register, Forgot Password, Reset Password
│   │   ├── landing/         # Marketing & product landing page
│   │   ├── super-admin/     # Platform Admin dashboards, tenants, revenue, settings
│   │   └── tenant/          # Workspace dashboards, projects, tasks Kanban, members, reports
│   └── shared/
│       └── components/      # Reusable UI (Navbar, Sidebar, Modal, Table, Notification bell)
├── environments/            # API endpoint configuration
└── styles.css               # Global Burgundy + Charcoal CSS design tokens
```

---

## 🛠️ Development & Build Commands

### Install Dependencies
```bash
npm install
```

### Run Local Development Server
```bash
npm start
```
Navigates to `http://localhost:4200/`. Proxies API calls to backend running on `http://localhost:5258`.

### Run Unit Tests
```bash
npm test -- --watch=false
```
Executes comprehensive Vitest test suites across guards, services, and components.

### Production Build
```bash
npm run build
```
Generates optimized, production-ready bundles in the `dist/` directory.
