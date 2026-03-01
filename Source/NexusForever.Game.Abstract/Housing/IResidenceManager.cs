using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Abstract.Housing
{
    public interface IResidenceManager
    {
        IResidence Residence { get; }

        /// <summary>
        /// Create new <see cref="IDecor"/> from supplied <see cref="HousingDecorInfoEntry"/> to residence your crate.
        /// </summary>
        /// <remarks>
        /// Decor will be created for your residence regardless of the current residence you are on.
        /// </remarks>
        void DecorCreate(HousingDecorInfoEntry entry, uint quantity = 1);

        /// <summary>
        /// Set the privacy level of your residence with the supplied <see cref="ResidencePrivacyLevel"/>.
        /// </summary>
        void SetResidencePrivacy(ResidencePrivacyLevel privacy);

        /// <summary>
        /// Send <see cref="ServerHousingBasics"/> message to <see cref="IPlayer"/>.
        /// </summary>
        void SendHousingBasics();

        /// <summary>
        /// Update upkeep state when the owning player logs in.
        /// </summary>
        void OnLogin(DateTime? lastOnline);

        /// <summary>
        /// Update upkeep state from world tick while the owning player is online.
        /// </summary>
        void Update(double lastTick);
    }
}
