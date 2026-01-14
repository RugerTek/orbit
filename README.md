# OrbitOS

**AI-Native Business Operating System** - A modern, multi-tenant platform for operations management built with enterprise-grade standards.

[![CI](https://github.com/RodrigoCC90/Master-Template/actions/workflows/ci.yml/badge.svg)](https://github.com/RodrigoCC90/Master-Template/actions/workflows/ci.yml)
[![Security Scan](https://github.com/RodrigoCC90/Master-Template/actions/workflows/security.yml/badge.svg)](https://github.com/RodrigoCC90/Master-Template/actions/workflows/security.yml)

## Overview

OrbitOS is a comprehensive operations management platform featuring:

- **Multi-tenant architecture** with organization-scoped data isolation
- **Role-based access control** with granular function permissions
- **Modern authentication** supporting Azure AD, Google OAuth, and local accounts
- **AI-first development** with spec-driven architecture
- **Enterprise-grade security** following OWASP and SOC 2 guidelines

## Tech Stack

| Component | Technology | Port |
|-----------|------------|------|
| **API** | .NET 8, EF Core, PostgreSQL | 5027 |
| **Web** | Nuxt 4, Vue 3, Vuetify, TailwindCSS | 3000 |
| **Mobile** | Flutter 3.x, BLoC | N/A |

## Project Structure

```
OrbitOS-Workspace/
├── orbitos-api/          # .NET 8 Backend API
│   ├── src/
│   │   ├── OrbitOS.Api/           # Controllers, middleware
│   │   ├── OrbitOS.Application/   # Business logic, services
│   │   ├── OrbitOS.Domain/        # Entities, interfaces
│   │   ├── OrbitOS.Infrastructure/# Data access, external services
│   │   └── OrbitOS.Contracts/     # DTOs, validators
│   └── tests/                     # Unit & integration tests
├── orbitos-web/          # Nuxt 4 Frontend
│   ├── app/                       # Pages, components, composables
│   └── tests/                     # Unit & E2E tests
├── orbitos-mobile/       # Flutter Mobile App
├── contracts/            # OpenAPI specification
├── specs/                # Feature & entity specifications
├── docs/                 # Documentation
└── .github/              # CI/CD workflows
```

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/)
- [Docker](https://www.docker.com/) (optional, for containerized development)

### Environment Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/RodrigoCC90/Master-Template.git
   cd Master-Template
   ```

2. **Configure environment variables**

   Copy the example files and fill in your values:
   ```bash
   cp orbitos-api/.env.example orbitos-api/.env
   cp orbitos-web/.env.example orbitos-web/.env
   ```

3. **Start the API**
   ```bash
   cd orbitos-api
   dotnet restore
   dotnet run --project src/OrbitOS.Api
   ```

4. **Start the Web frontend**
   ```bash
   cd orbitos-web
   npm install --legacy-peer-deps
   npm run dev
   ```

### Using Docker

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down
```

## Configuration

### Required Environment Variables

#### API (`orbitos-api/.env`)

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key (min 32 characters) |
| `Google__ClientId` | Google OAuth Client ID |
| `Google__ClientSecret` | Google OAuth Client Secret |

#### Web (`orbitos-web/.env`)

| Variable | Description |
|----------|-------------|
| `NUXT_PUBLIC_API_BASE_URL` | API base URL |
| `NUXT_PUBLIC_GOOGLE_CLIENT_ID` | Google OAuth Client ID |

## Development

### Running Tests

```bash
# API tests
cd orbitos-api && dotnet test

# Web unit tests
cd orbitos-web && npm run test:unit

# Web E2E tests
cd orbitos-web && npx playwright test
```

### Code Quality

The project enforces:
- **60% minimum code coverage** (80% target)
- **TypeScript strict mode**
- **ESLint** for code style
- **Security scanning** via GitHub Actions

### Git Workflow

See [docs/GIT_WORKFLOW.md](docs/GIT_WORKFLOW.md) for branching conventions and commit message standards.

## Architecture

### Clean Architecture Layers

```
Domain (Entities, Interfaces)
    ↑
Application (Services, DTOs)
    ↑
Infrastructure (EF Core, External Services)
    ↑
API (Controllers, Middleware)
```

### Multi-Tenancy

All data is scoped by `organization_id`. Every database query must filter by organization context.

### Authentication Flow

1. User authenticates via Google OAuth or local credentials
2. API validates and issues JWT token
3. Frontend stores token and includes in API requests
4. API validates JWT and extracts user claims

## API Documentation

The API follows OpenAPI 3.1 specification. See [contracts/openapi.yaml](contracts/openapi.yaml).

When running locally, access Swagger UI at: `http://localhost:5027/swagger`

## Security

See [SECURITY.md](SECURITY.md) for security policies and vulnerability reporting.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

## License

This project is proprietary software. All rights reserved.

---

Built with enterprise-grade standards for AI-first development.
