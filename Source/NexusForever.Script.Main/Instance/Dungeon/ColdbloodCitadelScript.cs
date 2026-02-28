using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Coldblood Citadel dungeon (WorldId 3522).
    ///
    /// Completion condition: all six boss kills required.
    ///   Darksister #1        — Creature2Id 75472 ("[CBC]")
    ///   Darksister #2        — Creature2Id 75473 ("[CBC]")
    ///   Darksister #3        — Creature2Id 75474 ("[CBC]")
    ///   Ice Boss             — Creature2Id 75508 ("[CBC]")
    ///   High Priest          — Creature2Id 75509 ("[CBC]")
    ///   Harizog Coldblood    — Creature2Id 75459 ("[CBC] Final Boss")
    ///
    /// Source: Creature2.tbl name search (prefix [CBC]).
    /// </summary>
    [ScriptFilterOwnerId(3522)]
    public class ColdbloodCitadelScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            75472u, 75473u, 75474u,  // Darksisters council (3 members)
            75508u,                   // Ice Boss
            75509u,                   // High Priest
            75459u,                   // Harizog Coldblood — final boss
        };

        private const int RequiredBossCount = 6;

        private IContentMapInstance owner;
        private HashSet<uint> defeatedBosses = new();

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            this.owner = owner;
            defeatedBosses.Clear();
        }

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            if (!BossCreatureIds.Contains(creatureId) || !defeatedBosses.Add(creatureId))
                return;

            if (defeatedBosses.Count < RequiredBossCount)
                return;

            if (owner.Match != null)
                owner.Match.MatchFinish();
            else
                owner.OnMatchFinish();
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            defeatedBosses.Clear();
        }
    }
}
