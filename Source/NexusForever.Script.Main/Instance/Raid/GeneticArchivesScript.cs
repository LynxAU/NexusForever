using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Genetic Archives raid (WorldId 1462, internal "GeneticArchives").
    ///
    /// Completion condition: all eight main boss encounters must be defeated.
    ///   e410 Experiment X-89 (Strain Mauler)       — Creature2Id 49198
    ///   e411 Phage Maw (Metal Maw)                  — Creature2Id 52974
    ///   e412 Kuralak the Defiler                    — Creature2Id 52969
    ///   e413 Phagetech Prototypes (4 Gho-bots)      — Creature2Id 54029 / 54030 / 54031 / 54032
    ///   e413 Phagetech Guardian C-148 (Probebot #1) — Creature2Id 54055
    ///   e413 Phagetech Guardian C-432 (Probebot #2) — Creature2Id 54056
    ///   Phageborn Convergence — five-member council — Creature2Id 52963 / 52964 / 52968 / 52970 / 52971
    ///   e415 Dreadphage Ohmna                       — Creature2Id 49395
    ///
    /// RequiredBossCount = 1+1+1+4+1+1+5+1 = 15 individual boss deaths.
    ///
    /// Optional miniboss encounters (not required for raid completion):
    ///   Genetic Monstrosity          — Creature2Id 54968
    ///   Gravitron Operator           — Creature2Id 56184 (boss entity; 56163 = machine object)
    ///   Hideously Malformed Mutant   — Creature2Id 56178
    ///   Fetid Miscreation            — Creature2Id 56377 (internal: Ravenok; unverified)
    ///   Guardian East                — Creature2Id 54785
    ///   Guardian West                — Creature2Id 54787
    ///   Malfunctioning Battery       — Creature2Id 56174
    ///   Malfunctioning Dynamo        — Creature2Id 54935
    ///   Malfunctioning Piston        — Creature2Id 56106
    ///   Malfunctioning Gear          — Creature2Id 55066
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Genetic Archives.sql
    /// Source: Creature2.tbl name search for "[GA] e4XX" tagged entries + Spell4.tbl ability data
    ///         + WildStarLogs encounter list (cross-referenced 2026-02).
    /// </summary>
    [ScriptFilterOwnerId(1462)]
    public class GeneticArchivesScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Required boss creature IDs — all must die for raid completion.
        // Gho-bots/TMNS are multi-member encounters; each member counts individually.
        // C-148/C-432 are separate encounters from Phagetech Prototypes (Gho-bots).
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            49198u,  // e410 — Experiment X-89 (Strain Mauler)
            52974u,  // e411 — Phage Maw (Metal Maw)
            52969u,  // e412 — Kuralak the Defiler (Genetic Architect)
            54029u, 54030u, 54031u, 54032u,  // e413 — Phagetech Prototypes: Gho-bots (Augmentor, Fabricator, Protector, Commander)
            54055u,  // e413 — Phagetech Guardian C-148 (Probebot #1)
            54056u,  // e413 — Phagetech Guardian C-432 (Probebot #2)
            52963u, 52964u, 52968u, 52970u, 52971u,  // Phageborn Convergence — five-member council
            49395u,  // e415 — Dreadphage Ohmna (final boss)
        };

        // 1 (X-89) + 1 (Metal Maw) + 1 (Kuralak) + 4 (Gho-bots) + 1 (C-148) + 1 (C-432) + 5 (Phageborn) + 1 (Ohmna) = 15
        private const int RequiredBossCount = 15;

        private IContentMapInstance owner;
        private readonly HashSet<uint> defeatedBosses = new();

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

            // All main boss encounters defeated — raid complete.
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
