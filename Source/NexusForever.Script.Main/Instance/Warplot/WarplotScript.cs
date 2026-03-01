using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Warplot
{
    /// <summary>
    /// Map script stub for Warplot PvP content.
    ///
    /// Warplots are 20v20 PvP battles on customised housing-style plots.
    /// Each WarParty defends/attacks objectives on their opponent's plot.
    ///
    /// TODO: Determine the actual WorldId from Map\PvPWarPlot* in the GameTable.
    ///       Seed the map_entrance row for MatchType 4 (Warplot) once the WorldId is known.
    ///       Implement objective-based win condition (Nexus Core destruction or ticket depletion).
    ///
    /// WorldId: TBD — search Map table for paths starting with "Map\PvPWarPlot".
    /// </summary>
    // [ScriptFilterOwnerId(TBD_WORLD_ID)]
    public class WarplotScript // : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // private IContentMapInstance owner;

        // public void OnLoad(IContentMapInstance owner)
        // {
        //     this.owner = owner;
        // }

        // public void OnBossDeath(uint creatureId)
        // {
        //     // TODO: warplot win condition (nexus core creature death or ticket pool)
        // }

        // public void OnEncounterReset()
        // {
        // }
    }
}
