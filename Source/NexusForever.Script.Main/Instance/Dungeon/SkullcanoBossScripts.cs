using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Skullcano (WorldId 1263) — Boss Encounter Scripts
    // Source: TargetGroup entries for PublicEvent 148.

    /// <summary>Stew-Shaman Tugga, Normal. TG 2599.</summary>
    [ScriptFilterCreatureId(24493u)]
    public class SkullcanoTuggaNScript : EncounterBossScript { }

    /// <summary>Stew-Shaman Tugga, Veteran. TG 2599.</summary>
    [ScriptFilterCreatureId(24898u)]
    public class SkullcanoTuggaVScript : EncounterBossScript { }

    /// <summary>Thunderfoot, Normal. TG 2600.</summary>
    [ScriptFilterCreatureId(24475u)]
    public class SkullcanoThunderfootNScript : EncounterBossScript { }

    /// <summary>Thunderfoot, Veteran. TG 2600.</summary>
    [ScriptFilterCreatureId(24893u)]
    public class SkullcanoThunderfootVScript : EncounterBossScript { }

    /// <summary>Bosun Octog, Normal. TG 2601.</summary>
    [ScriptFilterCreatureId(24486u)]
    public class SkullcanoOctogNScript : EncounterBossScript { }

    /// <summary>Bosun Octog, Veteran. TG 2601.</summary>
    [ScriptFilterCreatureId(24894u)]
    public class SkullcanoOctogVScript : EncounterBossScript { }

    /// <summary>Quartermaster Gruh (miniboss), Normal. TG 2869.</summary>
    [ScriptFilterCreatureId(24490u)]
    public class SkullcanoGruhNScript : EncounterBossScript { }

    /// <summary>Quartermaster Gruh (miniboss), Veteran. TG 2869.</summary>
    [ScriptFilterCreatureId(24896u)]
    public class SkullcanoGruhVScript : EncounterBossScript { }
}
