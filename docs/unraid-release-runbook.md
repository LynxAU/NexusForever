# Unraid Release Runbook

## Share Layout (cache pool)
- `nexusforever/app` - git checkout and deployment scripts
- `nexusforever/releases` - versioned published builds
- `nexusforever/current` - active release symlink
- `nexusforever/config` - environment-specific config/secrets
- `nexusforever/data` - persistent runtime data
- `nexusforever/logs` - logs
- `nexusforever/backups` - local short-term backups

## Release Steps
1. Pull latest source and checkout target tag/commit.
2. Build/publish release artifact. The release bundles deploy-critical world data from `assets/tbl` and `assets/map`.
3. Backup database.
4. Run schema migrations (fail-fast).
5. Switch `current` symlink to new release.
6. Restart services and run health checks.
7. If healthy, keep release and record tag.
8. If unhealthy, rollback symlink and restart previous release.

## Rollback
1. Point `current` to previous release.
2. Restart services.
3. If needed, restore DB from pre-release backup.
4. Capture incident notes before next attempt.
