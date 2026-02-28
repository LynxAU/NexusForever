using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    // Astrovoid Prison (WorldId 1437, AdventureAstrovoidPrison) - Boss Encounter Scripts
    //
    // NOTE: This adventure was previously mislabelled as "Star-Comm Station".
    // World.tbl asset path confirms WorldId 1437 = Map\AdventureAstrovoidPrison.
    //
    // Boss creature IDs confirmed from Creature2.tbl w1437 description search:
    //   27444 - Warden Rhadman            (prison warden boss)
    //   48517 - Professor Goldbough       (Nexus-3 variant)
    //   48846 - Professor Goldbough       (Nexus-5 variant)
    //   49720 - Gadget-Equipped Prisoner  (Nexus-5 prisoner boss)
    //
    // No Jabbithole spell data sourced for these creatures yet.
    // All scripts are EncounterBossScript stubs (fire TriggerBossDeath on death).

    /// <summary>Warden Rhadman - prison warden boss. Creature2Id 27444.</summary>
    [ScriptFilterCreatureId(27444u)]
    public class AstrovoidWardenRhadmanScript : EncounterBossScript { }

    /// <summary>Professor Goldbough - Nexus-3 variant. Creature2Id 48517.</summary>
    [ScriptFilterCreatureId(48517u)]
    public class AstrovoidGoldboughN3Script : EncounterBossScript { }

    /// <summary>Professor Goldbough - Nexus-5 variant. Creature2Id 48846.</summary>
    [ScriptFilterCreatureId(48846u)]
    public class AstrovoidGoldboughN5Script : EncounterBossScript { }

    /// <summary>Gadget-Equipped Prisoner - Nexus-5 prisoner boss. Creature2Id 49720.</summary>
    [ScriptFilterCreatureId(49720u)]
    public class AstrovoidGadgetPrisonerScript : EncounterBossScript { }
}
