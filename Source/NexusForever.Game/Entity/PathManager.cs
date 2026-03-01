using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using Path = NexusForever.Game.Static.Entity.Path;

namespace NexusForever.Game.Entity
{
    public class PathManager : IPathManager
    {
        private const uint MaxPathCount = 4u;
        private const uint MaxPathLevel = 30u;

        private readonly IPlayer player;
        private readonly Dictionary<Path, IPathEntry> paths = new();
        private readonly Dictionary<uint, uint> settlerMissionObjectiveFlags = new();
        private readonly HashSet<uint> completedSettlerMissions = new();

        /// <summary>
        /// Create a new <see cref="IPathManager"/> from <see cref="IPlayer"/> database model.
        /// </summary>
        public PathManager(IPlayer owner, CharacterModel model)
        {
            player = owner;
            foreach (CharacterPathModel pathModel in model.Path)
                paths.Add((Path)pathModel.Path, new PathEntry(pathModel));

            Validate();
        }

        private void Validate()
        {
            if (paths.Count != MaxPathCount)
            {
                // sanity checks to make sure a player always has entries for all paths
                if (paths.Count == 0)
                    SetPathEntry(player.Path, PathCreate(player.Path, true));

                for (Path path = Path.Soldier; path <= Path.Explorer; path++)
                    if (GetPathEntry(path) == null)
                        SetPathEntry(path, PathCreate(path));
            }

            // Check for missing level up rewards - this can happen if player leveled up
            // before this feature was implemented or due to a bug
            CheckForMissingRewards();
        }

        /// <summary>
        /// Check and grant any missing path level-up rewards based on current XP.
        /// </summary>
        private void CheckForMissingRewards()
        {
            foreach (IPathEntry entry in paths.Values)
            {
                if (!entry.Unlocked)
                    continue;

                uint currentLevel = GetCurrentLevel(entry.Path);
                byte rewardedLevel = entry.LevelRewarded;

                // Grant rewards for any levels that haven't been rewarded yet
                for (byte level = (byte)(rewardedLevel + 1); level <= currentLevel; level++)
                {
                    GrantLevelUpReward(entry.Path, level);
                }
            }
        }

        /// <summary>
        /// Create a new <see cref="IPathEntry"/>.
        /// </summary>
        private IPathEntry PathCreate(Path path, bool unlocked = false)
        {
            if (path > Path.Explorer)
                return null;

            if (GetPathEntry(path) != null)
                throw new ArgumentException($"{path} is already added to the player!");

            var pathEntry = new PathEntry(
                player.CharacterId,
                path,
                unlocked
            );
            SetPathEntry(path, pathEntry);
            return pathEntry;
        }

        /// <summary>
        /// Checks to see if a <see cref="IPlayer"/>'s <see cref="Path"/> is active.
        /// </summary>
        public bool IsPathActive(Path pathToCheck)
        {
            return player.Path == pathToCheck;
        }

        /// <summary>
        /// Attempts to activate a <see cref="IPlayer"/>'s <see cref="Path"/>.
        /// </summary>
        public void ActivatePath(Path pathToActivate)
        {
            if (pathToActivate > Path.Explorer)
                throw new ArgumentException("Path is not recognised.");

            if (!IsPathUnlocked(pathToActivate))
                throw new ArgumentException("Path is not unlocked.");

            if (IsPathActive(pathToActivate))
                throw new ArgumentException("Path is already active.");

            player.Path = pathToActivate;

            SendServerPathActivateResult(GenericError.Ok);
            SendSetUnitPathTypePacket();
            SendPathLogPacket();
        }

        /// <summary>
        /// Checks to see if a <see cref="IPlayer"/>'s <see cref="Path"/> is mathced by a corresponding <see cref="PathUnlockedMask"/> flag.
        /// </summary>
        public bool IsPathUnlocked(Path pathToUnlock)
        {
            return GetPathEntry(pathToUnlock).Unlocked;
        }

        /// <summary>
        /// Attemps to adjust the <see cref="IPlayer"/>'s <see cref="PathUnlockedMask"/> status.
        /// </summary>
        public void UnlockPath(Path pathToUnlock)
        {
            if (pathToUnlock > Path.Explorer)
                throw new ArgumentException("Path is not recognised.");

            if (IsPathUnlocked(pathToUnlock))
                throw new ArgumentException("Path is already unlocked.");

            GetPathEntry(pathToUnlock).Unlocked = true;

            SendServerPathUnlockResult();
            SendPathLogPacket();
        }

        /// <summary>
        /// Add XP to the current <see cref="Path"/>.
        /// </summary>
        public void AddXp(uint xp)
        {
            if (xp == 0)
                throw new ArgumentException("XP must be greater than 0.");

            Path path = player.Path;

            if (GetCurrentLevel(path) < MaxPathLevel)
            {
                IPathEntry entry = GetPathEntry(path);

                checked
                {
                    entry.TotalXp += xp;
                }

                foreach (uint level in CheckForLevelUp(entry.TotalXp, xp))
                    GrantLevelUpReward(path, level);

                SendServerPathUpdateXp(entry.TotalXp);
            }
            else
            {
                // Player is at max level (30) - still grant XP for Elder progression
                IPathEntry entry = GetPathEntry(path);
                checked
                {
                    entry.TotalXp += xp;
                }
                SendServerPathUpdateXp(entry.TotalXp);
            }
        }

        public void HandleSettlerBuildEvent(uint pathSettlerImprovementGroupId, uint buildTier)
        {
            if (pathSettlerImprovementGroupId == 0u || player.Path != Path.Settler)
                return;

            PathSettlerImprovementGroupEntry improvementGroup = GameTableManager.Instance.PathSettlerImprovementGroup.GetEntry(pathSettlerImprovementGroupId);
            if (improvementGroup?.Creature2IdDepot > 0u)
            {
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, improvementGroup.Creature2IdDepot, 1u);
                foreach (uint targetGroupId in AssetManager.Instance.GetTargetGroupsForCreatureId(improvementGroup.Creature2IdDepot) ?? Enumerable.Empty<uint>())
                    player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroup, targetGroupId, 1u);
            }

            IEnumerable<PathMissionEntry> settlerMissions = GameTableManager.Instance.PathMission.Entries
                .Where(e => e.PathTypeEnum == (uint)Path.Settler && e.ObjectId == pathSettlerImprovementGroupId);

            foreach (PathMissionEntry mission in settlerMissions)
            {
                if (mission.Id > ushort.MaxValue)
                    continue;

                uint bit = buildTier >= 31u ? 0x80000000u : 1u << (int)buildTier;
                uint objectiveFlags = settlerMissionObjectiveFlags.TryGetValue(mission.Id, out uint existingFlags)
                    ? existingFlags | bit
                    : bit;

                settlerMissionObjectiveFlags[mission.Id] = objectiveFlags;
                bool completed = completedSettlerMissions.Contains(mission.Id);

                if (!completed)
                    player.Session.EnqueueMessageEncrypted(new ServerPathMissionAdvanced { PathMissionId = (ushort)mission.Id });

                if (!completed && objectiveFlags != 0u)
                    completedSettlerMissions.Add(mission.Id);

                player.Session.EnqueueMessageEncrypted(new ServerPathMissionUpdate
                {
                    PathMissionId             = (ushort)mission.Id,
                    Completed                 = completedSettlerMissions.Contains(mission.Id),
                    ObjectiveCompletionFlags  = objectiveFlags,
                    StateFlags                = 0u
                });
            }
        }

        /// <summary>
        /// Get the current <see cref="Path"/> level for the <see cref="IPlayer"/>.
        /// </summary>
        private uint GetCurrentLevel(Path path)
        {
            return GameTableManager.Instance.PathLevel.Entries
                .LastOrDefault(x => x.PathXP <= paths[path].TotalXp && x.PathTypeEnum == (uint)path)?.PathLevel ?? 1u;
        }

        /// <summary>
        /// Get the level based on an amount of XP.
        /// </summary>
        /// <param name="xp">The XP value to get the level by</param>
        private uint GetLevelByExperience(uint xp)
        {
            return GameTableManager.Instance.PathLevel.Entries
                .LastOrDefault(x => x.PathXP <= xp && x.PathTypeEnum == (uint)player.Path)?.PathLevel ?? 1u;
        }

        /// <summary>
        /// Check to see if a level up should happen based on current XP and XP just earned.
        /// </summary>
        /// <param name="totalXp">Path XP after XP earned has been applied</param>
        /// <param name="xpGained">XP just earned</param>
        private IEnumerable<uint> CheckForLevelUp(uint totalXp, uint xpGained)
        {
            uint currentLevel = GetLevelByExperience(totalXp >= xpGained ? totalXp - xpGained : 0u);
            return GameTableManager.Instance.PathLevel.Entries
                .Where(x => x.PathLevel > currentLevel && x.PathXP <= totalXp && x.PathTypeEnum == (uint)player.Path)
                .Select(e => e.PathLevel);
        }

        /// <summary>
        /// Grants a player a level up reward for a <see cref="Path"/> and level
        /// </summary>
        /// <param name="path">The path to grant the reward for</param>
        /// <param name="level">The level to grant the reward for</param>
        private void GrantLevelUpReward(Path path, uint level)
        {
            // TODO: look at this in more in depth, might be a better way to handle
            uint baseRewardObjectId = (uint)path * MaxPathLevel + 7u; // 7 is the base offset
            uint pathRewardObjectId = baseRewardObjectId + Math.Clamp(level - 2, 0, 29); // level - 2 is used because the objectIDs start at level 2 and a -2 offset was needed

            IEnumerable<PathRewardEntry> pathRewardEntries = GameTableManager.Instance.PathReward.Entries
                .Where(x => x.ObjectId == pathRewardObjectId);
            foreach (PathRewardEntry pathRewardEntry in pathRewardEntries)
            {
                // Skip rewards with prerequisites that aren't met
                if (pathRewardEntry.PrerequisiteId > 0 && !PrerequisiteManager.Instance.Meets(player, pathRewardEntry.PrerequisiteId))
                    continue;

                GrantPathReward(pathRewardEntry);
            }

            GetPathEntry(path).LevelRewarded = (byte)level;
            player.CastSpell(53234, new Spell.SpellParameters());
        }

        /// <summary>
        /// Grant the <see cref="IPlayer"/> rewards from the <see cref="PathRewardEntry"/>
        /// </summary>
        /// <param name="pathRewardEntry">The entry containing items, spells, or titles, to be rewarded"/></param>
        private void GrantPathReward(PathRewardEntry pathRewardEntry)
        {
            if (pathRewardEntry == null)
                throw new ArgumentNullException();

            // Grant item rewards
            if (pathRewardEntry.Item2Id > 0)
                player.Inventory.ItemCreate(InventoryLocation.Inventory, pathRewardEntry.Item2Id, pathRewardEntry.Count, ItemUpdateReason.PathReward);

            // Grant spell rewards
            if (pathRewardEntry.Spell4Id > 0)
            {
                Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(pathRewardEntry.Spell4Id);
                if (spell4Entry != null)
                    player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);
            }

            // Grant title rewards
            if (pathRewardEntry.CharacterTitleId > 0)
                player.TitleManager.AddTitle((ushort)pathRewardEntry.CharacterTitleId);

            // Grant quest rewards
            if (pathRewardEntry.Quest2Id > 0)
                player.QuestManager.QuestAdd((ushort)pathRewardEntry.Quest2Id, null);
        }

        private PathUnlockedMask GetPathUnlockedMask()
        {
            PathUnlockedMask mask = PathUnlockedMask.None;
            foreach (IPathEntry entry in paths.Values)
                if (entry.Unlocked)
                    mask |= (PathUnlockedMask)(1 << (int)entry.Path);

            return mask;
        }

        /// <summary>
        /// Execute a DB Save of the <see cref="CharacterContext"/>
        /// </summary>
        public void Save(CharacterContext context)
        {
            foreach (IPathEntry pathEntry in paths.Values)
                pathEntry.Save(context);
        }

        public void SendInitialPackets()
        {
            SendPathLogPacket();
        }

        /// <summary>
        /// Used to update the Player's Path Log.
        /// </summary>
        private void SendPathLogPacket()
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathInitialise
            {
                ActivePath                  = player.Path,
                PathProgress                = paths.Values.Select(p => p.TotalXp).ToArray(),
                PathUnlockedMask            = GetPathUnlockedMask(),
                TimeSinceLastActivateInDays = GetCooldownTime() // TODO: Need to figure out timestamp calculations necessary for this value to update the client appropriately
            });
        }

        private float GetCooldownTime()
        {
            return (float)DateTime.UtcNow.Subtract(player.PathActivatedTime).TotalDays * -1;
        }

        /// <summary>
        /// Used to tell the world (and the player) which Path Type this Player is.
        /// </summary>
        public void SendSetUnitPathTypePacket()
        {
            player.EnqueueToVisible(new ServerSetUnitPathType
            {
                UnitId = player.Guid,
                Path = player.Path,
            }, true);
        }

        /// <summary>
        /// Sends a response to the player's <see cref="Path"/> activate request
        /// </summary>
        /// <param name="result">Used for success or error values</param>
        public void SendServerPathActivateResult(GenericError result = GenericError.Ok)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathChangeResult
            {
                Result = result
            });
        }

        /// <summary>
        /// Sends a response to the player's request for unlocking a <see cref="Path"/>
        /// </summary>
        /// <param name="result">Used for success or error values</param>
        public void SendServerPathUnlockResult(GenericError result = GenericError.Ok)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathUnlockResult
            {
                Result           = result,
                UnlockedPathMask = GetPathUnlockedMask()
            });
        }

        /// <summary>
        /// Sends total XP for the activate path to the player
        /// </summary>
        /// <param name="totalXp">Total Path XP to be sent</param>
        private void SendServerPathUpdateXp(uint totalXp)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPathUpdateXP
            {
                TotalXP = totalXp
            });
        }

        private IPathEntry GetPathEntry(Path path)
        {
            paths.TryGetValue(path, out IPathEntry pathEntry);
            return pathEntry;
        }

        private void SetPathEntry(Path path, IPathEntry entry)
        {
            paths[path] = entry;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IPathEntry> GetEnumerator()
        {
            return paths.Values.GetEnumerator();
        }
    }
}
