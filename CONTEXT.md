# Booking Platform

A booking platform for reserving services or resources, built with ASP.NET Core, an Aspire-orchestrated backend, and a Vite frontend.

## Language

**Booking Database**:
The PostgreSQL database used for operational persistence and intended as the backing store for Marten (document database / event store).
_Avoid_: App database, Postgres instance

**Cache**:
The Redis instance used for output caching and transient data.
_Avoid_: Redis cache

**Server**:
The ASP.NET Core API that serves the backend.
_Avoid_: API, backend service

**Web Frontend**:
The Vite-based SPA served by the AppHost.
_Avoid_: Frontend app, client
