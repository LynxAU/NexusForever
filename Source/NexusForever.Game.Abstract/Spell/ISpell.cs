using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Spell
{
    public interface ISpell : IDisposable, IUpdate
    {
        ISpellParameters Parameters { get; }
        uint CastingId { get; }
        bool IsCasting { get; }
        bool IsFinished { get; }
        IReadOnlyCollection<ISpellTargetInfo> Targets { get; }

        IUnitEntity Caster { get; }

        /// <summary>
        /// Begin cast, checking prerequisites before initiating.
        /// </summary>
        void Cast();

        /// <summary>
        /// Cancel cast with supplied <see cref="CastResult"/>.
        /// </summary>
        void CancelCast(CastResult result);

        /// <summary>
        /// Enqueue a spell event callback to run after the supplied delay in seconds.
        /// </summary>
        void EnqueueEvent(double delay, Action callback);

        bool IsMovingInterrupted();
    }
}
