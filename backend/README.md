# Bluewater

Backend for Bluewater, a replacement for the legacy rowing club web application. It will eventually cover membership, scheduling, and fleet operations for the club.

## Tech stack

- .NET 10 / ASP.NET Core Web API
- EF Core with Npgsql (PostgreSQL)
- ASP.NET Core Identity + JWT bearer auth
- OpenAPI docs served via Scalar (`/scalar`)

## Project structure

```
Bluewater.Api     ASP.NET Core host, controllers, DI composition root
Bluewater.Core    Application services, DTOs
Bluewater.Infra   EF Core DbContext, Identity, JWT token service
Bluewater.Domain  Domain/Identity entities
```

See `CLAUDE.md` for more detail on the architecture and current scaffold state.


## Planned features

- User management
- Seasons (yearly group instances)
- User group management (groups span seasons with different members; each group has a category — main groups, competition, commissions, etc. — which also drives permissions)
- Fleet management
- Fleet reservations
- Training planner
- News
- Announcements
