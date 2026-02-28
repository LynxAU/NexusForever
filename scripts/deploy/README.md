# Deploy Scripts

## Scripts
- `release.sh` - publish a release artifact from source and bundle tracked game data from `assets/tbl` and `assets/map`.
- `deploy.sh` - copy a release into the release store and switch `current`.
- `rollback.sh` - switch `current` back to a previous release.
- `migrate.sh` - run DB migrations via `NexusForever.Aspire.Database.Migrations`.
- `healthcheck.sh` - simple TCP reachability check for service startup.
- `create_deploy_backup.sh` - snapshot auth/world release directories and config files.
- `restore_deploy_backup.sh` - restore auth/world release directories and config files from a backup snapshot.
- `verify_live_stack.sh` - verify auth/world container state, port reachability, and recent fatal logs.
- `unraid_live_rebuild.sh` - pull a branch on Unraid, rebuild `AuthServer` and `WorldServer`, sync into live bind-mount paths, restart live containers, and run health checks.

## Typical Flow
1. `release.sh` to build a new version.
2. Backup database.
3. `migrate.sh` to apply schema changes.
4. `deploy.sh` to switch active release.
5. Restart service.
6. `healthcheck.sh` to validate service is up.
7. If needed, `rollback.sh` to revert active release.

## Unraid One-Command Live Deploy
Run this on Unraid from your checked-out repo path:

```bash
./scripts/deploy/unraid_live_rebuild.sh /mnt/user/nexusforever-live/repo main origin
```

Defaults:
- Containers: `nf_iso_auth`, `nf_iso_world`
- Health ports: `23115`, `24000`
- Build: `dotnet publish -c Release -r linux-x64 --self-contained true`

Optional overrides:
- `AUTH_CONTAINER`, `WORLD_CONTAINER`
- `AUTH_PORT`, `WORLD_PORT`
- `HEALTH_HOST`, `HEALTH_TIMEOUT`
- `MYSQL_HOST`, `MYSQL_PORT`
- `RABBIT_HOST`, `RABBIT_PORT`
- `DEPENDENCY_TIMEOUT`
- `AUTH_CONFIG_DEST`, `WORLD_CONFIG_DEST`
- `BACKUP_ROOT`, `CREATE_BACKUP`
- `AUTO_RESTORE_ON_FAILURE`, `LOG_WINDOW_SECONDS`
- `MANIFEST_ROOT`

Behavior:
- Performs dependency preflight checks for MySQL and RabbitMQ before stopping live auth/world.
- If deploy fails after auth/world are stopped, it attempts automatic recovery by starting both containers.
- Creates a pre-deploy backup (release dirs + config) and can automatically restore on deploy failure.
- Writes a release manifest (`release_id`, git SHA, mount paths, backup path) for auditability.

## Canonical Unraid Compose
Files:
- `scripts/deploy/unraid/docker-compose.nf_iso.yml`
- `scripts/deploy/unraid/.env.example`

Boot/recreate stack on Unraid:

```bash
cd /mnt/user/nexusforever-live/repo/scripts/deploy/unraid
cp .env.example .env
docker compose -f docker-compose.nf_iso.yml --env-file .env up -d
```
