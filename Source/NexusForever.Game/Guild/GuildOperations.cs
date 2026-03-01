using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable.Text.Filter;
using NexusForever.GameTable.Text.Static;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public partial class Guild
    {
        [GuildOperationHandler(GuildOperation.AdditionalInfo)]
        private IGuildResultInfo GuildOperationAdditionalInfo(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (member.Rank.Index > 0)
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (!TextFilterManager.Instance.IsTextValid(operation.TextValue)
                    || !TextFilterManager.Instance.IsTextValid(operation.TextValue, UserText.GuildName)) 
                    return new GuildResultInfo(GuildResult.InvalidGuildInfo, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
            {
                AdditionalInfo = operation.TextValue;

                Broadcast(new ServerGuildInfoMessageUpdate
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                    InfoMessage = AdditionalInfo
                });
            }

            return result;
        }

        [GuildOperationHandler(GuildOperation.MessageOfTheDay)]
        private IGuildResultInfo GuildOperationMessageOfTheDay(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.MessageOfTheDay))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (!TextFilterManager.Instance.IsTextValid(operation.TextValue)
                    || !TextFilterManager.Instance.IsTextValid(operation.TextValue, UserText.GuildMessageOfTheDay))
                    return new GuildResultInfo(GuildResult.InvalidMessageOfTheDay, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
            {
                MessageOfTheDay = operation.TextValue;

                Broadcast(new ServerGuildMotdUpdate
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                    MessageOfTheDay = MessageOfTheDay
                });
            }

            return result;
        }

        [GuildOperationHandler(GuildOperation.TaxUpdate)]
        private IGuildResultInfo GuildOperationTaxUpdate(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (member.Rank.Index > 0)
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
                SetTaxes(Convert.ToBoolean(operation.Data));

            return result;
        }

        [GuildOperationHandler(GuildOperation.RecruitmentDescription)]
        private IGuildResultInfo GuildOperationRecruitmentDescription(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.MessageOfTheDay))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (!TextFilterManager.Instance.IsTextValid(operation.TextValue)
                    || !TextFilterManager.Instance.IsTextValid(operation.TextValue, UserText.GuildRecruitDescription))
                    return new GuildResultInfo(GuildResult.InvalidGuildRecruitDescription, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
                RecruitmentDescription = operation.TextValue;

            return result;
        }

        [GuildOperationHandler(GuildOperation.RecruitmentDemand)]
        private IGuildResultInfo GuildOperationRecruitmentDemand(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.MessageOfTheDay))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
                RecruitmentDemand = operation.Data.UInt32Data;

            return result;
        }

        [GuildOperationHandler(GuildOperation.RecruitmentMinLevel)]
        private IGuildResultInfo GuildOperationRecruitmentMinLevel(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.MessageOfTheDay))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (operation.Data.UInt32Data > 50u)
                    return new GuildResultInfo(GuildResult.NotHighEnoughLevel, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
                RecruitmentMinLevel = operation.Data.UInt32Data;

            return result;
        }

        [GuildOperationHandler(GuildOperation.GuildClassification)]
        private IGuildResultInfo GuildOperationGuildClassification(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.MessageOfTheDay))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (!Enum.IsDefined(typeof(GuildClassification), operation.Data.Int32Data))
                    return new GuildResultInfo(GuildResult.UnableToProcess, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
            {
                Classification = (GuildClassification)operation.Data.Int32Data;

                Broadcast(new ServerGuildClassification
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                    Classification = Classification
                });
            }

            return result;
        }
    }
}
