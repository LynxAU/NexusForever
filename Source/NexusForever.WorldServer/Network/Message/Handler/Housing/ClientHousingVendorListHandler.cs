using System.Linq;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingVendorListHandler : IMessageHandler<IWorldSession, ClientHousingVendorList>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientHousingVendorListHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingVendorList _)
        {
            var serverHousingVendorList = new ServerHousingVendorList
            {
                ListType = 0
            };

            foreach (HousingPlugItemEntry entry in gameTableManager.HousingPlugItem.Entries
                .Where(e => e != null && e.Id != 0u))
            {
                uint cost = 0u;
                if (entry.HousingContributionInfoId00 != 0u)
                {
                    HousingContributionInfoEntry contribution = gameTableManager.HousingContributionInfo.GetEntry(entry.HousingContributionInfoId00);
                    if (contribution != null)
                        cost = contribution.ContributionPointRequirement;
                }

                serverHousingVendorList.PlugItems.Add(new ServerHousingVendorList.PlugItem
                {
                    SourceId = 0ul,
                    PlugItemId = entry.Id,
                    Cost = cost,
                    PlugItemFlags = entry.Flags
                });
            }

            session.EnqueueMessageEncrypted(serverHousingVendorList);
        }
    }
}
