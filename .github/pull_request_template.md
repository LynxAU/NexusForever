## Summary
- What changed:
- Why:

## Validation
- [ ] `dotnet build` passed for affected projects
- [ ] Manual smoke test performed for changed feature

## Database Checklist (required if schema/data changed)
- [ ] Added a new forward migration (no edits to applied migrations)
- [ ] Migration applies cleanly on a fresh database
- [ ] Rollback behavior for newest migration verified
- [ ] Raw SQL migrations are deterministic (ordered by filename)
- [ ] Migration failures stop execution (no partial-continue behavior)

## Gameplay/Script Checklist (required if encounter/content changed)
- [ ] Creature IDs verified against spawned entities
- [ ] Encounter completion and cleanup paths tested
- [ ] Added/updated comments for source confidence where data is inferred

## Release Risk
- Risk level: Low / Medium / High
- Rollback plan:
