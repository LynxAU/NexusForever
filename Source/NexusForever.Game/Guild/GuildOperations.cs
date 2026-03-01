using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
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

        [GuildOperationHandler(GuildOperation.BankTabRename)]
        private IGuildResultInfo GuildOperationBankTabRename(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.BankTabRename))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (operation.Rank > 9u)
                    return new GuildResultInfo(GuildResult.NotABankTab, Identity);

                if (!TextFilterManager.Instance.IsTextValid(operation.TextValue)
                    || !TextFilterManager.Instance.IsTextValid(operation.TextValue, UserText.GuildBankTabName))
                    return new GuildResultInfo(GuildResult.InvalidBankTabName, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success)
            {
                RenameBankTab((byte)operation.Rank, operation.TextValue);

                Broadcast(new ServerGuildBankTabRename
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                    BankTabNames = GetBankTabNames()
                });
            }

            return result;
        }

        [GuildOperationHandler(GuildOperation.PurchasePerk)]
        private IGuildResultInfo GuildOperationPurchasePerk(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            GuildPerk perk = (GuildPerk)operation.Data.UInt32Data;
            GuildPerkEntry perkEntry = null;

            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.SpendInfluence))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (!Enum.IsDefined(typeof(GuildPerk), (int)operation.Data.UInt32Data))
                    return new GuildResultInfo(GuildResult.PerkDoesNotExist, Identity);

                perkEntry = GameTableManager.Instance.GuildPerk.GetEntry((uint)perk);
                if (perkEntry == null)
                    return new GuildResultInfo(GuildResult.PerkDoesNotExist, Identity);

                if (IsPerkUnlocked(perk))
                    return new GuildResultInfo(GuildResult.PerkIsAlreadyUnlocked, Identity);

                uint[] requiredPerks =
                {
                    perkEntry.GuildPerkIdRequired00,
                    perkEntry.GuildPerkIdRequired01,
                    perkEntry.GuildPerkIdRequired02
                };

                foreach (uint requiredPerk in requiredPerks.Where(p => p != 0u))
                    if (!IsPerkUnlocked((GuildPerk)requiredPerk))
                        return new GuildResultInfo(GuildResult.RequiresPerkPurchase, Identity);

                if (perkEntry.AchievementIdRequired != 0u
                    && !AchievementManager.HasCompletedAchievement((ushort)perkEntry.AchievementIdRequired))
                    return new GuildResultInfo(GuildResult.RequiresAchievement, Identity);

                // Influence economy is not implemented yet; unlock flow is still allowed.
                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success && UnlockPerk(perk))
            {
                Broadcast(new ServerGuildPerkUnlocked
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                    GuildPerkId = (ushort)perk
                });
            }

            return result;
        }

        [GuildOperationHandler(GuildOperation.ActivatePerk)]
        private IGuildResultInfo GuildOperationActivatePerk(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            GuildPerk perk = (GuildPerk)operation.Data.UInt32Data;

            IGuildResultInfo GetResult()
            {
                if (!member.Rank.HasPermission(GuildRankPermission.SpendInfluence))
                    return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

                if (!Enum.IsDefined(typeof(GuildPerk), (int)operation.Data.UInt32Data))
                    return new GuildResultInfo(GuildResult.PerkDoesNotExist, Identity);

                if (!IsPerkUnlocked(perk))
                    return new GuildResultInfo(GuildResult.RequiresPerkPurchase, Identity);

                if (IsPerkActive(perk))
                    return new GuildResultInfo(GuildResult.PerkIsAlreadyActive, Identity);

                return new GuildResultInfo(GuildResult.Success, Identity);
            }

            IGuildResultInfo result = GetResult();
            if (result.Result == GuildResult.Success && ActivatePerk(perk))
            {
                Broadcast(new ServerGuildPerkActivated
                {
                    GuildIdentity = Identity.ToNetworkIdentity(),
                    GuildPerkId = (ushort)perk,
                    DaysAgoActivated = 0f
                });
            }

            return result;
        }

        [GuildOperationHandler(GuildOperation.NominateOrVote)]
        private IGuildResultInfo GuildOperationNominateOrVote(IGuildMember member, IPlayer player, ClientGuildOperation operation)
        {
            if (!member.Rank.HasPermission(GuildRankPermission.Vote))
                return new GuildResultInfo(GuildResult.RankLacksSufficientPermissions, Identity);

            // Vote state machine is not implemented; respond explicitly instead of leaving unhandled.
            return new GuildResultInfo(GuildResult.NoVoteInProgress, Identity);
        }
    }
}
