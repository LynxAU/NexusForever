using System.Collections.Immutable;
using System.Diagnostics;
using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Static.Challenges;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Challenge
{
    public sealed class GlobalChallengeManager : Singleton<GlobalChallengeManager>, IGlobalChallengeManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private ImmutableDictionary<uint, IChallengeInfo> challengeStore;

        // Combat-type challenges indexed by their Target (creature id)
        private ImmutableDictionary<uint, ImmutableList<IChallengeInfo>> combatTargetIndex;
        private ImmutableDictionary<ChallengeType, ImmutableList<IChallengeInfo>> challengeTypeIndex;

        public void Initialise()
        {
            Stopwatch sw = Stopwatch.StartNew();

            var builder = ImmutableDictionary.CreateBuilder<uint, IChallengeInfo>();
            var combatIndex = new Dictionary<uint, List<IChallengeInfo>>();
            var typeIndex = new Dictionary<ChallengeType, List<IChallengeInfo>>();

            foreach (ChallengeEntry entry in GameTableManager.Instance.Challenge.Entries)
            {
                if (entry.Id == 0)
                    continue;

                ChallengeTierEntry[] tiers = new ChallengeTierEntry[3];
                tiers[0] = entry.ChallengeTierId00 != 0 ? GameTableManager.Instance.ChallengeTier.GetEntry(entry.ChallengeTierId00) : null;
                tiers[1] = entry.ChallengeTierId01 != 0 ? GameTableManager.Instance.ChallengeTier.GetEntry(entry.ChallengeTierId01) : null;
                tiers[2] = entry.ChallengeTierId02 != 0 ? GameTableManager.Instance.ChallengeTier.GetEntry(entry.ChallengeTierId02) : null;

                var info = new ChallengeInfo(entry, tiers);
                builder.Add(entry.Id, info);
                if (!typeIndex.TryGetValue(info.Type, out List<IChallengeInfo> challengesByType))
                {
                    challengesByType = new List<IChallengeInfo>();
                    typeIndex[info.Type] = challengesByType;
                }
                challengesByType.Add(info);

                if (info.Type == ChallengeType.Combat && info.Target != 0)
                {
                    if (!combatIndex.TryGetValue(info.Target, out List<IChallengeInfo> list))
                    {
                        list = new List<IChallengeInfo>();
                        combatIndex[info.Target] = list;
                    }
                    list.Add(info);
                }
            }

            challengeStore    = builder.ToImmutable();
            combatTargetIndex = combatIndex.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableList());
            challengeTypeIndex = typeIndex.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableList());

            log.Info($"Cached {challengeStore.Count} challenge entries ({combatTargetIndex.Count} unique combat targets) in {sw.ElapsedMilliseconds}ms.");
        }

        public IChallengeInfo GetChallengeInfo(uint challengeId)
        {
            return challengeStore.TryGetValue(challengeId, out IChallengeInfo info) ? info : null;
        }

        public IEnumerable<IChallengeInfo> GetCombatChallengesForTarget(uint creatureId)
        {
            return combatTargetIndex.TryGetValue(creatureId, out ImmutableList<IChallengeInfo> list)
                ? list
                : Enumerable.Empty<IChallengeInfo>();
        }

        public IEnumerable<IChallengeInfo> GetChallengesByType(ChallengeType type)
        {
            return challengeTypeIndex.TryGetValue(type, out ImmutableList<IChallengeInfo> list)
                ? list
                : Enumerable.Empty<IChallengeInfo>();
        }
    }
}
