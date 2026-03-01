using System;
using System.Numerics;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public class ResidenceManager : IResidenceManager
    {
        public IResidence Residence { get; private set; }

        private readonly IPlayer owner;

        /// <summary>
        /// Create a new <see cref="IResidenceManager"/>.
        /// </summary>
        public ResidenceManager(IPlayer player)
        {
            owner     = player;
            Residence = GlobalResidenceManager.Instance.GetResidenceByOwner(owner.CharacterId);
        }

        /// <summary>
        /// Create new <see cref="IDecor"/> from supplied <see cref="HousingDecorInfoEntry"/> to residence your crate.
        /// </summary>
        /// <remarks>
        /// Decor will be created for your residence regardless of the current residence you are on.
        /// </remarks>
        public void DecorCreate(HousingDecorInfoEntry entry, uint quantity = 1u)
        {
            Residence ??= GlobalResidenceManager.Instance.CreateResidence(owner);

            if (Residence.Map != null)
                Residence.Map.DecorCreate(Residence, entry, quantity);
            else
            {
                for (uint i = 0u; i < quantity; i++)
                    Residence.DecorCreate(entry);
            }
        }

        /// <summary>
        /// Set the privacy level of your residence with the supplied <see cref="ResidencePrivacyLevel"/>.
        /// </summary>
        public void SetResidencePrivacy(ResidencePrivacyLevel privacy)
        {
            if (Residence == null)
                throw new InvalidOperationException();

            Residence.PrivacyLevel = privacy;
            SendHousingBasics();
        }

        /// <summary>
        /// Send <see cref="ServerHousingBasics"/> message to <see cref="IPlayer"/>.
        /// </summary>
        public void SendHousingBasics()
        {
            // not really sure why the client converts the privacy level to and from flags
            // see Residence.GetResidencePrivacyLevel LUA function for more context
            var flags = (Residence?.PrivacyLevel ?? ResidencePrivacyLevel.Public) switch
            {
                ResidencePrivacyLevel.Public        => ServerHousingBasics.ResidencePrivacyLevelFlags.Public,
                ResidencePrivacyLevel.Private       => ServerHousingBasics.ResidencePrivacyLevelFlags.Private,
                ResidencePrivacyLevel.NeighborsOnly => ServerHousingBasics.ResidencePrivacyLevelFlags.NeighborsOnly,
                ResidencePrivacyLevel.RoommatesOnly => ServerHousingBasics.ResidencePrivacyLevelFlags.RoommatesOnly,
                _                                   => ServerHousingBasics.ResidencePrivacyLevelFlags.Public
            };

            owner.Session.EnqueueMessageEncrypted(new ServerHousingBasics
            {
                ResidenceId     = Residence?.Id ?? 0ul,
                /*NeighbourhoodId = GuildManager.GetGuild<Community>(GuildType.Community)?.Id ?? 0ul,*/
                PrivacyLevel    = flags
            });
        }

        public void OnLogin(DateTime? lastOnline)
        {
            if (Residence == null)
                return;

            if (lastOnline.HasValue)
            {
                double elapsedSeconds = Math.Max(0d, DateTime.UtcNow.Subtract(lastOnline.Value).TotalSeconds);
                if (elapsedSeconds > 0d)
                    Residence.UpdateUpkeep(elapsedSeconds);
            }

            SendHousingBasics();
        }

        public void Update(double lastTick)
        {
            Residence?.UpdateUpkeep(lastTick);
        }

        public IHousingMannequinEntity GetMannequin(byte mannequinIndex)
        {
            if (mannequinIndex == 0 || owner.Map == null)
                return null;

            // Prefer a direct world-socket mapping when available.
            IEnumerable<IHousingMannequinEntity> mannequins = owner.Map
                .Search(Vector3.Zero, null, new SearchCheckRange<IHousingMannequinEntity>(Vector3.Zero, null))
                .Where(m => m != null);

            IHousingMannequinEntity bySocket = mannequins.FirstOrDefault(m => m.WorldSocketId == mannequinIndex);
            if (bySocket != null)
                return bySocket;

            // Fallback: deterministic ordering to map 1-based mannequin index.
            return mannequins
                .OrderBy(m => m.Guid)
                .Skip(mannequinIndex - 1)
                .FirstOrDefault();
        }
    }
}
