# Parcel Tracking API

![CI](https://github.com/ElmarKessler/parcel-tracking/actions/workflows/ci.yml/badge.svg)

Small ASP.NET Core backend that tracks parcels from warehouse pick to delivery and plans pick routes through a warehouse grid.

Stack: .NET 10, ASP.NET Core Minimal API, EF Core with SQLite, xUnit, GitHub Actions.

## Run

```
dotnet run --project src/ParcelTracking.Api
dotnet test
```

Example requests: `requests.http` (VS Code extension "REST Client").

## Endpoints

| Method | Path | Notes |
|---|---|---|
| POST | /orders | Create an order |
| GET | /orders/{id} | Order with parcels |
| POST | /orders/{id}/parcels | Add a parcel (warehouse + bin) |
| GET | /parcels/{id} | Current state |
| GET | /parcels/{id}/history | Status events, oldest first |
| POST | /parcels/{id}/events | Status change, idempotent per `eventId` |
| GET | /warehouses/{id}/pick-route?parcelIds=1,2 | Pick order and steps |

## Design decisions

Full design: [docs/DESIGN.md](docs/DESIGN.md).

- **Status as a state machine.** Created, Picked, Packed, Shipped, Delivered. Only one step forward is allowed; anything else returns 409.
- **Idempotent events.** The sender provides an `eventId`. A retried event returns 200 and changes nothing. The same `eventId` with a different payload returns 409. The event row and the status change are saved in one transaction; a unique index on `eventId` is the safety net.
- **Pick route.** BFS gives the shortest path between cells. Stops are visited nearest-first (greedy). This is simple but not guaranteed optimal: the optimal order is a travelling-salesman problem.
- **Thin endpoints.** Business logic sits in `ParcelEventService`, `ParcelStateMachine` and `PickRoutePlanner`, which are tested without HTTP. Idempotency is tested against real SQLite in memory, because EF Core's InMemory provider does not enforce unique indexes.

## Next steps

Authentication, EF Core migrations instead of `EnsureCreated`, Azure App Service deployment, event ingestion through a queue, handling out-of-order events, better route ordering (2-opt).

## About this project

Built to learn C#/.NET after working in Java and C++. I used an AI assistant as a tutor and for drafts; I reviewed and can explain every line.
