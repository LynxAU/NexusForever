using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Sanctuary of the Swordmaiden dungeon (WorldId 1271, internal "TorineDungeon").
    ///
    /// Completion condition: all eight boss/miniboss encounters must be defeated.
    ///   Deadringer Shallaos          — TG 3208 — 28600 (N) / 28599 (V)
    ///   Ondu Lifeweaver              — TG 3207 — 28721 (N) / 28720 (V)
    ///   Moldwood Overlord Skash      — TG 3209 — 28727 (N) / 28728 (V)
    ///   Rayna Darkspeaker            — TG 3210 — 28733 (N) / 28732 (V)
    ///   Spiritmother Selene (final)  — TG 3211 — 28735 (N) / 28736 (V)
    ///   Corrupted Edgesmith Torian   — TG 3237 — 28985 (N) / 28986 (V) — miniboss
    ///   Corrupted Lifecaller Khalee  — TG 3238 — 28992 (N) / 28993 (V) — miniboss
    ///   Corrupted Deathbringer       — TG 3239 — 28995 (N, Koroll) / 28996 (V, Dareia) — miniboss
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Sanctuary of the Swordmaiden.sql
    /// Source: TargetGroup entries for PublicEvent 166 (WorldId 1271).
    /// </summary>
    [ScriptFilterOwnerId(1271)]
    public class SanctuaryOfTheSwordmaidenScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            28600u, 28599u,  // Deadringer Shallaos (Normal, Veteran)
            28721u, 28720u,  // Ondu Lifeweaver (Normal, Veteran)
            28727u, 28728u,  // Moldwood Overlord Skash (Normal, Veteran)
            28733u, 28732u,  // Rayna Darkspeaker (Normal, Veteran)
            28735u, 28736u,  // Spiritmother Selene the Corrupted (Normal, Veteran)
            28985u, 28986u,  // Corrupted Edgesmith Torian (Normal, Veteran)
            28992u, 28993u,  // Corrupted Lifecaller Khalee (Normal, Veteran)
            28995u, 28996u,  // Corrupted Deathbringer Koroll/Dareia (Normal, Veteran)
        };

        // 8 distinct encounter slots (5 main bosses + 3 minibosses).
        private const int RequiredBossCount = 8;

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
