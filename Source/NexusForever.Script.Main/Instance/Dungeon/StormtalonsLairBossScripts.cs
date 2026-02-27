using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Stormtalon's Lair (EthnDunon, WorldId 382) — Boss Encounter Scripts
    // Source: TargetGroup entries for PublicEvent 145.

    /// <summary>Invoker — final form, Normal difficulty. TG 2585.</summary>
    [ScriptFilterCreatureId(17160u)]
    public class StormtalonsLairInvokerNScript : EncounterBossScript { }

    /// <summary>Invoker — final form, Veteran difficulty. TG 2585.</summary>
    [ScriptFilterCreatureId(33405u)]
    public class StormtalonsLairInvokerVScript : EncounterBossScript { }

    /// <summary>Aethros — final boss, Normal difficulty. TG 2586.</summary>
    [ScriptFilterCreatureId(17166u)]
    public class StormtalonsLairAethrosNScript : EncounterBossScript { }

    /// <summary>Aethros — final boss, Veteran difficulty. TG 2586.</summary>
    [ScriptFilterCreatureId(32703u)]
    public class StormtalonsLairAethrosVScript : EncounterBossScript { }

    /// <summary>Arcanist Breeze-Binder — miniboss, Normal difficulty. TG 3030.</summary>
    [ScriptFilterCreatureId(24474u)]
    public class StormtalonsLairBreezebinderNScript : EncounterBossScript { }

    /// <summary>Arcanist Breeze-Binder — miniboss, Veteran difficulty. TG 3030.</summary>
    [ScriptFilterCreatureId(34711u)]
    public class StormtalonsLairBreezebinderVScript : EncounterBossScript { }

    /// <summary>Overseer Drift-Catcher — miniboss, Normal difficulty. TG 3921.</summary>
    [ScriptFilterCreatureId(33361u)]
    public class StormtalonsLairDriftcatcherNScript : EncounterBossScript { }

    /// <summary>Overseer Drift-Catcher — miniboss, Veteran difficulty. TG 3921.</summary>
    [ScriptFilterCreatureId(33362u)]
    public class StormtalonsLairDriftcatcherVScript : EncounterBossScript { }
}
