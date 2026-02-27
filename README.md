## NexusForever
[![Discord](https://img.shields.io/discord/499473932131500034.svg?style=flat&logo=discord)](https://discord.gg/8wT3GEQ)

### Information
A server emulator for WildStar written in C# that supports build 16042.

### Completion Standard
Project progress uses two separate metrics:

- `Implementation Baseline %`: server-side feature/code coverage.
- `Live Fidelity %`: how closely gameplay matches live WildStar behavior end-to-end.

`100% complete` means **Live Fidelity 100%** with reproducible validation (tests/replays/log comparisons), not only implemented handlers or successful builds.

### Getting Started
[Server Setup Guide](https://www.emulator.ws/installation/server-guide)

### Requirements
 * Visual Studio 2026 (.NET 10 and C# 14 support required)
 * MySQL Server (or equivalent, eg: MariaDB)
 * Message Broker (RabbitMQ or Azure Service Bus)
 * WildStar 16042 client

### Recent Local Progress (Community Fork)
This fork is actively restoring gameplay systems, with a current focus on quest objective coverage and combat recovery.

#### Git Changes Summary (Current Working Set)
Modified files:
 * `Source/NexusForever.Game/Combat/DamageCalculator.cs`
   * Fixed healing/shield spell handling (no armor/deflect/glancing on heal-like effects).
   * Proper heal damage typing and healing multiplier handling.
   * Fixed physical armor mitigation offset usage.
   * Improved crit severity handling and combat RNG usage.
 * `Source/NexusForever.Game/Entity/CurrencyManager.cs`
   * Added `EarnCurrency` quest objective triggers.
 * `Source/NexusForever.Game/Entity/HarvestUnitEntity.cs`
   * Complete harvest system updates including tradeskill -> material mapping, auto-respawn via `RescanCooldown`, and `GatheResource` quest triggers.
 * `Source/NexusForever.Game/Entity/Inventory.cs`
   * Added `CollectItem` quest objective triggers.
 * `Source/NexusForever.Game/Entity/UnitEntity.cs`
   * Fixed spell lifecycle cleanup and disposal (prevents leaked/failed spells from lingering in `pendingSpells`).
 * `Source/NexusForever.Game/Spell/CharacterSpell.cs`
   * Added `PrimaryTargetId` propagation for player cast target tracking.
 * `Source/NexusForever.Game/Spell/Event/SpellEventManager.cs`
   * Fixed event timing/cleanup bug (prevents retry loops when spell callbacks fail).
 * `Source/NexusForever.Game/Spell/Spell.cs`
   * Added `SpellSuccess`, `SpellSuccess2`, `SpellSuccess3`, `SpellSuccess4` quest triggers.
   * Fixed target selection / primary target usage in spell start packets.
   * Added effect-handler exception guards for safer spell execution.
 * `Source/NexusForever.Game/Spell/SpellEffectHandler.cs`
   * Added `Heal`, `HealShields`, `DamageShields`, and `Transference` handlers.
   * Added provisional support paths for `DistanceDependentDamage` and `DistributedDamage`.
   * Added heal combat log generation and hardened damage handler null/deflect behavior.
 * `Source/NexusForever.WorldServer/Network/Message/Handler/Entity/ClientEntityInteractionHandler.cs`
   * Fixed `TalkTo` / `ActivateEntity` quest objective bug (`TalkTo` now only triggers for event `37`).

Ignored local-only file:
 * `NexusForever.code-workspace` (ignored via `.gitignore`)

#### Quest Objectives Now Working
 * `CollectItem` (inventory pickup)
 * `SpellSuccess` / `SpellSuccess2` / `SpellSuccess3` / `SpellSuccess4`
 * `TalkTo` (event `37` only)
 * `ActivateEntity` (other interaction events)
 * `GatheResource` (harvest nodes)
 * `EarnCurrency` (currency gains)

#### Combat Recovery Notes (Current Passes)
 * Spell lifecycle stability improved (failed casts no longer linger; finished spells are disposed).
 * Primary target tracking is now propagated from player casts into spell execution.
 * Heal and shield-heal spell effects now apply results and emit combat logs.
 * Several damage-like spell effects are now routed through the core damage path as interim support.
 * Combat handler coverage has increased, improving playability while deeper effect semantics are restored incrementally.

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
