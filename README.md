# 🚀 Multi-Tenant SaaS Organization Platform

A production-grade, enterprise-ready **Multi-Tenant SaaS Organization Platform** built with **.NET 8 Web API** and **Angular 19/20**, styled with a **Burgundy + Warm Beige + Charcoal** corporate design system.

---

## 🏗️ Architecture Overview

The system is designed following **Clean Architecture** principles and separation of concerns. It is structured into clearly defined layers:

```
SaaSOrganizationPlatform/
├── backend/
│   ├── SaaSPlatform.API/                   # Presentation Layer (Controllers, Middlewares, DI configuration)
│   ├── SaaSPlatform_DataAccess/            # Application Layer (Services, DTOs, Business Logic Interfaces)
│   ├── SaaSPlatform_Model/                 # Domain Layer (Database Entities, Value Objects, Domain Models)
│   ├── SaaSOrganizationPlatform.Infrastructure/ # Data Access Layer (EF Core, Repositories, Unit of Work, Migrations)
│   └── SaaSPlatform.Tests/                 # Unit & Integration Tests (xUnit, Moq)
│
├── frontend/
│   └── saas-platform-angular/              # Standalone Angular 19/20 Architecture (Signals, Services, Guards)
│
└── docker-compose.yml                      # Production Docker orchestration (SQL Server + API + Nginx Frontend)
```

---

## 💡 How Multi-Tenancy Works (Fresher-Friendly Explanation)

### 1. Data Isolation Strategy
- Every organization is assigned a unique `TenantId` (GUID).
- All tenant-owned records (`Users`, `Projects`, `Tasks`, `Payments`, etc.) have a `TenantId` foreign key.
- **EF Core Global Query Filters**: When queries execute, EF Core automatically appends `WHERE TenantId = @currentTenantId` to SQL queries behind the scenes. This guarantees that **Tenant A can never see or leak Tenant B's data**.

### 2. Tenant Context Resolution
- When a user logs in, their `TenantId` is embedded securely into their signed **JWT (JSON Web Token)**.
- Every API request validates the token and extracts the `TenantId` directly from the authenticated claims principal.
- Client requests cannot forge or change the `TenantId` in query strings or headers.

---

## ✨ Key Features & Technical Highlights

| Feature | Technologies Used | How It Works |
| :--- | :--- | :--- |
| **Authentication & RBAC** | JWT Bearer, BCrypt.Net, Refresh Tokens | Role-based authorization (`SuperAdmin`, `TenantAdmin`, `Member`) with account lockout protection. |
| **Project Management** | EF Core, Unit of Work Pattern | CRUD operations, deadline tracking, team member allocation per project. |
| **Task & Kanban Board** | Angular Drag-and-Drop, CSS Grid | Interactive 3-column Kanban board (`To Do`, `In Progress`, `Completed`) with real-time state synchronization. |
| **Analytics & Export** | **QuestPDF** & **EPPlus** | Generates PDF reports and formatted multi-sheet Excel workbooks (`.xlsx`) on demand. |
| **Audit Logs** | System Logging Repository | Tracks security events (logins, failed attempts, account lockouts, modifications) for compliance. |
| **Health Checks** | ASP.NET Core Diagnostics | `/health` endpoint for container monitoring and uptime probes. |
| **Design System** | Pure CSS Tokens | Corporate Burgundy (`#9F1239`), Charcoal (`#1F2937`), and Warm Off-White (`#FAF9F6`). |

---

## 🔑 Demo Login Accounts

All accounts are pre-seeded in the database:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@saas.com` | `admin123` | Global Platform Dashboard, Tenant Management, Subscription Plans, Platform Settings, Global Revenue, Audit Logs. |
| **Tenant Admin** | `tenant@acme.com` | `tenant123` | Workspace Dashboard, Projects, Tasks / Kanban Board, Team Members, Analytics & Export, Billing, Settings. |
| **Tenant Member** | `member@acme.com` | `member123` | Assigned Projects & Tasks, Kanban status updates, Personal Profile. |

---

## 🏃 Getting Started (Run Locally)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) & npm
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or LocalDB)

### Step 1: Start Backend API
```bash
# Navigate to the API folder
cd backend/SaaSPlatform.API

# Run the API (applies EF Core migrations and seeds database automatically)
dotnet run
```
Backend will start on: **`http://localhost:5258`** (Swagger docs available at `/swagger`).

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

- Frontend: `http://localhost:80`
- Backend API: `http://localhost:5000`
- Health check: `http://localhost:5000/health`
- Database: Port `1433`

To stop:
```bash
docker-compose down
```

---

## 🧪 Automated Testing

### Backend Unit Tests (228 Tests)
```bash
dotnet test backend/SaaSPlatform.Tests/SaaSPlatform.Tests.csproj
```

### Frontend Unit Tests (37 Tests)
```bash
cd frontend/saas-platform-angular
npm test -- --watch=false
```

---

## 🎓 Interview Talking Points (Fresher Guide)

When presenting this project to interviewers or seniors:

1. **"Why did you choose Clean Architecture?"**
   > *"Clean Architecture isolates business rules from infrastructure concerns. If we switch databases from SQL Server to PostgreSQL, or change external services, our core application entities and logic remain completely untouched."*

2. **"How did you prevent cross-tenant data leakage?"**
   > *"We combined EF Core Global Query Filters with JWT claim extraction. The `TenantId` is read exclusively from the authenticated user's JWT claims, never from client requests, ensuring data separation at both the API and database levels."*

3. **"How are file exports generated efficiently?"**
   > *"We avoided heavy headless browsers for PDFs. We use QuestPDF for vector layout generation and EPPlus for high-speed OpenXML spreadsheet creation, streaming binary content directly via HTTP response streams."*

4. **"How does the frontend maintain state without heavy state libraries?"**
   > *"We leveraged Angular Standalone Components combined with reactive RxJS services and modern Signals, keeping the bundle size small and rendering performant."*
