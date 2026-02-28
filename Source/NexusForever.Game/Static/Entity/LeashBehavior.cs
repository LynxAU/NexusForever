namespace NexusForever.Game.Static.Entity
{
    /// <summary>
    /// Defines the leash behavior for an NPC when exceeding leash range.
    /// </summary>
    public enum LeashBehavior
    {
        /// <summary>
        /// Standard behavior: NPC evades and returns to spawn position.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Infinite leash: NPC will chase target indefinitely without returning.
        /// Typically used for bosses and world bosses.
        /// </summary>
        Infinite = 1,

        /// <summary>
        /// No leash: NPC has no leash limit and will chase forever.
        /// </summary>
        None = 2
    }
}
