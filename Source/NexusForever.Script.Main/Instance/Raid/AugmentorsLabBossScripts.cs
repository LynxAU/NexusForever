using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Augmentors' Lab (AugmentorsLab, WorldId 3040) — Boss Encounter Scripts
    // Source: Creature2.tbl "[IC]" (Infinite Crimelabs) name tag search.
    // All entries belong to encounter e2681 - Augmentors.

    /// <summary>Augmenters God Unit — final boss. Creature2Id 50979.</summary>
    [ScriptFilterCreatureId(50979u)]
    public class ICAugmentersGodUnitScript : EncounterBossScript { }

    /// <summary>Prime Evolutionary Operant. Creature2Id 50472.</summary>
    [ScriptFilterCreatureId(50472u)]
    public class ICPrimeEvolutionaryOperantScript : EncounterBossScript { }

    /// <summary>Phaged Evolutionary Operant. Creature2Id 50423.</summary>
    [ScriptFilterCreatureId(50423u)]
    public class ICPhagedEvolutionaryOperantScript : EncounterBossScript { }

    /// <summary>Chestacabra. Creature2Id 50425.</summary>
    [ScriptFilterCreatureId(50425u)]
    public class ICChestacabraScript : EncounterBossScript { }

    /// <summary>Circuit Breaker. Creature2Id 61597.</summary>
    [ScriptFilterCreatureId(61597u)]
    public class ICCircuitBreakerScript : EncounterBossScript { }
}
