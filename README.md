# 🚀 Multi-Tenant SaaS Organization Platform

[![.NET 8](https://img.shields.io/badge/.NET%208-ASP.NET%20Core%20Web%20API-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular 18](https://img.shields.io/badge/Angular%2018-Standalone%20%2B%20Signals-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![SQL Server](https://img.shields.io/badge/Database-Microsoft%20SQL%20Server-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Tests Passed](https://img.shields.io/badge/xUnit%20Tests-258%20Passed%20(100%25)-brightgreen?logo=checkmarx&logoColor=white)](backend/SaaSPlatform.Tests)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture%20%2B%20RBAC-blue)](#-multi-tenancy-architecture--data-isolation)

A production-grade, enterprise-ready Multi-Tenant SaaS Organization Platform built with **.NET 8 Web API** and **Angular 18**, styled with an **Enterprise Royal Blue + Deep Slate** modern corporate design system.

---

## 🏗️ Architecture Overview

The system is designed following Clean Architecture principles and separation of concerns. It is structured into clearly defined layers:

```
SaaSOrganizationPlatform/
├── backend/
│   ├── SaaSPlatform.API/                         # Presentation Layer (Controllers, Middlewares, DI configuration)
│   ├── SaaSPlatform_DataAccess/                  # Application Layer (Services, DTOs, Business Logic Interfaces)
│   ├── SaaSPlatform_Model/                       # Domain Layer (Database Entities, Value Objects, Domain Models)
│   ├── SaaSOrganizationPlatform.Infrastructure/  # Data Access Layer (EF Core, Repositories, Unit of Work, Migrations)
│   └── SaaSPlatform.Tests/                       # Unit & Integration Tests (xUnit, Moq - 258 Tests Passed)
│
├── frontend/
│   └── saas-platform-angular/                    # Standalone Angular 18 Architecture (Signals, Services, Guards)
│
└── docker-compose.yml                            # Production Docker orchestration (SQL Server + API + Nginx Frontend)
```

---

## 💡 Multi-Tenancy Architecture & Data Isolation

### 1. Data Isolation Strategy
* **Tenant Isolation Column:** Every organization is assigned a unique `TenantId` (GUID). All tenant-owned records (`Users`, `Projects`, `Tasks`, `Payments`, `AuditLogs`, etc.) have an indexed `TenantId` foreign key.
* **EF Core Global Query Filters:** When queries execute, Entity Framework Core automatically appends `WHERE TenantId = @currentTenantId` to SQL queries behind the scenes. This guarantees that **Tenant A can never see, modify, or leak Tenant B's data**.
* **Zero-Leakage Assurance:** Application developers do not need to manually write `.Where(p => p.TenantId == tenantId)` on each repository query; the filter is baked directly into the `DbContext` model pipeline.

### 2. Tenant Context Resolution
* **Claims-Based JWT:** When a user logs in, their `TenantId` and `Role` are embedded securely into their signed JWT (JSON Web Token).
* **Automatic Pipeline Injection:** The API validates the token on every incoming request and extracts the `TenantId` directly from the authenticated claims principal via `TenantResolutionMiddleware`.
* **Tamper-Proof:** Client requests cannot forge or change the `TenantId` in query strings or request headers.

---

## ✨ Key Features & Technical Highlights

| Feature | Technologies Used | Implementation Details |
| :--- | :--- | :--- |
| **Authentication & RBAC** | JWT Bearer, BCrypt.Net, Refresh Tokens | Role-based authorization across **4 distinct roles** (`SuperAdmin`, `TenantAdmin`, `Manager`, `Member`) with account lockout protection. |
| **Project Management** | EF Core, Unit of Work Pattern | Full CRUD operations, budget tracking, milestone deadlines, and project manager allocation. |
| **Task & Sprint Kanban Board** | Angular Drag-and-Drop, CSS Grid | Interactive sprint columns (`To Do`, `In Progress`, `Done`) with priority badges and real-time state synchronization. |
| **Analytics & Export Engine** | QuestPDF & EPPlus | Generates branded executive PDF reports and formatted multi-sheet Excel workbooks (`.xlsx`) on demand. |
| **Security Audit Logs** | System Logging Repository | Tracks security events (logins, failed attempts, account lockouts, tenant modifications) for compliance. |
| **Health Checks** | ASP.NET Core Diagnostics | `/health` endpoint for container monitoring, SQL database connectivity, and uptime probes. |
| **Enterprise Design System** | Pure CSS Tokens | Modern Enterprise Royal Blue (`#2563EB`), Deep Slate (`#0F172A`), with instant Dark and Light mode switching. |

---

## 🎭 The 4 Platform Roles & Access Control

| Role | Target User | Access Scope & Responsibilities |
| :--- | :--- | :--- |
| **👑 Super Admin** | SaaS Platform Owner | Global platform dashboard, tenant directory, subscription tier pricing, system maintenance mode, global MRR revenue analytics, and audit log inspection. |
| **🏢 Tenant Admin** | Company Owner / IT Admin | Organization dashboard, employee directory & invitations, project creation, billing & plan management, security settings, and PDF/Excel report exports. |
| **📋 Project Manager** | Team Lead / Scrum Master | Project leadership, task creation & assignment, sprint Kanban tracking, priority allocation (`Urgent`, `High`, `Normal`, `Low`), and delivery velocity tracking. |
| **💻 Tenant Member** | Individual Contributor | Personal "My Tasks" board, updating task lifecycle stages (`To Do` ➔ `In Progress` ➔ `Done`), deadline alerts, and theme preferences. |

---

## 🔑 Demo Login Accounts

All accounts are pre-seeded in the database:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@saas.com` | `admin123` | Global Platform Dashboard, Tenant Management, Subscription Plans, Platform Settings, Global Revenue, Audit Logs. |
| **Tenant Admin** | `tenant@acme.com`<br>*(or `rahul.patil@technova.com`)* | `tenant123`<br>*(or `Password123!`)* | Workspace Dashboard, Projects, Tasks / Kanban Board, Team Members, Analytics & Export, Billing, Settings. |
| **Project Manager** | `manager@acme.com`<br>*(or `sneha.more@technova.com`)* | `manager123`<br>*(or `Password123!`)* | Project Leadership, Sprint Management, Task Assignment & Priorities, Delivery Velocity. |
| **Tenant Member** | `member@acme.com`<br>*(or `akash.jadhav@technova.com`)* | `member123`<br>*(or `Password123!`)* | Assigned Projects & Tasks, Kanban status updates, Personal Profile. |

---

## 🏃 Getting Started (Run Locally)

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js 20+](https://nodejs.org/) & npm
* [SQL Server](https://www.microsoft.com/sql-server) (or LocalDB / SQL Server Express)

---

### Step 1: Start Backend API
```bash
# Navigate to the API folder
cd backend/SaaSPlatform.API

# Run the API (applies EF Core migrations and seeds database automatically)
dotnet run
```
Backend will start on: **`http://localhost:5258`** (Swagger docs available at **`http://localhost:5258/swagger`**).

---

### Step 2: Start Angular Frontend
```bash
# Navigate to frontend folder
cd frontend/saas-platform-angular

# Install dependencies (first time only)
npm install

# Start the dev server
npm start
```
Frontend will be live at: **`http://localhost:4200`**

---

## 🐳 Running with Docker (Production Mode)

Spin up the entire stack (SQL Server database + .NET API + Angular on Nginx) with a single command:

```bash
docker-compose up --build -d
```

* **Frontend:** `http://localhost:80`
* **Backend API:** `http://localhost:5000`
* **Health Check:** `http://localhost:5000/health`
* **Database:** Port `1433`

To stop:
```bash
docker-compose down
```

---

## 🧪 Automated Testing

### Backend Unit Tests (258 Tests - 100% Passed)
```bash
dotnet test backend/SaaSPlatform.Tests/SaaSPlatform.Tests.csproj
```
* Multi-tenant query filter verification (ensures zero data leakage between tenants).
* 4-Role RBAC authorization attribute tests (`[Authorize(Roles = "...")]`).
* BCrypt authentication, password hashing, and account lockout threshold tests.

### Frontend Unit & Production Build
```bash
cd frontend/saas-platform-angular

# Run frontend tests
npm test -- --watch=false

# Validate production build bundle
npm run build
```

---

## 📄 License
This project is open-source and available under the [MIT License](LICENSE).
