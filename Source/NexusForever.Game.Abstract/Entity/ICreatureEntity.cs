namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// An <see cref="ICreatureEntity"/> is an extension to <see cref="IUnitEntity"/> which contains logic specific to non player controlled combat entities.
    /// </summary>
    public interface ICreatureEntity : IUnitEntity
    {
        /// <summary>
        /// Radius within which the NPC will auto-aggro hostile units.
        /// </summary>
        float AggroRadius { get; }
    }
}
