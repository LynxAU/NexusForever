using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Malgrave Trail adventure (WorldId 1181).
    ///
    /// Type: Survival/resource management  navigate the Malgrave desert managing
    /// supplies while fending off path-specific threat encounters.
    ///
    /// Boss encounters vary by player path (Soldier, Scientist, Explorer):
    ///   Murk (20814), Gurk (20828)          Soldier setpiece bosses
    ///   Elemental Horror (20584)             Scientist setpiece boss
    ///   Blisterbone Dreglord (19818)         Group encounter boss
    ///   Clanlord Vezrek (32687/53250)        Explorer encounter boss
    ///
    /// Uses FallbackRequiredBossKills because different paths spawn different bosses.
    /// </summary>
    [ScriptFilterOwnerId(1181)]
    public class MalgraveTrailScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad()
        {
            // No explicit waves  FallbackRequiredBossKills handles completion
            // regardless of which path-specific bosses spawn.
        }
    }
}
