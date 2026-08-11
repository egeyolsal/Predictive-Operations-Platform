# Predictive Operations Platform

A full-stack operations platform for workforce (task) management and inventory tracking, built on **.NET 8** and **Angular 21**. The platform's core differentiator is a set of **statistical analytics features built entirely from first principles** — no external machine learning libraries — layered on top of a standard, production-style CRUD application.

> **A note on naming:** this project deliberately avoids the label "AI" for its core analytics. The anomaly detection and stock forecasting features are grounded in classic statistics (z-score, moving averages), not trained models. The only genuinely AI-powered component is an optional, clearly scoped chat assistant (Google Gemini), which is rate-limited and kept separate from the statistical core. See [Why Statistics, Not ML](#why-statistics-not-ml) below.

---

## Table of Contents

- [Core Features](#core-features)
- [Tech Stack](#tech-stack)
- [Architecture Highlights](#architecture-highlights)
- [Why Statistics, Not ML](#why-statistics-not-ml)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Default Roles & First Admin Setup](#default-roles--first-admin-setup)
- [API Overview](#api-overview)

---

## Core Features

### Task & Workforce Management
- Full CRUD for tasks, with category and assignee (dropdown-driven) selection.
- Status workflow (`ToDo` → `InProgress` → `Done`), with workers able to self-manage the status of tasks assigned to them, and admins retaining full control.
- Barcode-based material consumption: marking a task as using inventory (via barcode lookup) automatically deducts stock and records a transaction.

### Statistical Anomaly Detection (Z-Score)
- When a task is completed, its duration (`CompletedAt - CreatedAt`) is compared against the mean and standard deviation of other completed tasks in the same category.
- Uses a **leave-one-out** approach: the task being evaluated is excluded from its own baseline calculation, preventing the "masking effect" where an outlier skews the very statistics used to detect it.
- Guards against division-by-zero and low-sample-size edge cases (minimum sample size, minimum standard deviation thresholds).
- Flagged tasks (`IsAnomalous = true`) are surfaced in the UI, visible only to `Admin` and `Analyst` roles.

### Predictive Inventory Forecasting (Moving Average)
- Calculates each inventory item's daily consumption velocity from historical transaction data.
- Projects days remaining until stock reaches zero, and — more actionably — days remaining until it crosses the critical threshold.
- When an item's projected depletion is imminent, the system **automatically opens a system-generated task** (`IsSystemGenerated = true`) as an alert. This flag is intentionally kept separate from `IsAnomalous`, which is reserved exclusively for the duration-based statistical result — the two concepts are not conflated.
- Alert generation is idempotent (no duplicate alerts for the same item while one is already open) and automatically resolves when new stock is received.

### Inventory & Procurement
- Inventory CRUD with category dropdown, unique constraints on item name and barcode (enforced at both the application and database level).
- Supplier and customer management.
- Invoice generation with line items, and **PDF export** via QuestPDF — invoices are rendered server-side and streamed to the client, never assembled client-side.

### Dashboard & Reporting
- Real-time metrics: task status trends, staff performance, top consumed inventory items, 7-day task activity.
- Chart visualizations (Chart.js / PrimeNG Charts).

### Optional AI Assistant
- A chat widget backed by Google Gemini, scoped to answer questions about how the platform's features work.
- Rate-limited per user (fixed window) to prevent abuse and control API cost.
- System prompt explicitly instructs the model to describe the statistical features accurately (as statistics, not AI) — keeping the assistant's own explanations consistent with the platform's actual architecture.

### Authentication & Authorization
- JWT-based authentication with BCrypt password hashing.
- Role-based access control (`Admin`, `Analyst`, `Worker`), enforced at the API layer via `[Authorize(Roles = "...")]` — not just hidden in the UI.
- User identity for self-service endpoints (profile, password change) is always resolved from the JWT claim, never from a route parameter, to prevent insecure direct object reference (IDOR) vulnerabilities.
- Secrets (JWT signing key, Gemini API key) are kept out of source control via .NET User Secrets.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 Web API, Entity Framework Core 8, SQLite |
| Frontend | Angular 21 (standalone components, zoneless change detection, signals) |
| UI | PrimeNG 21, PrimeFlex |
| Auth | JWT Bearer, BCrypt.Net |
| PDF Generation | QuestPDF |
| AI (optional) | Google Gemini API |
| Charts | Chart.js |

---

## Architecture Highlights

- **Repository + Unit of Work pattern** over EF Core, keeping data access consistent and testable.
- **DTOs are strictly separated from entities** — Create/Update/Response DTOs never expose internal or server-controlled fields (timestamps, computed flags) to the client, and API responses never leak EF Core navigation properties.
- **Controllers stay thin**; statistical and business logic lives in dedicated services (`TaskAnomalyService`, `StockPredictionService`), each behind an interface for testability.
- **Global exception handling middleware** ensures unhandled errors return a clean, generic response to the client while full details are logged server-side.
- **CORS, rate limiting, and role-based authorization** are configured explicitly rather than left to defaults.

---

## Why Statistics, Not ML

Early in this project's design, off-the-shelf ML tooling (e.g. ML.NET) was considered for both anomaly detection and stock forecasting — and deliberately rejected. The reasoning:

1. **Data volume.** The dataset (a handful of inventory items, tens of historical transactions) is far too small for a trained model to produce a meaningfully better result than direct calculation.
2. **Explainability.** A z-score or a moving average can be verified by hand, line by line. A trained model's output cannot — and for an operations tool where a human ultimately acts on the result, that verifiability matters.
3. **Proportionality.** Adding a machine learning dependency (or a separate microservice) for a problem that a few dozen lines of arithmetic solve correctly would be disproportionate complexity for the problem at hand.

This is a considered trade-off, not a limitation the project is unaware of — the README, UI copy, and even the AI assistant's own system prompt are kept consistent with this framing throughout the codebase.

---

## Project Structure

```
Backend/
├── Controllers/       # Thin HTTP endpoints
├── Services/           # Business logic (anomaly detection, stock prediction, PDF, AI, tokens)
├── Models/              # EF Core entities
├── Dtos/                 # Request/response contracts
├── Repositories/     # Generic repository + Unit of Work
├── Data/                  # DbContext, DataSeeder
└── Migrations/       # EF Core migrations

Frontend/
└── src/app/
    ├── core/              # Auth, guards, interceptors, layout (navbar/sidebar)
    └── features/     # One folder per feature area (tasks, inventory, dashboard,
                            #   invoices, customers, suppliers, profile, users, auth)
```

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+ and npm
- Angular CLI (`npm install -g @angular/cli`)

### Backend Setup

```bash
cd Backend

# Restore dependencies
dotnet restore

# Configure required secrets (never committed to source control)
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-secret-key-at-least-32-characters-long"
dotnet user-secrets set "GeminiApiKey" "your-google-gemini-api-key"   # optional, only needed for the AI assistant

# Apply migrations
dotnet ef database update

# Seed realistic sample data (categories, users, inventory, historical tasks, transactions)
dotnet run -- seed

# Run the API
dotnet run --launch-profile https
```

The API will be available at `https://localhost:7249` (Swagger UI at `/swagger`).

### Frontend Setup

```bash
cd Frontend
npm install
ng serve
```

The app will be available at `http://localhost:4200`.

---

## Default Roles & First Admin Setup

New registrations are always assigned the `Worker` role by default (self-registration cannot grant elevated privileges). To create the first `Admin` account:

1. Register a user normally via `POST /api/auth/register` (or the login page's registration flow, if enabled).
2. Open the SQLite database file and manually change that user's `Role` column to `0` (Admin).
3. Log in again to receive a token reflecting the updated role.

Seeded worker accounts (`worker1`–`worker5`) use the password `Worker123!` for local testing.

---

## API Overview

| Area | Base Route | Notes |
|---|---|---|
| Auth | `/api/auth` | Register, login |
| Tasks | `/api/task` | CRUD, status updates, material consumption |
| Inventory | `/api/inventory` | CRUD, includes computed critical-threshold flag |
| Categories | `/api/category` | CRUD |
| Analytics | `/api/analytics` | Stock depletion predictions |
| Dashboard | `/api/dashboard` | Aggregated metrics for the dashboard UI |
| Invoices | `/api/invoice` | CRUD, PDF export |
| Customers / Suppliers | `/api/customer`, `/api/supplier` | CRUD |
| Profile | `/api/profile` | Self-service profile and password management (identity resolved from JWT, not route params) |
| Users | `/api/user` | Admin-only role management |
| AI Assistant | `/api/aiassistant` | Rate-limited chat endpoint (optional feature) |

All endpoints except `/api/auth/*` require a valid JWT bearer token; role-restricted endpoints additionally enforce `[Authorize(Roles = "...")]` server-side.
