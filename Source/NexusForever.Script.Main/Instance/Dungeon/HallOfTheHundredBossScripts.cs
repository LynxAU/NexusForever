using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Hall of the Hundred (WorldId 3009) - Boss Encounter Scripts
    //
    // Creature IDs confirmed via Creature2.tbl description search (w3009 tag).
    //
    // Harizog Coldblood (67444) appears in both this dungeon and Coldblood Citadel (75459).
    // Both share the same [CBC] combat spell set, so ColdbloodHarizogBase is reused.
    //
    // Spell data for Varegor (67457) and optional bosses sourced from Jabbithole
    // is not yet available - these remain EncounterBossScript stubs.
    //
    // -- Varegor the Abominable (mandatory) ---------------------------------------
    // Ice Gorganoth - World Story 2 mandatory boss.
    // No Jabbithole spell data found for this creature ID.

    /// <summary>Varegor the Abominable - Ice Gorganoth, mandatory boss. Creature2Id 67457.</summary>
    [ScriptFilterCreatureId(67457u)]
    public class HallVaregorScript : EncounterBossScript { }

    // -- Harizog Coldblood (final boss) -------------------------------------------
    // Part 6 - Hall of the Hundred. Uses the same [CBC] spell set as Coldblood Citadel.
    // Spells: 88198 Sovereign Slam, 88187 AoE Spikes, 88259 Jump To Player,
    //         88205 Blood Boil, 88210 Soulfrost Surge, 88202 Seeking Shadow,
    //         88186 Freezing Frenzy, 88317 Seeking Shadow Bomb.

    /// <summary>Harizog Coldblood - Part 6 final boss. Creature2Id 67444.</summary>
    [ScriptFilterCreatureId(67444u)]
    public class HallHarizogScript : ColdbloodHarizogBase { }

    // -- Optional / side encounter bosses -----------------------------------------

    /// <summary>Icebound Overlord - WS2 Ice Boss. Creature2Id 71577.</summary>
    [ScriptFilterCreatureId(71577u)]
    public class HallIceboundOverlordScript : EncounterBossScript { }

    /// <summary>Darkwitch Yotul - Osun Witch Optional Boss. Creature2Id 71173.</summary>
    [ScriptFilterCreatureId(71173u)]
    public class HallDarkwitchYotulScript : EncounterBossScript { }

    /// <summary>Unbound Flame Elemental - Optional Boss. Creature2Id 71414.</summary>
    [ScriptFilterCreatureId(71414u)]
    public class HallFlameElementalScript : EncounterBossScript { }
}
