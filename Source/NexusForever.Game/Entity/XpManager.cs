using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public class XpManager : IXpManager
    {
        public uint TotalXp
        {
            get => totalXp;
            private set
            {
                totalXp = value;
                isDirty = true;
            }
        }
        private uint totalXp;

        public uint RestBonusXp
        {
            get => restBonusXp;
            private set
            {
                restBonusXp = value;
                isDirty = true;
            }
        }
        private uint restBonusXp;

        private bool isDirty;
        private readonly IPlayer player;

        /// <summary>
        /// Create a new <see cref="IXpManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public XpManager(IPlayer player, CharacterModel model)
        {
            this.player = player;
            totalXp = model.TotalXp;

            CalculateRestXpAtLogin(model);
        }

        public void Save(CharacterContext context)
        {
            if (!isDirty)
                return;

            // character is attached in Player::Save, this will only be local lookup
            CharacterModel character = context.Character.Find(player.CharacterId);
            character.TotalXp = TotalXp;
            character.RestBonusXp = RestBonusXp;

            EntityEntry<CharacterModel> entity = context.Entry(character);
            entity.Property(p => p.TotalXp).IsModified = true;
            entity.Property(p => p.RestBonusXp).IsModified = true;

            isDirty = false;
        }

        private void CalculateRestXpAtLogin(CharacterModel model)
        {
            // don't calculate rest xp for first login
            if (model.LastOnline == null)
                return;

            // Level 50 characters have no XP-based rest bonus (Elder Gem rest logic not yet implemented).
            if (player.Level >= 50)
                return;

            float xpForLevel     = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level)?.MinXpForLevel ?? 0f;
            float xpForNextLevel = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level + 1)?.MinXpForLevel ?? 0f;
            uint maximumBonusXp  = (uint)((xpForNextLevel - xpForLevel) * 1.5f);

            // Base rate: ~0.24% of level XP per hour, accumulates regardless of logout location.
            // WildStar rest XP accrued offline everywhere; housing gave the same base rate plus
            // potential decor bonuses (TODO: apply decor/spell bonuses when those systems support it).
            const double RestXpBaseRate = 0.0024;
            double hoursSinceLogin = DateTime.UtcNow.Subtract((DateTime)model.LastOnline).TotalHours;
            double xpPercentEarned = hoursSinceLogin * RestXpBaseRate;

            // TODO: Apply bonuses from spells as necessary

            uint bonusXpValue = Math.Clamp((uint)((xpForNextLevel - xpForLevel) * xpPercentEarned), 0, maximumBonusXp);
            uint totalBonusXp = Math.Clamp(model.RestBonusXp + bonusXpValue, 0u, maximumBonusXp);
            RestBonusXp = totalBonusXp;
        }

        /// <summary>
        /// Grants <see cref="IPlayer"/> the supplied experience, handling level up if necessary.
        /// </summary>
        /// <param name="earnedXp">Experience to grant</param>
        /// <param name="reason"><see cref="ExpReason"/> for the experience grant</param>
        public void GrantXp(uint earnedXp, ExpReason reason = ExpReason.Cheat)
        {
            // TODO: move to configuration option
            const uint maxLevel = 50;

            if (earnedXp < 1)
                return;

            //if (!IsAlive)
            //    return;

            if (player.Level >= maxLevel)
                return;

            // TODO: Apply XP bonuses from current spells or active events

            // Signature XP rate was 25% extra. 
            uint signatureXp = 0u;
            if (player.SignatureEnabled)
                signatureXp = (uint)(earnedXp * 0.25f); // TODO: Make rate configurable.

            // Consume rest XP from the pool: up to 50% of base XP per kill, capped by available pool.
            uint restXp = 0u;
            if (reason == ExpReason.KillCreature && RestBonusXp > 0)
            {
                restXp = Math.Min((uint)(earnedXp * 0.5f), RestBonusXp);
                RestBonusXp -= restXp;
            }

            player.Session.EnqueueMessageEncrypted(new ServerExperienceGained
            {
                TotalXpGained     = earnedXp + signatureXp + restXp,
                RestXpAmount      = restXp,
                SignatureXpAmount = signatureXp,
                Reason            = reason
            });
            
            uint totalXp = TotalXp + earnedXp + signatureXp + restXp;

            uint xpToNextLevel = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level + 1)?.MinXpForLevel ?? uint.MaxValue;
            while (totalXp >= xpToNextLevel && player.Level < maxLevel) // WorldServer.Rules.MaxLevel)
            {
                GrantLevel((byte)(player.Level + 1));

                if (player.Level >= maxLevel)
                    break;

                xpToNextLevel = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level + 1)?.MinXpForLevel ?? uint.MaxValue;
            }

            TotalXp += earnedXp + signatureXp + restXp;
        }

        /// <summary>
        /// Sets <see cref="IPlayer"/> to the supplied level and adjusts XP accordingly. Mainly for use with GM commands.
        /// </summary>
        /// <param name="newLevel">New level to be set</param>
        /// <param name="reason"><see cref="ExpReason"/> for the level grant</param>
        public void SetLevel(byte newLevel, ExpReason reason = ExpReason.Cheat)
        {
            if (newLevel == player.Level)
                return;

            var xpEntry = GameTableManager.Instance.XpPerLevel.GetEntry(newLevel);
            if (xpEntry == null)
                return;

            uint newXp = xpEntry.MinXpForLevel;
            player.Session.EnqueueMessageEncrypted(new ServerExperienceGained
            {
                TotalXpGained     = newXp > TotalXp ? newXp - TotalXp : 0u,
                RestXpAmount      = 0,
                SignatureXpAmount = 0,
                Reason            = reason
            });

            TotalXp = newXp;
            GrantLevel(newLevel);
        }

        /// <summary>
        /// Grants <see cref="IPlayer"/> the supplied level and adjusts XP accordingly
        /// </summary>
        /// <param name="newLevel">New level to be set</param>
        private void GrantLevel(byte newLevel)
        {
            uint oldLevel = player.Level;
            if (newLevel == oldLevel)
                return;

            player.Level = newLevel;

            // Cast level up spell to trigger the big level up UI/effects
            player.CastSpell(53378, (byte)(newLevel - 1), new SpellParameters());

            // Grant Rewards for level up
            player.SpellManager.GrantSpells();
            // Unlock LAS slots
            // Unlock AMPs
            // Add feature access
        }

        public void ModifyRestBonusXp(int delta)
        {
            if (delta == 0 || player.Level >= 50)
                return;

            float xpForLevel     = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level)?.MinXpForLevel ?? 0f;
            float xpForNextLevel = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level + 1)?.MinXpForLevel ?? 0f;
            uint maximumBonusXp  = (uint)Math.Max(0f, (xpForNextLevel - xpForLevel) * 1.5f);

            long updated = (long)RestBonusXp + delta;
            RestBonusXp = (uint)Math.Clamp(updated, 0L, maximumBonusXp);
        }
    }
}
