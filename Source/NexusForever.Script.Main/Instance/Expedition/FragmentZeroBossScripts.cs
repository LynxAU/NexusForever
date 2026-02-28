using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    // Fragment Zero (WorldId 3180, ShiphandLevel6) - Boss Encounter Scripts
    //
    // The expedition has a normal (level 6) and veteran (level 50) version on the
    // same WorldId. Normal-mode uses lower creature IDs (67xxx range) and veteran
    // uses 69xxx range.
    //
    // Normal-mode bosses (level 6, Shiphand_level6 tag):
    //   67514 - Gronyx Boss (rock boss)
    //   67522 - Life-Overseer Boss
    //   67526 - Prototype Alpha (Xenobyte Boss 1)
    //   67527 - Prototype Beta  (Xenobyte Boss 2)
    //   67528 - Prototype Delta (Xenobyte Boss 3)
    //   69088 - Project "Matron" Boss
    //
    // Veteran-mode bosses (level 50 elite):
    //   69664 - Project "Matron" Boss (veteran)
    //   69672 - Prototype Alpha Veteran (Xenobyte Boss 1)
    //   69635 - Life-Overseer Boss (veteran)
    //   69673 - Prototype Beta Veteran  (Xenobyte Boss 2)
    //   69674 - Prototype Delta Veteran (Xenobyte Boss 3) - final boss
    //
    // No Jabbithole spell data sourced for these creatures yet.
    // All scripts are EncounterBossScript stubs (fire TriggerBossDeath on death).

    // -- Normal-mode bosses (level 6) --------------------------------------------

    /// <summary>Gronyx Boss - normal mode. Creature2Id 67514.</summary>
    [ScriptFilterCreatureId(67514u)]
    public class FragmentZeroGronyxNScript : EncounterBossScript { }

    /// <summary>Life-Overseer Boss - normal mode. Creature2Id 67522.</summary>
    [ScriptFilterCreatureId(67522u)]
    public class FragmentZeroLifeOverseerNScript : EncounterBossScript { }

    /// <summary>Prototype Alpha - Xenobyte Boss 1, normal mode. Creature2Id 67526.</summary>
    [ScriptFilterCreatureId(67526u)]
    public class FragmentZeroAlphaNScript : EncounterBossScript { }

    /// <summary>Prototype Beta - Xenobyte Boss 2, normal mode. Creature2Id 67527.</summary>
    [ScriptFilterCreatureId(67527u)]
    public class FragmentZeroBetaNScript : EncounterBossScript { }

    /// <summary>Prototype Delta - Xenobyte Boss 3, normal mode. Creature2Id 67528.</summary>
    [ScriptFilterCreatureId(67528u)]
    public class FragmentZeroDeltaNScript : EncounterBossScript { }

    /// <summary>Project "Matron" Boss - normal mode. Creature2Id 69088.</summary>
    [ScriptFilterCreatureId(69088u)]
    public class FragmentZeroMatronNScript : EncounterBossScript { }

    // -- Veteran-mode bosses (level 50 elite) ------------------------------------

    /// <summary>Project "Matron" Boss - veteran. Obj 4417, TG 12255. Creature2Id 69664.</summary>
    [ScriptFilterCreatureId(69664u)]
    public class FragmentZeroBoss1Script : EncounterBossScript { }

    /// <summary>Prototype Alpha Veteran - Xenobyte Boss 1. Obj 4423, TG 12269. Creature2Id 69672.</summary>
    [ScriptFilterCreatureId(69672u)]
    public class FragmentZeroBoss2Script : EncounterBossScript { }

    /// <summary>Life-Overseer Boss - veteran. Obj 4444, TG 12266. Creature2Id 69635.</summary>
    [ScriptFilterCreatureId(69635u)]
    public class FragmentZeroBoss3Script : EncounterBossScript { }

    /// <summary>Prototype Beta Veteran - Xenobyte Boss 2. Obj 4449, TG 12270. Creature2Id 69673.</summary>
    [ScriptFilterCreatureId(69673u)]
    public class FragmentZeroBoss4Script : EncounterBossScript { }

    /// <summary>Prototype Delta Veteran - Xenobyte Boss 3, final boss. Obj 4450, TG 12271. Creature2Id 69674.</summary>
    [ScriptFilterCreatureId(69674u)]
    public class FragmentZeroBoss5Script : EncounterBossScript { }
}
