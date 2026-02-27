using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;

namespace NexusForever.Script.Main.Instance
{
    /// <summary>
    /// Base script for boss encounter NPCs inside a <see cref="IContentMapInstance"/>.
    /// On death, notifies the containing map script via <see cref="IContentMapInstance.TriggerBossDeath"/>.
    /// Subclasses should add <see cref="NexusForever.Script.Template.Filter.ScriptFilterCreatureIdAttribute"/>
    /// to bind to the correct creature id.
    /// </summary>
    public class EncounterBossScript : INonPlayerScript, IOwnedScript<INonPlayerEntity>
    {
        private INonPlayerEntity owner;

        /// <inheritdoc/>
        public void OnLoad(INonPlayerEntity owner)
        {
            this.owner = owner;
        }

        /// <inheritdoc/>
        public void OnDeath()
        {
            (owner.Map as IContentMapInstance)?.TriggerBossDeath(owner.CreatureId);
        }
    }
}
