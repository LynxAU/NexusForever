using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Base class for adventure instance map scripts.
    ///
    /// Adventures complete when every creature ID registered via
    /// <see cref="RegisterRequiredKill"/> has died at least once. Subclasses override
    /// <see cref="OnAdventureLoad"/> to register creatures and optionally define waves.
    ///
    /// For wave-based adventures (Hycrest, Galeras) use <see cref="AddWave"/>:
    /// the adventure completes when the final wave is cleared.
    /// For kill-all adventures (StarComm, Farside etc.) use <see cref="RegisterRequiredKill"/>
    /// to add every required creature.
    ///
    /// Creature IDs for all adventures require in-game testing or retail sniff data
    /// (adventures have no bracket prefix in Creature2.tbl). Wire in each subclass once
    /// IDs are confirmed.
    /// </summary>
    public abstract class AdventureScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // A wave is a set of creature IDs that must all die before the wave is considered cleared.
        private sealed class AdventureWave
        {
            private readonly HashSet<uint> required;
            private readonly HashSet<uint> killed = new();

            public AdventureWave(IEnumerable<uint> creatureIds)
            {
                required = new HashSet<uint>(creatureIds);
            }

            public bool IsEmpty => required.Count == 0;

            /// <returns>True if this kill completed the wave.</returns>
            public bool RecordKill(uint creatureId)
            {
                if (!required.Contains(creatureId))
                    return false;
                killed.Add(creatureId);
                return killed.Count >= required.Count;
            }

            public void Reset() => killed.Clear();
        }

        private readonly List<AdventureWave> waves = new();
        private int currentWave = 0;

        protected IContentMapInstance Owner { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            Owner       = owner;
            currentWave = 0;
            waves.Clear(); // Clear before rebuilding so repeated loads don't double waves.
            OnAdventureLoad();
        }

        /// <summary>
        /// Override to register required kills and waves using
        /// <see cref="RegisterRequiredKill"/> or <see cref="AddWave"/>.
        /// Called on every <see cref="OnLoad"/> so state is reset each attempt.
        /// </summary>
        protected abstract void OnAdventureLoad();

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            if (waves.Count == 0 || currentWave >= waves.Count)
                return;

            if (!waves[currentWave].RecordKill(creatureId))
                return;

            currentWave++;
            if (currentWave >= waves.Count)
                FinishAdventure();
            else
                OnWaveComplete(currentWave - 1); // notify subclass of wave transition
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            currentWave = 0;
            foreach (AdventureWave wave in waves)
                wave.Reset();
        }

        // ── Builder API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Add a single creature kill as a standalone wave.
        /// Equivalent to <c>AddWave(creatureId)</c>.
        /// </summary>
        protected void RegisterRequiredKill(uint creatureId) => AddWave(creatureId);

        /// <summary>
        /// Add a wave of one or more creatures that must all die before the adventure can advance.
        /// Waves are cleared in order; the adventure completes when the last wave is cleared.
        /// </summary>
        protected void AddWave(params uint[] creatureIds)
        {
            waves.Add(new AdventureWave(creatureIds));
        }

        /// <summary>
        /// Called when wave <paramref name="waveIndex"/> is cleared and the next wave begins.
        /// Override to play emotes, spawn reinforcements, or apply other per-wave effects.
        /// </summary>
        protected virtual void OnWaveComplete(int waveIndex) { }

        // ── Completion ────────────────────────────────────────────────────────────

        private void FinishAdventure()
        {
            if (Owner?.Match != null)
                Owner.Match.MatchFinish();
            else
                Owner?.OnMatchFinish();
        }
    }
}
