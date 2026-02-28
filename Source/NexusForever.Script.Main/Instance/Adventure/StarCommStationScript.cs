using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for the Astrovoid Prison adventure (WorldId 1437, AdventureAstrovoidPrison).
    ///
    /// NOTE: This adventure was previously mislabelled as "Star-Comm Station".
    /// World.tbl asset path confirms WorldId 1437 = Map\AdventureAstrovoidPrison.
    ///
    /// Type: Combat - players fight through a Dominion prison in the Astrovoid.
    ///
    /// Boss creatures (confirmed from Creature2.tbl w1437 description search):
    ///   Warden Rhadman           - Creature2Id 27444
    ///   Professor Goldbough (N3) - Creature2Id 48517
    ///   Professor Goldbough (N5) - Creature2Id 48846
    ///   Gadget-Equipped Prisoner - Creature2Id 49720
    ///
    /// FallbackRequiredBossKills = 3 (three of the four bosses must die).
    /// </summary>
    [ScriptFilterOwnerId(1437)]
    public class StarCommStationScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
