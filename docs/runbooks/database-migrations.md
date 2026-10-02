# Runbook — Database migrations

Applies to FND-02 onward. Background: [ADR 0003](../decisions/0003-postgresql-migrations-and-database-layout.md),
[plan.md §10](../plan.md).

## Rules

- Migrations run **once per deployment, from a controlled step** (the Migrator). The API never migrates at startup.
- For now a **single database login** serves the Migrator and, later, the API. Separate migration/runtime roles
  are introduced with the hosted database (DEP-01).
- Never commit connection strings or passwords. The input is the `ConnectionStrings__appdb` environment variable.

## Local (Aspire)

`dotnet run --project src/aspire/Saas.Subscription.Sample.AppHost --launch-profile https` starts PostgreSQL 17
with the persistent volume `saas-sample-pgdata`, runs the Migrator to completion, then starts the API.

- The `postgres` password is generated once and stored in the AppHost's user secrets
  (`Parameters:postgres-password`).
- **Reset the database:** stop the AppHost, `docker volume rm saas-sample-pgdata`, start again.
- **Never reset only the secrets:** the password is baked into the existing volume. If the secrets are lost,
  remove the volume too.
- Inspect: `docker exec -e PGPASSWORD=<password> <postgres container> psql -h localhost -U postgres -d saas_sample`
  (the password is in the container's `POSTGRES_PASSWORD` variable).

## Run the Migrator by hand

```bash
export ConnectionStrings__appdb="Host=...;Port=...;Database=saas_sample;Username=<login>;Password=<password>"
dotnet run --project src/backend/Saas.Subscription.Sample.Migrator
```

Exit code 0 = up to date, 1 = failed (message on stderr, no secrets), 2 = missing configuration.
Re-running is safe and a no-op once applied; concurrent runs serialize.

## Add a migration

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> \
  --project src/backend/Saas.Subscription.Sample.Infrastructure \
  --startup-project src/backend/Saas.Subscription.Sample.Migrator \
  --output-dir Persistence/Migrations
```

Review the generated file: anything EF cannot express (deferrable constraints) is hand-written SQL. A test
fails if the model and the migrations drift. Prefer backward-compatible changes; `Down` is for local use and
destructive changes need a documented rollback limitation first.

## Hosted database (Supabase) — to complete in DEP-01

Create a dedicated migration login (non-superuser with `CREATE` on the database) and a limited runtime login
with DML-only grants, and verify the runtime login cannot run DDL. Run the Migrator from CI or an operator
machine with TLS verification enabled and record the result. The tables live in `public`, which Supabase exposes
through its Data API by default: disable the Data API (or revoke `anon`/`authenticated` privileges) before any data is stored.
