namespace NexusForever.Script.Template
{
    public interface INonPlayerScript : IUnitScript
    {
        /// <summary>
        /// Invoked each AI tick while the NPC is in combat. Use this to drive spell rotations and custom combat behaviour.
        /// </summary>
        void OnCombatUpdate(double lastTick)
        {
        }

        /// <summary>
        /// Invoked each AI tick while the NPC is out of combat and not returning from an evade.
        /// Use this to drive custom idle behaviour such as emotes, look-at targets, or scripted movement.
        /// </summary>
        void OnIdleUpdate(double lastTick)
        {
        }

        /// <summary>
        /// Invoked when the NPC begins evading (leash broken, returning to spawn).
        /// </summary>
        void OnEvade()
        {
        }
    }
}
