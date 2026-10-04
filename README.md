# Cafe Management System

A comprehensive management system for coffee shop chains, handling operations, orders, and quality assurance (QA).

## Table of Contents
- [Project Overview](#project-overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Documentation](#documentation)
- [Getting Started](#getting-started)
- [Future Development](#future-development)
- [License](#license)

## Project Overview
The Cafe Management System (CMS) is designed to streamline the management of coffee shop chains by integrating:
- Shop operations (staff, inventory, shifts)
- Order processing (dine-in, takeaway, online)
- Quality assurance (service and product checks, customer feedback)
- Reporting and analytics for informed decision-making

The system aims to improve operational efficiency, maintain service quality across locations, and enhance customer satisfaction.

## Inventory Management Features
The system now includes comprehensive inventory management capabilities:
- Track inventory items (ingredients, supplies) per shop
- Set reorder levels and receive low stock alerts
- Record inventory usage (linked to orders or manual adjustment)
- Manage supplier information
- View inventory valuation and reports
- Adjust inventory quantities with reason tracking
- Set inventory quantities directly
- Deactivate/delete inventory items

## Technology Stack
- **Backend**: C# (.NET 8) with ASP.NET Core Web API (**Clean Architecture**: Domain, Application, Infrastructure, API layers)
- **Database**: MongoDB (NoSQL, document-oriented) - **Official MongoDB.Driver**
- **Frontend**: React 18 with Tailwind CSS for responsive UI - **Vite + TypeScript**
- **State Management**: **TanStack Query (React Query)** for server state + **Zustand** for client state
- **Authentication**: **ASP.NET Core Identity** with JWT tokens
- **Additional Tools**:
  - Docker / Docker Compose (for containerization and local development)
  - Swagger/OpenAPI (for API documentation)
  - xUnit (backend unit/integration testing)
  - **Testcontainers** for MongoDB integration tests
  - Jest & React Testing Library (frontend testing)
  - **Serilog + Seq** for logging and observability
  - **GitHub Actions** for CI/CD
  - Deployment: **Docker Compose on VM**

## Project Structure
```
cafe-management-system/
├── documentations/             # All project documentation
│   ├── database-design.md      # MongoDB schema design (10 collections with audit fields)
│   ├── features-and-functions.md # Detailed features and API endpoints (v1, RFC 7807, cursor pagination)
│   └── project-outline.md      # High-level project plan and scope
├── src/                        # Source code (monorepo)
│   ├── backend/                # C#/.NET Web API (Clean Architecture)
│   │   ├── src/
│   │   │   ├── CafeManagement.Domain/          # Domain layer (entities, value objects, events)
│   │   │   ├── CafeManagement.Application/     # Application layer (use cases, DTOs, interfaces)
│   │   │   ├── CafeManagement.Infrastructure/  # Infrastructure layer (MongoDB, Identity, external services)
│   │   │   └── CafeManagement.Api/             # API layer (controllers, middleware, Swagger)
│   │   ├── tests/
│   │   │   ├── CafeManagement.UnitTests/
│   │   │   ├── CafeManagement.IntegrationTests/
│   │   │   └── CafeManagement.ArchitectureTests/
│   │   ├── Dockerfile
│   │   └── docker-compose.yml
│   └── frontend/               # React/Vite/TypeScript application
│       ├── src/
│       │   ├── features/         # Feature-based modules (auth, shops, staff, menu, orders, qa, reports)
│       │   ├── shared/           # Shared components, hooks, utilities
│       │   ├── components/       # Reusable UI components
│       │   ├── lib/              # API client, query client, store
│       │   └── styles/           # Tailwind CSS, global styles
│       ├── tests/                # Jest + React Testing Library tests
│       ├── Dockerfile
│       └── docker-compose.yml
├── docker-compose.yml            # Root docker-compose for full stack
├── .github/workflows/            # GitHub Actions CI/CD
├── README.md                     # This file
└── ...                           # Configuration files
```

## Documentation
Detailed documentation is available in the `documentations/` directory:
1. [Project Outline](documentations/project-outline.md) - Objectives, scope, phases
2. [Database Design](documentations/database-design.md) - MongoDB collections and structure
3. [Features and Functions](documentations/features-and-functions.md) - Feature list and corresponding API endpoints

## Getting Started
*Note: This project is currently in the design phase. Implementation steps will follow.*

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js >= 18](https://nodejs.org/) and npm
- [MongoDB](https://www.mongodb.com/try/download/community) (or use MongoDB Atlas)
- [Docker](https://www.docker.com/get-started) and Docker Compose
- [Seq](https://datalust.co/seq) for log aggregation (optional, can use Docker)

### Installation Steps (Future)
1. Clone the repository
2. Set up MongoDB instance and update connection string
3. Backend:
   - Navigate to `src/backend`
   - Run `dotnet restore`
   - Update `appsettings.json` with MongoDB connection, JWT settings, Seq URL
   - Run `dotnet run` to start the API
4. Frontend:
   - Navigate to `src/frontend`
   - Run `npm install`
   - Create `.env` file with API URL (`VITE_API_URL=http://localhost:5000/api/v1`)
   - Run `npm run dev` to launch the Vite dev server
5. Or use Docker Compose for full stack:
   - Run `docker-compose up -d` from root directory

### API Documentation
Once the backend is running, API documentation will be available via Swagger UI at `/swagger` (e.g., `http://localhost:5000/swagger`).

## Future Development
- Phase 1: Core operations (shops, staff, menu, basic orders)
- Phase 2: Inventory management, advanced orders, payment integration
- Phase 3: QA module, customer feedback, reporting
- Phase 4: Analytics dashboard, mobile responsiveness, third-party integrations

## Contributing
Please read the contributing guidelines (to be added) before submitting pull requests.

## License
This project is licensed under the MIT License - see the LICENSE file (to be created) for details.

## Acknowledgments
- Inspired by the need for efficient management solutions in the F&B industry.
- Built with ❤️ using modern full-stack technologies.

---

