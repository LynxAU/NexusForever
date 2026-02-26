## NexusForever
[![Discord](https://img.shields.io/discord/499473932131500034.svg?style=flat&logo=discord)](https://discord.gg/8wT3GEQ)

### Information
A server emulator for WildStar written in C# that supports build 16042.

### Getting Started
[Server Setup Guide](https://www.emulator.ws/installation/server-guide)

### Requirements
 * Visual Studio 2026 (.NET 10 and C# 14 support required)
 * MySQL Server (or equivalent, eg: MariaDB)
 * Message Broker (RabbitMQ or Azure Service Bus)
 * WildStar 16042 client

### Recent Local Progress (Community Fork)
This fork is actively restoring gameplay systems with parallel workstreams for quest objective coverage, spell/combat effect recovery, and NPC combat AI.

#### Integrated Progress (`main`)
Current integrated community-fork work (through `6893fad9`) includes:

 * Quest objective trigger coverage expansions:
   * `CollectItem`, `EarnCurrency`, `GatheResource`
   * `TalkTo` / `TalkToTargetGroup` (dialogue event `37`)
   * `ActivateEntity`, `ActivateTargetGroup`, `ActivateTargetGroupChecklist`
   * `SpellSuccess`, `SpellSuccess2`, `SpellSuccess3`, `SpellSuccess4`
   * `SucceedCSI` (via CSI result packet handling)
   * `CompleteQuest` (quest turn-in completion now updates dependent objectives)
   * `CompleteEvent`
   * `EnterZone`, `EnterArea`
   * `LearnTradeskill`, `ObtainSchematic`, `CraftSchematic`
 * CSI packet support for quest updates:
   * Added `ClientSpellInteractionResult` (`0x0805`) opcode and packet model.
   * Added server handler to resolve `CastingId` -> active spell and trigger `SucceedCSI`.
 * Combat stability and targeting fixes:
   * Failed spells no longer linger in `pendingSpells`.
   * Finished spells are disposed correctly.
   * Spell event callback retry-loop bug fixed.
   * Player cast `PrimaryTargetId` propagation fixed.
 * Combat effect support and math recovery:
   * Implemented `Heal`, `HealShields`, `DamageShields`, `Transference` (initial path)
   * Implemented `Absorption`, `HealingAbsorption`, `ModifyInterruptArmor`
   * Implemented `VitalModifier`, `SapVital`, `ClampVital` (with current `DataBits` assumptions documented in code)
   * Added entity vital APIs (`GetVitalValue` / `ModifyVital`) for health/shield/resources/interrupt armor
   * Improved `DamageCalculator` heal handling, mitigation correctness, formula fallbacks, and level guards

Ignored local-only file:
 * `NexusForever.code-workspace` (ignored via `.gitignore`)

#### Quest Objective Coverage Notes
Working objective hooks now include:

 * `CollectItem`
 * `EarnCurrency`
 * `GatheResource`
 * `KillCreature` / `KillCreature2`
 * `KillTargetGroup` / `KillTargetGroups`
 * `TalkTo` / `TalkToTargetGroup`
 * `ActivateEntity` / `ActivateTargetGroup` / `ActivateTargetGroupChecklist`
 * `SpellSuccess` / `SpellSuccess2` / `SpellSuccess3` / `SpellSuccess4`
 * `SucceedCSI`
 * `CompleteQuest`
 * `CompleteEvent`
 * `EnterZone`
 * `EnterArea`
 * `LearnTradeskill`
 * `ObtainSchematic`
 * `CraftSchematic`

Current placeholder caveats (needs follow-up when crafting/area systems are fully wired):

 * `LearnTradeskill`, `ObtainSchematic`, `CraftSchematic` are currently triggered from interaction open events and use placeholder data paths in some cases.
 * `EnterArea` currently uses a placeholder trigger path and should be replaced with proper world-location/area tracking.
 * `SucceedCSI` depends on CSI cast tracking and `PrimaryTargetId` being valid for the interaction spell.

#### Active Branch Work (Not Yet Merged to `main`)
Parallel branch work in progress:

 * `combat/npc-ai-basic` (`6d3f82e3`, `c455e1a3`)
   * NPC combat AI foundations: aggro scan, chase, leash/evade, and combat script hooks
   * `ICreatureEntity.AggroRadius`
   * `UnitEntity` enter/exit combat callbacks for scripts
   * `INonPlayerScript` / `IUnitScript` combat lifecycle hooks
   * Added glance amount propagation into damage descriptions and combat logs
 * `codex/combat-effects` (`de2b9d22`, `b9f49cd6`)
   * Effect-specific combat log routing for `DamageShields` and `Transference`
   * Implemented `Kill` spell effect handler
   * Branch-local compile fix for `EnterArea` debug logging (`NLog` API mismatch)

#### Combat Recovery Notes (Current Passes)
 * Combat effect handler coverage has increased to `29 / 136` non-`UNUSED` spell effect types (~`21.3%`) on `codex/combat-effects`.
 * Core damage/heal/shield flows now emit more accurate combat logs (including glance, shield-damage logs, and transference log routing).
 * Vitals/resource manipulation support now exists at the entity layer, which unlocks more class mechanics and utility effects.
 * NPC combat behavior work is progressing in parallel and can now build on restored threat/combat-state hooks.

### Branches
NexusForever has multiple branches:
* **[Master](https://github.com/NexusForever/NexusForever/tree/master)**  
Latest stable release, develop is merged into master once enough content has accumulated in develop.  
Compiled binary releases are based on this branch.
* **[Game Rework](https://github.com/NexusForever/NexusForever/tree/game_rework)**  
Current active development branch, major refactors and updates to the project are underway in this branch.  
All PR's should be targeted to this branch.  
This branch will eventually be merged back into develop.  
* **[Develop](https://github.com/NexusForever/NexusForever/tree/develop)**  
~~Active development branch with the latest features but may be unstable.  
Any new pull requests must be targed towards this branch.~~

### Links
 * [Website](https://emulator.ws)
 * [Discord](https://discord.gg/8wT3GEQ)
 * [World Database](https://github.com/NexusForever/NexusForever.WorldDatabase)

## Build Status
### Windows
Automated builds that will run on Windows or Windows Server.

Master:  
![Master](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Master%20Windows)  
Game Rework:  
![Game Rework](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Windows?branchName=game_rework)  
Development:  
![Development](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Windows?branchName=develop)
### Linux
Automated builds that will run on various Linux distributions.  
See the [.NET runtime identifer documentation](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids)  for more information on exact distributions.

Master:  
![Master](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Master%20Linux)  
Game Rework:  
![Game Rework](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Linux?branchName=game_rework)  
Development:  
![Development](https://dev.azure.com/NexusForever/NexusForever/_apis/build/status/NexusForever%20Develop%20Linux?branchName=develop)
