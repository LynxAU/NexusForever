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
3. Create pre-deploy backup for live auth/world release dirs and config files.
4. Backup database.
5. Run schema migrations (fail-fast).
6. Switch `current` symlink to new release.
7. Restart services and run health checks.
8. Run post-deploy verification (containers + log sanity).
9. If healthy, keep release and record tag.
10. If unhealthy, restore pre-deploy backup and restart previous runtime.

## One-Command Deploy (recommended)
On Unraid:

```bash
./scripts/deploy/unraid_live_rebuild.sh /mnt/user/nexusforever-live/repo main origin
```

This command includes:
- dependency preflight checks
- pre-deploy backup
- rebuild/publish of auth + world
- restart of `nf_iso_auth` and `nf_iso_world`
- health checks + recent log verification

## Canonical nf_iso Stack Compose
Use:
- `scripts/deploy/unraid/docker-compose.nf_iso.yml`
- `scripts/deploy/unraid/.env.example`
- `scripts/deploy/unraid/bootstrap_nf_iso_stack.sh`

Bootstrap/recreate stack:

```bash
cd /mnt/user/nexusforever-live/repo/scripts/deploy/unraid
cp .env.example .env
docker compose -f docker-compose.nf_iso.yml --env-file .env up -d
```

If the stack drifts (missing containers/network), run:

```bash
./bootstrap_nf_iso_stack.sh .env
```

## Rollback
1. Restore the latest pre-deploy backup for auth/world release dirs and config.
2. Restart `nf_iso_auth` and `nf_iso_world`.
3. If needed, restore DB from pre-release backup.
4. Capture incident notes before next attempt.
