using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Hycrest Insurrection adventure (WorldId 1149).
    ///
    /// Type: Wave defense — players hold Hycrest Hamlet against multiple invasion waves
    /// culminating in a final commander fight.
    ///
    /// Boss creature IDs are wired via <c>FallbackAdventureBossScripts.cs</c>; completion
    /// remains fallback-based until wave sequencing is fully scripted.
    /// </summary>
    [ScriptFilterOwnerId(1149)]
    public class HycrestInsurrectionScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 2;

        protected override void OnAdventureLoad() { }
    }
}
