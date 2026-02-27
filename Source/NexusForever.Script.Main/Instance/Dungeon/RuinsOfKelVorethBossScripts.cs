using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Ruins of Kel Voreth (OsunDungeon, WorldId 1336) — Boss Encounter Scripts
    // Source: TargetGroup entries for PublicEvent 161.

    /// <summary>Grond the Corpsemaker, Normal. TG 3841.</summary>
    [ScriptFilterCreatureId(32534u)]
    public class KelVorethGrondNScript : EncounterBossScript { }

    /// <summary>Grond the Corpsemaker, Veteran. TG 3841.</summary>
    [ScriptFilterCreatureId(32535u)]
    public class KelVorethGrondVScript : EncounterBossScript { }

    /// <summary>Drokk, Normal. TG 3842.</summary>
    [ScriptFilterCreatureId(32536u)]
    public class KelVorethDrokkNScript : EncounterBossScript { }

    /// <summary>Drokk, Veteran. TG 3842.</summary>
    [ScriptFilterCreatureId(32539u)]
    public class KelVorethDrokkVScript : EncounterBossScript { }

    /// <summary>Darkwitch Gurka (miniboss), Normal. TG 3850.</summary>
    [ScriptFilterCreatureId(33049u)]
    public class KelVorethGurkaNScript : EncounterBossScript { }

    /// <summary>Darkwitch Gurka (miniboss), Veteran. TG 3850.</summary>
    [ScriptFilterCreatureId(33050u)]
    public class KelVorethGurkaVScript : EncounterBossScript { }
}
