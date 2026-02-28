# NexusForever Project Status (Agent Handoff)

Last updated: 2026-02-28  
Audience: Agent onboarding (Qwen)  
Scope: What changed in the last 2 days, what is validated, what remains.

## 1) Executive Summary

- Core emulator systems are functional enough for focused parity work.
- Biggest progress was data pipeline and benchmarking workflow for encounter/spell parity.
- We now have an end-to-end, repeatable way to harvest WildStar Logs CSV combat data at scale without requiring API auth.
- We also established a realistic benchmark standard: use **P75 kill data** (not single logs) for tuning baselines.

## 2) Completion Semantics (Project Standard)

This project now treats percentages as two separate metrics:

- **Implementation Baseline %**: feature/system coverage in code.
- **Live Fidelity %**: how close behavior is to retail WildStar end-to-end.

`100%` means live-accurate behavior from input -> server logic -> outcomes, including edge cases and interactions.

## 3) Major Breakthroughs (Last 2 Days)

## A) Game table extraction workflow stabilized

We confirmed and documented working extraction/parsing patterns for client `.tbl` data:

- Table location:
  - `tmp/tables/tbl/*.tbl`
- Parser references:
  - `tmp/parse_dungeons3.py`
  - `tmp/parse_raid_ach.py`
  - `tmp/debug_tbl.py`
  - `tmp/find_entrances.py`

This removed a lot of guesswork around world/spawn/spell metadata access.

## B) Loot/bootstrap data work materially advanced

Generated/importable SQL sets and probes for loot/source reconstruction:

- `tmp/loot_ga.sql`
- `tmp/loot_dungeons.sql`
- `tmp/loot_datascape.sql`
- `tmp/loot_ultiproto.sql`
- Scraper/probe: `tmp/scrape_jabbit_loot.py`

## C) WildStar Logs scraping path unblocked (critical)

Initial attempt to use `/v1` API is valid technically, but currently key-gated:

- `/v1/docsjson` exists and enumerates API routes.
- `/v1/*` returns `401 Invalid key specified` without key.

So we built a no-key fallback using real browser automation.

### Implemented tools

- API-key path (kept for future if key available):
  - `tmp/wildstarlogs/fetch_top10_boss_csv.py`
- No-key Playwright path (currently primary):
  - `tmp/wildstarlogs/fetch_top10_boss_csv_playwright.py`

This tool:
1. Opens zone rankings
2. Iterates bosses
3. Pulls top N ranking report links
4. Opens each report
5. Clicks CSV export
6. Saves CSV + JSON metadata + manifest

## D) Correct metric extraction fixed

We validated that default ranking links can export the wrong perspective (player/source totals).  
We patched the scraper to force query parameters for encounter-side ability baselines:

- `type=damage-done`
- `by=ability`
- `hostility=1`

This produces enemy ability damage tables suitable for boss tuning.

## E) Full GA top-10 enemy ability harvest completed

Output directory:

- `tmp/wildstarlogs/top10_csv_ui_enemy/`

Result:

- 16 bosses discovered
- 10 logs each
- 160/160 successful exports
- Manifest:
  - `tmp/wildstarlogs/top10_csv_ui_enemy/manifest.json`

## F) P75 baseline builder implemented

Built aggregator to produce per-boss/per-ability benchmark stats from harvested CSVs:

- Script:
  - `tmp/wildstarlogs/build_enemy_ability_baseline.py`
- Outputs:
  - `tmp/wildstarlogs/top10_csv_ui_enemy/enemy_ability_baseline_p75.csv`
  - `tmp/wildstarlogs/top10_csv_ui_enemy/enemy_ability_baseline_p75.json`
  - `tmp/wildstarlogs/top10_csv_ui_enemy/enemy_ability_baseline_p75.md`

The baseline includes p50/p75/p90 context (with P75 as primary target).

## 4) Verified Current Workflow

## Harvest (no API key required)

```powershell
python tmp\wildstarlogs\fetch_top10_boss_csv_playwright.py --zone-id 5 --top 10 --by ability --hostility 1 --out-dir tmp\wildstarlogs\top10_csv_ui_enemy
```

## Build tuning baseline (P75)

```powershell
python tmp\wildstarlogs\build_enemy_ability_baseline.py --input-dir tmp\wildstarlogs\top10_csv_ui_enemy --out-dir tmp\wildstarlogs\top10_csv_ui_enemy
```

## 5) Practical Interpretation of the Data

Use these fields with care:

- Comp/gear sensitive:
  - `Amount`, `DPS`, `Hits`
- Better baseline signals:
  - `Share %` per ability
  - `Avg Hit` percentile bands (plus context)
- Standard target:
  - Tune toward **P75** kill profiles, not single report snapshots.

## 6) Current Known Gaps / Risks

- API-key route still blocked unless valid WildStar Logs key is obtained (non-blocking now).
- Some legacy/unreleased encounters (Hall/Infinite placeholders) should not be treated as parity blockers.
- Benchmarking is strong now; final retail parity still requires implementation work in spell effects/AI encounter logic and validation loops.

## 7) Immediate Next Steps for Qwen

1. Consume `enemy_ability_baseline_p75.csv` and map ability names to internal spell IDs/tuning hooks.
2. Implement server-side tuning passes per boss ability using P75 as baseline target.
3. Re-run scrape + baseline after each tuning wave to track movement.
4. Add diff tooling (baseline vs current emulator logs) for regression detection.
5. Prioritize high-impact bosses first (largest outgoing ability share spread).

## 8) Important Context for Multi-Agent Work

- Do not overwrite unrelated in-progress work from other agents.
- Keep reporting split into:
  - Implementation Baseline %
  - Live Fidelity %
- Treat “done” claims as fidelity-tested, not just “implemented in code”.

## 9) Key Artifact Index

- Root reference docs:
  - `../Todo.md`
  - `../sharedinfo.md`
  - `../TEAM_WORKFLOW.md`
- Table extraction/parsing:
  - `tmp/parse_dungeons3.py`
  - `tmp/parse_raid_ach.py`
  - `tmp/debug_tbl.py`
- Logs harvesting:
  - `tmp/wildstarlogs/fetch_top10_boss_csv_playwright.py`
  - `tmp/wildstarlogs/top10_csv_ui_enemy/manifest.json`
- Baselines:
  - `tmp/wildstarlogs/build_enemy_ability_baseline.py`
  - `tmp/wildstarlogs/top10_csv_ui_enemy/enemy_ability_baseline_p75.csv`

