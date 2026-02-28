# Database Migration Workflow

## Rules
- Never edit an already-applied migration. Create a new forward migration.
- Keep schema migrations and optional data imports separate.
- Raw SQL migrations must execute in deterministic filename order.
- Migration execution must fail fast on first error.

## Naming
- EF migrations: default timestamp prefix from EF tooling.
- Raw SQL: `YYYYMMDDHHMMSS_description.sql`.

## PR Requirements
- Build passes for affected projects.
- Migration applies from empty database.
- Newest migration rollback behavior validated.
- Notes include any operational impact.

## Runtime Strategy
- Dev/test: startup migration is allowed.
- Production: run migration job before app switch-over.
- Optional world-data imports run in a separate step from required schema.

## Deployment Safety
- Backup DB before migration.
- If migration fails, stop deployment and rollback app/db according to runbook.
- Tag release after successful migrate + health checks.
