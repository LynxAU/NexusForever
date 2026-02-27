using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Ruins of Kel Voreth (OsunDungeon, WorldId 1336) — Boss Encounter Scripts
    // Source: TargetGroup entries for PublicEvent 161 + Creature2.tbl name search.

    /// <summary>Grond the Corpsemaker, Normal. TG 3841.</summary>
    [ScriptFilterCreatureId(32534u)]
    public class KelVorethGrondNScript : EncounterBossScript { }

    /// <summary>Grond the Corpsemaker, Veteran. TG 3841.</summary>
    [ScriptFilterCreatureId(32535u)]
    public class KelVorethGrondVScript : EncounterBossScript { }

    /// <summary>Forgemaster Trogun — second boss, Normal.</summary>
    [ScriptFilterCreatureId(32531u)]
    public class KelVorethTrogunNScript : EncounterBossScript { }

    /// <summary>Forgemaster Trogun — second boss, Veteran.</summary>
    [ScriptFilterCreatureId(32533u)]
    public class KelVorethTrogunVScript : EncounterBossScript { }

    /// <summary>Slavemaster Drokk — final boss, Normal. TG 3842.</summary>
    [ScriptFilterCreatureId(32536u)]
    public class KelVorethDrokkNScript : EncounterBossScript { }

    /// <summary>Slavemaster Drokk — final boss, Veteran. TG 3842.</summary>
    [ScriptFilterCreatureId(32539u)]
    public class KelVorethDrokkVScript : EncounterBossScript { }

    /// <summary>Darkwitch Gurka (optional miniboss), Normal. TG 3850.</summary>
    [ScriptFilterCreatureId(33049u)]
    public class KelVorethGurkaNScript : EncounterBossScript { }

    /// <summary>Darkwitch Gurka (optional miniboss), Veteran. TG 3850.</summary>
    [ScriptFilterCreatureId(33050u)]
    public class KelVorethGurkaVScript : EncounterBossScript { }
}
