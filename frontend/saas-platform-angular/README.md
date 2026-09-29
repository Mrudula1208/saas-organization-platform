# SaaS Organization Platform - Frontend Web Client

The enterprise web client for the Multi-Tenant SaaS Organization Platform, built with modern standalone **Angular 18**, **TypeScript**, and a custom **Royal Blue + Slate** design system.

---

## 🏛️ Architecture & Key Concepts

- **Standalone Components**: Eliminates legacy NgModules for faster compilation, tree-shaking, and lazy-loaded routes.
- **Reactive State Management**: Powered by Angular Signals and RxJS observables for granular, high-performance UI updates.
- **4-Role Dynamic Routing**: Route guards (`authGuard`, `roleGuard`) dynamically enforce access across all **4 distinct platform roles**:
  1. **👑 SuperAdmin**: Global platform oversight, tenant directory, subscription plans, system revenue, and security audit logs.
  2. **🏢 TenantAdmin**: Organization management, user directory & invitations, project CRUD, billing, and QuestPDF/EPPlus compliance reports.
  3. **📋 Manager**: Project leadership, task creation & assignment, sprint Kanban tracking, priority allocation, and delivery velocity.
  4. **💻 Member**: Individual contributor workflow, personal task board, status lifecycle transitions (`To Do` ➔ `In Progress` ➔ `Done`), and notifications.
- **HTTP Interceptors**: Automatically injects JWT Bearer tokens, manages rolling refresh token rotations, and gracefully handles 401/403 responses.
- **Enterprise Design System**: Bespoke CSS variables design system with zero external UI framework bloat (custom tables, modals, cards, badges, Kanban boards, and instant Dark/Light theme switching).

---

## 🎭 4-Role Frontend Experience

| Role | Access Scope | Primary Routes & Views |
| :--- | :--- | :--- |
| **SuperAdmin** | Global Platform Host | `/super-admin/dashboard`, `/super-admin/tenants`, `/super-admin/subscription-plans`, `/super-admin/revenue`, `/super-admin/system-logs`, `/super-admin/users`, `/super-admin/reports` |
| **TenantAdmin** | Organization Administrator | `/tenant/dashboard`, `/tenant/users`, `/tenant/projects`, `/tenant/tasks`, `/tenant/billing`, `/tenant/reports`, `/tenant/settings` |
| **Manager** | Project & Team Lead | `/tenant/dashboard`, `/tenant/projects`, `/tenant/tasks` (Full Sprint & Task CRUD), `/tenant/reports`, `/tenant/notifications` |
| **Member** | Individual Contributor | `/tenant/dashboard` (My Tasks View), `/tenant/tasks` (Status Progress), `/tenant/notifications`, `/tenant/settings` (Profile & Theme) |

---

## 📂 Directory Structure

```
src/
├── app/
│   ├── core/
│   │   ├── guards/          # Route authorization guards (authGuard, roleGuard)
│   │   ├── interceptors/    # JWT token injection & error handling
│   │   └── services/        # HTTP API services (Auth, Tenant, Project, Task, Report, Billing, Theme)
│   ├── models/              # TypeScript interfaces and DTO models
│   ├── pages/
│   │   ├── auth/            # Login, Register, Forgot Password, Reset Password
│   │   ├── landing/         # Marketing & product landing page
│   │   ├── logout/          # Clean, centered confirmation modal
│   │   ├── super-admin/     # Platform Admin dashboards, tenants, revenue, settings, system logs
│   │   └── tenant/          # Tenant dashboards, projects, tasks Kanban, members, reports, billing
│   └── shared/
│       └── components/      # Reusable UI (Navbar, Sidebar, Modal, Table, Notification bell)
├── environments/            # API endpoint configuration
└── styles.css               # Global Royal Blue + Slate CSS design tokens & theme variables
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
Navigates to `http://localhost:4200/`. Proxies API calls to the ASP.NET Core backend running on `http://localhost:5258`.

### Production Build
```bash
npm run build
```
Generates optimized, production-ready bundles in the `dist/saas-platform-angular/` directory.

---

## 🔑 Pre-Seeded Test Accounts

| Role | Email | Password |
| :--- | :--- | :--- |
| **👑 SuperAdmin** | `admin@saas.com` | `admin123` |
| **🏢 TenantAdmin** | `rahul.patil@technova.com` | `Password123!` |
| **📋 Manager** | `sneha.more@technova.com` | `Password123!` |
| **💻 Member** | `akash.jadhav@technova.com` | `Password123!` |
