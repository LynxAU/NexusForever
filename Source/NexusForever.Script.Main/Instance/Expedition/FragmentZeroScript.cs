using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Fragment Zero expedition (WorldId 3180, ShiphandLevel6).
    ///
    /// The expedition has a normal (level 6) and veteran (level 50) version on the
    /// same WorldId. Completion requires 5 of the tracked bosses to die.
    ///
    /// Normal-mode kill objectives (level 6, Shiphand_level6):
    ///   Life-Overseer Boss     - Creature2Id 67522
    ///   Project "Matron" Boss  - Creature2Id 69088
    ///   Prototype Alpha        - Creature2Id 67526  (Xenobyte Boss 1)
    ///   Prototype Beta         - Creature2Id 67527  (Xenobyte Boss 2)
    ///   Prototype Delta        - Creature2Id 67528  (Xenobyte Boss 3)
    ///
    /// Veteran-mode kill objectives (level 50 elite):
    ///   Obj 4417 KillTargetGroup - Creature2Id 69664 (Project Matron veteran)
    ///   Obj 4423 KillTargetGroup - Creature2Id 69672 (Prototype Alpha veteran)
    ///   Obj 4444 KillTargetGroup - Creature2Id 69635 (Life-Overseer veteran)
    ///   Obj 4449 KillTargetGroup - Creature2Id 69673 (Prototype Beta veteran)
    ///   Obj 4450 KillTargetGroup - Creature2Id 69674 (Prototype Delta veteran - final boss)
    ///
    /// Gronyx Boss (67514) is a normal-mode side boss, not in veteran kill objectives.
    /// </summary>
    [ScriptFilterOwnerId(3180)]
    public class FragmentZeroScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            // Normal-mode bosses (level 6)
            67522u,  // Life-Overseer Boss
            69088u,  // Project "Matron" Boss
            67526u,  // Prototype Alpha (Xenobyte Boss 1)
            67527u,  // Prototype Beta  (Xenobyte Boss 2)
            67528u,  // Prototype Delta (Xenobyte Boss 3)

            // Veteran-mode bosses (level 50 elite)
            69664u,  // Project "Matron" Boss veteran (Obj 4417)
            69672u,  // Prototype Alpha Veteran        (Obj 4423)
            69635u,  // Life-Overseer Boss veteran     (Obj 4444)
            69673u,  // Prototype Beta Veteran         (Obj 4449)
            69674u,  // Prototype Delta Veteran        (Obj 4450 - final boss)
        };

        private const int RequiredBossCount = 5;

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

            // Five bosses defeated - instance complete.
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
