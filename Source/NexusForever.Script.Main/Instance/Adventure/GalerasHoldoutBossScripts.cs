using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    // Galeras Holdout / Siege of Tempest Refuge (WorldId 1233)  Boss Encounter Scripts
    //
    // The adventure spawns different bosses based on player faction and random wave selection.
    // Jabbithole spell data available for Chief Engineer Vortec / "Dynamite" Dax:
    //   53346 | Wrench Strike  melee attack
    //   53347 | Wrench Strike  melee attack (variant)
    //
    // All bosses below fire TriggerBossDeath on death, enabling FallbackRequiredBossKills.

    //  Exile-side bosses (Dominion players fight these)

    /// <summary>Exile Warbot  Warbot Boss. Creature2Id 21544.</summary>
    [ScriptFilterCreatureId(21544u)]
    public class GalerasWarbotExileScript : EncounterBossScript { }

    /// <summary>Esper Alderblade  Esper Boss (Exile). Creature2Id 21545.</summary>
    [ScriptFilterCreatureId(21545u)]
    public class GalerasEsperExileScript : EncounterBossScript { }

    /// <summary>Elementalist Blazewood  Elementalist Boss (Exile). Creature2Id 21546.</summary>
    [ScriptFilterCreatureId(21546u)]
    public class GalerasElementalistExileScript : EncounterBossScript { }

    /// <summary>FCON Defense Tank  Tank Boss (Exile). Creature2Id 21547.</summary>
    [ScriptFilterCreatureId(21547u)]
    public class GalerasTankExileScript : EncounterBossScript { }

    /// <summary>Sergeant Berog  Berserker Boss (Exile). Creature2Id 21549.</summary>
    [ScriptFilterCreatureId(21549u)]
    public class GalerasBerserkerExileScript : EncounterBossScript { }

    /// <summary>Agent Blackwatch  Stalker Boss (Exile). Creature2Id 21550.</summary>
    [ScriptFilterCreatureId(21550u)]
    public class GalerasStalkerExileScript : EncounterBossScript { }

    /// <summary>"Dynamite" Dax  Engineer Boss (Exile). Creature2Id 21551.</summary>
    [ScriptFilterCreatureId(21551u)]
    public class GalerasEngineerExileScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 53346, initialDelay: 2.0, interval:  6.0); // Wrench Strike
            ScheduleSpell(spell4Id: 53347, initialDelay: 5.0, interval: 10.0); // Wrench Strike (variant)
            SetEnrage(seconds: 360.0, enrageSpellId: 53347);
        }
    }

    /// <summary>Spellslinger Boss (Exile). Creature2Id 21552.</summary>
    [ScriptFilterCreatureId(21552u)]
    public class GalerasSpellslingerExileScript : EncounterBossScript { }

    /// <summary>Evoker Peale  Sonic Boss (Exile). Creature2Id 21557.</summary>
    [ScriptFilterCreatureId(21557u)]
    public class GalerasEvokerExileScript : EncounterBossScript { }

    //  Dominion-side bosses (Exile players fight these)

    /// <summary>Esper Moko  Esper Boss (Dominion). Creature2Id 22166.</summary>
    [ScriptFilterCreatureId(22166u)]
    public class GalerasEsperDominionScript : EncounterBossScript { }

    /// <summary>Elementalist Boss (Dominion). Creature2Id 22167.</summary>
    [ScriptFilterCreatureId(22167u)]
    public class GalerasElementalistDominionScript : EncounterBossScript { }

    /// <summary>Berserker Boss (Dominion). Creature2Id 22168.</summary>
    [ScriptFilterCreatureId(22168u)]
    public class GalerasBerserkerDominionScript : EncounterBossScript { }

    /// <summary>Agent Razios  Stalker Boss (Dominion). Creature2Id 22169.</summary>
    [ScriptFilterCreatureId(22169u)]
    public class GalerasStalkerDominionScript : EncounterBossScript { }

    /// <summary>Chief Engineer Boss (Dominion). Creature2Id 22170.</summary>
    [ScriptFilterCreatureId(22170u)]
    public class GalerasEngineerDominionScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 53346, initialDelay: 2.0, interval:  6.0); // Wrench Strike
            ScheduleSpell(spell4Id: 53347, initialDelay: 5.0, interval: 10.0); // Wrench Strike (variant)
            SetEnrage(seconds: 360.0, enrageSpellId: 53347);
        }
    }

    /// <summary>Spellslinger Boss (Dominion). Creature2Id 22171.</summary>
    [ScriptFilterCreatureId(22171u)]
    public class GalerasSpellslingerDominionScript : EncounterBossScript { }

    /// <summary>Siren Aria  Sonic Boss (Dominion). Creature2Id 22176.</summary>
    [ScriptFilterCreatureId(22176u)]
    public class GalerasSirenDominionScript : EncounterBossScript { }

    /// <summary>Legion Assault Tank  Tank Boss (Dominion). Creature2Id 22199.</summary>
    [ScriptFilterCreatureId(22199u)]
    public class GalerasTankDominionScript : EncounterBossScript { }

    /// <summary>Warbot Boss  Disguise (Dominion). Creature2Id 22244.</summary>
    [ScriptFilterCreatureId(22244u)]
    public class GalerasWarbotDominionScript : EncounterBossScript { }

    //  Veteran variants (Exile-side)

    /// <summary>Exile Warbot  VETERAN. Creature2Id 51576.</summary>
    [ScriptFilterCreatureId(51576u)]
    public class GalerasWarbotExileVScript : EncounterBossScript { }

    /// <summary>Esper Alderblade  VETERAN (Exile). Creature2Id 51577.</summary>
    [ScriptFilterCreatureId(51577u)]
    public class GalerasEsperExileVScript : EncounterBossScript { }

    /// <summary>Elementalist Blazewood  VETERAN (Exile). Creature2Id 51578.</summary>
    [ScriptFilterCreatureId(51578u)]
    public class GalerasElementalistExileVScript : EncounterBossScript { }

    /// <summary>FCON Defense Tank  VETERAN (Exile). Creature2Id 51579.</summary>
    [ScriptFilterCreatureId(51579u)]
    public class GalerasTankExileVScript : EncounterBossScript { }

    /// <summary>Sergeant Berog  VETERAN (Exile). Creature2Id 51580.</summary>
    [ScriptFilterCreatureId(51580u)]
    public class GalerasBerserkerExileVScript : EncounterBossScript { }

    /// <summary>Agent Blackwatch  VETERAN (Exile). Creature2Id 51582.</summary>
    [ScriptFilterCreatureId(51582u)]
    public class GalerasStalkerExileVScript : EncounterBossScript { }

    /// <summary>"Dynamite" Dax  VETERAN Engineer Boss (Exile). Creature2Id 51583.</summary>
    [ScriptFilterCreatureId(51583u)]
    public class GalerasEngineerExileVScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 53346, initialDelay: 2.0, interval:  6.0); // Wrench Strike
            ScheduleSpell(spell4Id: 53347, initialDelay: 5.0, interval: 10.0); // Wrench Strike (variant)
            SetEnrage(seconds: 360.0, enrageSpellId: 53347);
        }
    }

    /// <summary>Spellslinger Boss  VETERAN (Exile). Creature2Id 51584.</summary>
    [ScriptFilterCreatureId(51584u)]
    public class GalerasSpellslingerExileVScript : EncounterBossScript { }

    /// <summary>Evoker Peale  VETERAN Sonic Boss (Exile). Creature2Id 51590.</summary>
    [ScriptFilterCreatureId(51590u)]
    public class GalerasEvokerExileVScript : EncounterBossScript { }
}
