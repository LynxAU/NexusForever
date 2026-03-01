using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Cinematic;
using NLog;

namespace NexusForever.Game.Entity
{
    public class CinematicManager : ICinematicManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private IPlayer owner;

        private ICinematicBase currentCinematic;
        private readonly Queue<ICinematicBase> queuedCinematics = new();

        /// <summary>
        /// Initialise a <see cref="ICinematicManager"/> for this <see cref="IPlayer"/>.
        /// </summary>
        public CinematicManager(IPlayer player)
        {
            owner = player;
        }

        /// <summary>
        /// Queue a <see cref="ICinematicBase"/> to be played.
        /// </summary>
        public void QueueCinematic(ICinematicBase cinematic)
        {
            queuedCinematics.Enqueue(cinematic);
            if (currentCinematic == null && queuedCinematics.Count >= 1u)
                PlayQueuedCinematic();
        }

        /// <summary>
        /// Play the next queued <see cref="ICinematicBase"/>.
        /// </summary>
        public void PlayQueuedCinematic()
        {
            if (queuedCinematics.Count == 0)
                return;

            ICinematicBase cinematic = queuedCinematics.Dequeue();
            currentCinematic = cinematic;
            currentCinematic.StartPlayback(owner);
        }

        /// <summary>
        /// Handle the <see cref="CinematicState"/> the Client sent back. Only to be called from Client Handlers.
        /// </summary>
        public void HandleClientCinematicState(CinematicState cinematicState)
        {
            switch (cinematicState)
            {
                case CinematicState.Initalising:
                    // Cast Spell: Generic - Cinematic Player State - Tier 1 (Spell4 ID: 49887)
                    // Makes the player invisible and immune to aggro during the cinematic.
                    owner.CastSpell(49887u, new SpellParameters());
                    break;
                case CinematicState.Finishing:
                    // Remove the Cinematic Player State buff to restore visibility and aggro.
                    ISpell stateSpell = owner.GetActiveSpell(s => !s.IsFinished && s.Parameters.SpellInfo?.Entry.Id == 49887u);
                    if (stateSpell != null)
                        owner.CancelSpellCast(stateSpell.CastingId);
                    break;
                case CinematicState.Ended:
                    // Player is back in the world. Continue any scripts that may've been paused.
                    currentCinematic = null;
                    PlayQueuedCinematic();
                    break;
            }
        }
    }
}
