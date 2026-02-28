using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Galeras Holdout / Siege of Tempest Refuge adventure (WorldId 1233).
    ///
    /// Type: Wave defense  players hold a fortified position against assault waves.
    /// Boss encounters vary by player faction (Exile vs Dominion) and random selection.
    /// Over 20 possible boss creatures exist (class-specific bosses, tank bosses, etc.)
    /// with both normal (level 30-50) and veteran (level 50, Tier 3) variants.
    ///
    /// Uses FallbackRequiredBossKills because different factions/paths face different bosses.
    /// </summary>
    [ScriptFilterOwnerId(1233)]
    public class GalerasHoldoutScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad()
        {
            // No explicit waves  FallbackRequiredBossKills handles completion
            // regardless of which faction-specific bosses spawn.
        }
    }
}
