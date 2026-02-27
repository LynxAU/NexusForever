using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Spell
{
    public static class SpellHandler
    {
        [SpellEffectHandler(SpellEffectType.VitalModifier)]
        public static void HandleEffectVitalModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            Vital vital = (Vital)info.Entry.DataBits00;

            float requestedAmount = info.Entry.DataBits01;
            if (requestedAmount == 0f)
            {
                for (int i = 0; i < info.Entry.ParameterValue.Length; i++)
                {
                    if (info.Entry.ParameterValue[i] == 0f)
                        continue;

                    requestedAmount = info.Entry.ParameterValue[i];
                    break;
                }
            }

            if (requestedAmount == 0f)
                return;

            float before = target.GetVitalValue(vital);
            target.ModifyVital(vital, requestedAmount);
            float after = target.GetVitalValue(vital);

            float appliedAmount = after - before;
            if (appliedAmount == 0f)
                return;

            info.AddCombatLog(new CombatLogVitalModifier
            {
                Amount         = appliedAmount,
                VitalModified  = vital,
                BShowCombatLog = true,
                CastData       = BuildCastData(spell, target, info)
            });
        }

        [SpellEffectHandler(SpellEffectType.SapVital)]
        public static void HandleEffectSapVital(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Assumption: SapVital uses the same primary data layout as VitalModifier
            // (DataBits00 = Vital, DataBits01 = amount), but always drains the target.
            Vital vital = (Vital)info.Entry.DataBits00;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
                return;

            float before = target.GetVitalValue(vital);
            target.ModifyVital(vital, -(float)amount);
            float after = target.GetVitalValue(vital);

            float appliedAmount = after - before;
            if (appliedAmount == 0f)
                return;

            info.AddCombatLog(new CombatLogVitalModifier
            {
                Amount         = appliedAmount,
                VitalModified  = vital,
                BShowCombatLog = true,
                CastData       = BuildCastData(spell, target, info)
            });
        }

        [SpellEffectHandler(SpellEffectType.ClampVital)]
        public static void HandleEffectClampVital(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Assumption: DataBits00 = Vital, DataBits01 = Min, DataBits02 = Max.
            // If both bounds are zero, fall back to the first parameter values.
            Vital vital = (Vital)info.Entry.DataBits00;

            float minValue = info.Entry.DataBits01;
            float maxValue = info.Entry.DataBits02;

            if (minValue == 0f && maxValue == 0f)
            {
                if (info.Entry.ParameterValue.Length > 0)
                    minValue = info.Entry.ParameterValue[0];
                if (info.Entry.ParameterValue.Length > 1)
                    maxValue = info.Entry.ParameterValue[1];
            }

            bool hasMin = minValue != 0f;
            bool hasMax = maxValue != 0f;
            if (!hasMin && !hasMax)
                return;

            if (hasMin && hasMax && maxValue < minValue)
                (minValue, maxValue) = (maxValue, minValue);

            float current = target.GetVitalValue(vital);
            float clamped = current;
            if (hasMin)
                clamped = Math.Max(clamped, minValue);
            if (hasMax)
                clamped = Math.Min(clamped, maxValue);

            float delta = clamped - current;
            if (delta == 0f)
                return;

            target.ModifyVital(vital, delta);

            info.AddCombatLog(new CombatLogVitalModifier
            {
                Amount         = delta,
                VitalModified  = vital,
                BShowCombatLog = true,
                CastData       = BuildCastData(spell, target, info)
            });
        }

        [SpellEffectHandler(SpellEffectType.Damage)]
        public static void HandleEffectDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamageInternal(spell, target, info, splitCount: 1, shieldOnly: false, applyTransferenceSideEffects: false);
        }

        private static void HandleEffectDamageInternal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, int splitCount, bool shieldOnly, bool applyTransferenceSideEffects)
        {
            if (!spell.Caster.CanAttack(target))
                return;

            // TODO: once spell effect handlers aren't static, this should be injected without the factory
            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            var damageCalculator = factory.Resolve();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);

            if (info.DropEffect || info.Damage == null)
                return;

            if (splitCount > 1)
            {
                int splitRank = GetEffectTargetRank(spell, info, target.Guid);
                ApplyApproximateDamageSplit(info.Damage, splitCount, splitRank);
            }

            if (shieldOnly)
            {
                // Approximation: route this effect's adjusted damage directly into shields only.
                uint shieldDamage = Math.Min(target.Shield, info.Damage.AdjustedDamage);
                info.Damage.ShieldAbsorbAmount = shieldDamage;
                info.Damage.AdjustedDamage = 0u;
            }

            uint healthBefore = target.Health;
            target.TakeDamage(spell.Caster, info.Damage);

            uint overkill = info.Damage.AdjustedDamage > healthBefore
                ? info.Damage.AdjustedDamage - healthBefore
                : 0u;

            info.Damage.OverkillAmount = overkill;
            info.Damage.KilledTarget = !target.IsAlive;

            List<CombatLogTransference.CombatHealData> transferenceHealedUnits = applyTransferenceSideEffects
                ? ApplyTransferenceSideEffects(spell, info)
                : null;

            AddDamageCombatLog(spell, target, info, transferenceHealedUnits);
        }

        [SpellEffectHandler(SpellEffectType.DistanceDependentDamage)]
        public static void HandleEffectDistanceDependentDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.DistributedDamage)]
        public static void HandleEffectDistributedDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            int splitCount = GetEffectTargetCount(spell, info);
            HandleEffectDamageInternal(spell, target, info, splitCount, shieldOnly: false, applyTransferenceSideEffects: false);
        }

        [SpellEffectHandler(SpellEffectType.DamageShields)]
        public static void HandleEffectDamageShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamageInternal(spell, target, info, splitCount: 1, shieldOnly: true, applyTransferenceSideEffects: false);
        }

        [SpellEffectHandler(SpellEffectType.Transference)]
        public static void HandleEffectTransference(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamageInternal(spell, target, info, splitCount: 1, shieldOnly: false, applyTransferenceSideEffects: true);
        }

        [SpellEffectHandler(SpellEffectType.Heal)]
        public static void HandleEffectHeal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            var damageCalculator = factory.Resolve();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);

            if (info.DropEffect || info.Damage == null)
                return;

            uint requestedHeal = info.Damage.AdjustedDamage;
            uint healthBefore = target.Health;
            uint healingAbsorptionBefore = target.HealingAbsorptionPool;

            target.ModifyHealth(requestedHeal, DamageType.Heal, spell.Caster);

            uint healAbsorption = healingAbsorptionBefore - target.HealingAbsorptionPool;
            uint healAfterAbsorption = requestedHeal - healAbsorption;
            uint effectiveHeal = target.Health > healthBefore
                ? target.Health - healthBefore
                : 0u;
            uint overheal = healAfterAbsorption - effectiveHeal;

            info.Damage.AdjustedDamage = effectiveHeal;
            info.Damage.AbsorbedAmount = healAbsorption;
            info.Damage.OverkillAmount = 0u;
            info.Damage.KilledTarget = false;

            info.AddCombatLog(new CombatLogHeal
            {
                HealAmount = effectiveHeal,
                Overheal   = overheal,
                Absorption = healAbsorption,
                EffectType = SpellEffectType.Heal,
                CastData   = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = info.Damage.CombatResult
                }
            });
        }

        [SpellEffectHandler(SpellEffectType.HealShields)]
        public static void HandleEffectHealShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            var damageCalculator = factory.Resolve();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);

            if (info.DropEffect || info.Damage == null)
                return;

            uint requestedShieldHeal = info.Damage.AdjustedDamage;
            uint healAbsorption = target.ConsumeHealingAbsorption(requestedShieldHeal);
            uint shieldHealAfterAbsorption = requestedShieldHeal - healAbsorption;
            uint shieldBefore = target.Shield;
            target.Shield = (uint)Math.Min((ulong)target.MaxShieldCapacity, (ulong)target.Shield + shieldHealAfterAbsorption);

            uint effectiveShieldHeal = target.Shield - shieldBefore;
            uint overheal = shieldHealAfterAbsorption - effectiveShieldHeal;
            info.Damage.AdjustedDamage = effectiveShieldHeal;
            info.Damage.AbsorbedAmount = healAbsorption;
            info.Damage.OverkillAmount = 0u;
            info.Damage.KilledTarget = false;

            info.AddCombatLog(new CombatLogHeal
            {
                HealAmount = effectiveShieldHeal,
                Overheal   = overheal,
                Absorption = healAbsorption,
                EffectType = SpellEffectType.HealShields,
                CastData   = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = info.Damage.CombatResult
                }
            });
        }

        [SpellEffectHandler(SpellEffectType.Absorption)]
        public static void HandleEffectAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
                return;

            uint before = target.DamageAbsorptionPool;
            target.AddDamageAbsorption(amount);
            uint appliedAmount = target.DamageAbsorptionPool - before;
            if (appliedAmount == 0u)
                return;

            info.AddCombatLog(new CombatLogAbsorption
            {
                AbsorptionAmount = appliedAmount,
                CastData         = BuildCastData(spell, target, info)
            });
        }

        [SpellEffectHandler(SpellEffectType.HealingAbsorption)]
        public static void HandleEffectHealingAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
                return;

            uint before = target.HealingAbsorptionPool;
            target.AddHealingAbsorption(amount);
            uint appliedAmount = target.HealingAbsorptionPool - before;
            if (appliedAmount == 0u)
                return;

            info.AddCombatLog(new CombatLogHealingAbsorption
            {
                Amount = appliedAmount
            });
        }

        [SpellEffectHandler(SpellEffectType.ModifyInterruptArmor)]
        public static void HandleEffectModifyInterruptArmor(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            int requestedDelta = DecodeSignedEffectAmount(info.Entry);
            if (requestedDelta == 0)
                return;

            uint before = target.InterruptArmor;
            uint after = ApplySignedDelta(before, requestedDelta);
            int appliedDelta = unchecked((int)after - (int)before);

            if (appliedDelta == 0)
                return;

            target.InterruptArmor = after;

            info.AddCombatLog(new CombatLogModifyInterruptArmor
            {
                Amount    = appliedDelta,
                CastData  = BuildCastData(spell, target, info)
            });
        }

        [SpellEffectHandler(SpellEffectType.Resurrect)]
        public static void HandleEffectResurrect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.ResurrectionManager.ResurrectRequest(spell.Caster.Guid);
        }

        [SpellEffectHandler(SpellEffectType.Kill)]
        public static void HandleEffectKill(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            uint lethalAmount = (uint)Math.Min(
                (ulong)uint.MaxValue,
                (ulong)Math.Max(1u, target.Health) + target.DamageAbsorptionPool);
            var damage = new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType        = DamageType.Physical,
                RawDamage         = lethalAmount,
                RawScaledDamage   = lethalAmount,
                AdjustedDamage    = lethalAmount,
                CombatResult      = CombatResult.Hit,
                ShieldAbsorbAmount = 0u,
                AbsorbedAmount    = 0u
            };

            info.AddDamage(damage);
            target.TakeDamage(spell.Caster, damage);

            if (!target.IsAlive)
                info.AddCombatLog(new CombatLogDeath { UnitId = target.Guid });
        }

        [SpellEffectHandler(SpellEffectType.Proxy)]
        public static void HandleEffectProxy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            target.CastSpell(info.Entry.DataBits00, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false
            });
        }

        [SpellEffectHandler(SpellEffectType.Disguise)]
        public static void HandleEffectDisguise(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            Creature2Entry creature2 = GameTableManager.Instance.Creature2.GetEntry(info.Entry.DataBits02);
            if (creature2 == null)
                return;

            Creature2DisplayGroupEntryEntry displayGroupEntry = GameTableManager.Instance.Creature2DisplayGroupEntry.Entries.FirstOrDefault(d => d.Creature2DisplayGroupId == creature2.Creature2DisplayGroupId);
            if (displayGroupEntry == null)
                return;

            target.DisplayInfo = displayGroupEntry.Creature2DisplayInfoId;
        }

        [SpellEffectHandler(SpellEffectType.SummonMount)]
        public static void HandleEffectSummonMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // TODO: handle NPC mounting?
            if (target is not IPlayer player)
                return;

            if (!player.CanMount())
                return;

            // TODO: needs to be replaced once spell effect handlers aren't static
            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();

            var mount = factory.CreateEntity<IMountEntity>();
            mount.Initialise(player, spell.Parameters.SpellInfo.Entry.Id, info.Entry.DataBits00, info.Entry.DataBits01, info.Entry.DataBits04);
            mount.EnqueuePassengerAdd(player, VehicleSeatType.Pilot, 0);

            // usually for hover boards
            /*if (info.Entry.DataBits04 > 0u)
            {
                mount.SetAppearance(new ItemVisual
                {
                    Slot      = ItemSlot.Mount,
                    DisplayId = (ushort)info.Entry.DataBits04
                });
            }*/

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(mount, position))
                player.Map.EnqueueAdd(mount, position);

            // FIXME: also cast 52539,Riding License - Riding Skill 1 - SWC - Tier 1,34464
            // FIXME: also cast 80530,Mount Sprint  - Tier 2,36122

            player.CastSpell(52539, new SpellParameters());
            player.CastSpell(80530, new SpellParameters());
        }

        [SpellEffectHandler(SpellEffectType.Teleport)]
        public static void HandleEffectTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            WorldLocation2Entry locationEntry = GameTableManager.Instance.WorldLocation2.GetEntry(info.Entry.DataBits00);
            if (locationEntry == null)
                return;

            if (target is IPlayer player)
                if (player.CanTeleport())
                    player.TeleportTo((ushort)locationEntry.WorldId, locationEntry.Position0, locationEntry.Position1, locationEntry.Position2);
        }

        [SpellEffectHandler(SpellEffectType.FullScreenEffect)]
        public static void HandleFullScreenEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // TODO/FIXME: Add duration into the queue so that the spell will automatically finish at the correct time. This is a workaround for Full Screen Effects.
            //events.EnqueueEvent(new Event.SpellEvent(info.Entry.DurationTime / 1000d, () => { status = SpellStatus.Finished; SendSpellFinish(); }));
        }

        [SpellEffectHandler(SpellEffectType.RapidTransport)]
        public static void HandleEffectRapidTransport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            TaxiNodeEntry taxiNode = GameTableManager.Instance.TaxiNode.GetEntry(spell.Parameters.TaxiNode);
            if (taxiNode == null)
                return;

            WorldLocation2Entry worldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
                return;

            if (target is not IPlayer player)
                return;

            if (!player.CanTeleport())
                return;

            var rotation = new Quaternion(worldLocation.Facing0, worldLocation.Facing1, worldLocation.Facing2, worldLocation.Facing3);
            player.Rotation = rotation.ToEuler();
            player.TeleportTo((ushort)worldLocation.WorldId, worldLocation.Position0, worldLocation.Position1, worldLocation.Position2);
        }

        [SpellEffectHandler(SpellEffectType.LearnDyeColor)]
        public static void HandleEffectLearnDyeColor(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.Account.GenericUnlockManager.Unlock((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.UnlockMount)]
        public static void HandleEffectUnlockMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(info.Entry.DataBits00);
            if (spell4Entry == null)
                return;

            player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);

            player.Session.EnqueueMessageEncrypted(new ServerUnlockMount
            {
                Spell4Id = info.Entry.DataBits00
            });
        }

        [SpellEffectHandler(SpellEffectType.UnlockPetFlair)]
        public static void HandleEffectUnlockPetFlair(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.PetCustomisationManager.UnlockFlair((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.UnlockVanityPet)]
        public static void HandleEffectUnlockVanityPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(info.Entry.DataBits00);
            if (spell4Entry == null)
                return;

            player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);

            player.Session.EnqueueMessageEncrypted(new ServerUnlockMount
            {
                Spell4Id = info.Entry.DataBits00
            });
        }

        [SpellEffectHandler(SpellEffectType.SummonVanityPet)]
        public static void HandleEffectSummonVanityPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            // enqueue removal of existing vanity pet if summoned
            if (player.VanityPetGuid != null)
            {
                IPetEntity oldVanityPet = player.GetVisible<IPetEntity>(player.VanityPetGuid.Value);
                oldVanityPet?.RemoveFromMap();
                player.VanityPetGuid = 0u;
            }

            // TODO: needs to be replaced once spell effect handlers aren't static
            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();

            var pet = factory.CreateEntity<IPetEntity>();
            pet.Initialise(player, info.Entry.DataBits00);

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(pet, position))
                player.Map.EnqueueAdd(pet, position);
        }

        [SpellEffectHandler(SpellEffectType.TitleGrant)]
        public static void HandleEffectTitleGrant(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.TitleManager.AddTitle((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.Fluff)]
        public static void HandleEffectFluff(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
        }

        [SpellEffectHandler(SpellEffectType.UnitPropertyModifier)]
        public static void HandleEffectPropertyModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // TODO: I suppose these could be cached somewhere instead of generating them every single effect?
            SpellPropertyModifier modifier = 
                new SpellPropertyModifier((Property)info.Entry.DataBits00, 
                    info.Entry.DataBits01, 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits02), 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits03), 
                    BitConverter.UInt32BitsToSingle(info.Entry.DataBits04));
            uint modifierInstanceId = spell.CastingId;
            target.AddSpellModifierProperty(modifier, modifierInstanceId);

            if (info.Entry.DurationTime > 0u)
            {
                Property property = (Property)info.Entry.DataBits00;
                spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => target.RemoveSpellProperty(property, modifierInstanceId));
            }
        }

        private static CombatLogCastData BuildCastData(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            return new CombatLogCastData
            {
                CasterId     = spell.Caster.Guid,
                TargetId     = target.Guid,
                SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                CombatResult = info.Damage?.CombatResult ?? CombatResult.Hit
            };
        }

        private static int GetEffectTargetCount(ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectTargetFlags effectTargetFlags = (SpellEffectTargetFlags)info.Entry.TargetFlags;
            int count = spell.Targets.Count(t => (t.Flags & effectTargetFlags) != 0);
            return Math.Max(1, count);
        }

        private static int GetEffectTargetRank(ISpell spell, ISpellTargetEffectInfo info, uint targetGuid)
        {
            SpellEffectTargetFlags effectTargetFlags = (SpellEffectTargetFlags)info.Entry.TargetFlags;
            List<uint> targetGuids = spell.Targets
                .Where(t => (t.Flags & effectTargetFlags) != 0)
                .Select(t => t.Entity.Guid)
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            int rank = targetGuids.IndexOf(targetGuid);
            return rank < 0 ? 0 : rank;
        }

        private static void ApplyApproximateDamageSplit(IDamageDescription damage, int splitCount, int splitRank)
        {
            if (splitCount <= 1)
                return;

            // Approximation: split all reported damage components across effect targets while preserving total.
            // TODO: Replace with effect-accurate distributed damage semantics (pre/post mitigation split rules).
            damage.RawDamage = DivideByCountWithRemainder(damage.RawDamage, splitCount, splitRank);
            damage.RawScaledDamage = DivideByCountWithRemainder(damage.RawScaledDamage, splitCount, splitRank);
            damage.AbsorbedAmount = DivideByCountWithRemainder(damage.AbsorbedAmount, splitCount, splitRank);
            damage.ShieldAbsorbAmount = DivideByCountWithRemainder(damage.ShieldAbsorbAmount, splitCount, splitRank);
            damage.AdjustedDamage = DivideByCountWithRemainder(damage.AdjustedDamage, splitCount, splitRank);
            damage.GlanceAmount = DivideByCountWithRemainder(damage.GlanceAmount, splitCount, splitRank);
        }

        private static List<CombatLogTransference.CombatHealData> ApplyTransferenceSideEffects(ISpell spell, ISpellTargetEffectInfo info)
        {
            if (info.Damage == null)
                return null;

            uint transferAmount = (uint)Math.Min((ulong)uint.MaxValue, (ulong)info.Damage.AdjustedDamage + info.Damage.ShieldAbsorbAmount);
            if (transferAmount == 0u)
                return null;

            Vital vital = ResolveTransferenceVital(info.Entry.DataBits00);
            float before = spell.Caster.GetVitalValue(vital);
            spell.Caster.ModifyVital(vital, transferAmount);
            float after = spell.Caster.GetVitalValue(vital);

            uint appliedAmount = after > before ? (uint)Math.Round(after - before) : 0u;
            uint overheal = transferAmount > appliedAmount ? transferAmount - appliedAmount : 0u;

            if (appliedAmount == 0u && overheal == 0u)
                return null;

            return
            [
                new CombatLogTransference.CombatHealData
                {
                    HealedUnitId = spell.Caster.Guid,
                    HealAmount   = appliedAmount,
                    Vital        = vital,
                    Overheal     = overheal,
                    Absorption   = 0u
                }
            ];
        }

        private static Vital ResolveTransferenceVital(uint dataBits00)
        {
            Vital vital = (Vital)dataBits00;
            return vital == Vital.Invalid || !Enum.IsDefined(vital)
                ? Vital.Health
                : vital;
        }

        private static void AddDamageCombatLog(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, List<CombatLogTransference.CombatHealData> transferenceHealedUnits = null)
        {
            if (info.Damage == null)
                return;

            SpellEffectType effectType = (SpellEffectType)info.Entry.EffectType;
            bool isMultiHit = info.Damage.MultiHitAmount != 0u;
            switch (effectType)
            {
                case SpellEffectType.DamageShields:
                    if (isMultiHit)
                    {
                        info.AddCombatLog(new CombatLogMultiHitShields
                        {
                            DamageAmount      = info.Damage.AdjustedDamage,
                            RawDamage         = info.Damage.RawDamage,
                            Shield            = info.Damage.ShieldAbsorbAmount,
                            Absorption        = info.Damage.AbsorbedAmount,
                            Overkill          = info.Damage.OverkillAmount,
                            GlanceAmount      = info.Damage.GlanceAmount,
                            BTargetVulnerable = false,
                            BKilled           = info.Damage.KilledTarget,
                            BPeriodic         = info.Entry.TickTime > 0u,
                            DamageType        = info.Damage.DamageType,
                            EffectType        = effectType,
                            CastData          = BuildCastData(spell, target, info)
                        });
                    }
                    else
                    {
                        info.AddCombatLog(new CombatLogDamageShield
                        {
                            MitigatedDamage   = info.Damage.AdjustedDamage,
                            RawDamage         = info.Damage.RawDamage,
                            Shield            = info.Damage.ShieldAbsorbAmount,
                            Absorption        = info.Damage.AbsorbedAmount,
                            Overkill          = info.Damage.OverkillAmount,
                            Glance            = info.Damage.GlanceAmount,
                            BTargetVulnerable = false,
                            BKilled           = info.Damage.KilledTarget,
                            BPeriodic         = info.Entry.TickTime > 0u,
                            DamageType        = info.Damage.DamageType,
                            EffectType        = effectType,
                            CastData          = BuildCastData(spell, target, info)
                        });
                    }
                    break;
                case SpellEffectType.Transference:
                    info.AddCombatLog(new CombatLogTransference
                    {
                        DamageAmount      = info.Damage.AdjustedDamage,
                        DamageType        = info.Damage.DamageType,
                        Shield            = info.Damage.ShieldAbsorbAmount,
                        Absorption        = info.Damage.AbsorbedAmount,
                        Overkill          = info.Damage.OverkillAmount,
                        GlanceAmount      = info.Damage.GlanceAmount,
                        BTargetVulnerable = false,
                        HealedUnits       = transferenceHealedUnits ?? []
                    });
                    break;
                default:
                    if (isMultiHit)
                    {
                        info.AddCombatLog(new CombatLogMultiHit
                        {
                            DamageAmount      = info.Damage.AdjustedDamage,
                            RawDamage         = info.Damage.RawDamage,
                            Shield            = info.Damage.ShieldAbsorbAmount,
                            Absorption        = info.Damage.AbsorbedAmount,
                            Overkill          = info.Damage.OverkillAmount,
                            GlanceAmount      = info.Damage.GlanceAmount,
                            BTargetVulnerable = false,
                            BKilled           = info.Damage.KilledTarget,
                            BPeriodic         = info.Entry.TickTime > 0u,
                            DamageType        = info.Damage.DamageType,
                            EffectType        = effectType,
                            CastData          = BuildCastData(spell, target, info)
                        });
                    }
                    else
                    {
                        info.AddCombatLog(new CombatLogDamage
                        {
                            MitigatedDamage   = info.Damage.AdjustedDamage,
                            RawDamage         = info.Damage.RawDamage,
                            Shield            = info.Damage.ShieldAbsorbAmount,
                            Absorption        = info.Damage.AbsorbedAmount,
                            Overkill          = info.Damage.OverkillAmount,
                            Glance            = info.Damage.GlanceAmount,
                            BTargetVulnerable = false,
                            BKilled           = info.Damage.KilledTarget,
                            BPeriodic         = info.Entry.TickTime > 0u,
                            DamageType        = info.Damage.DamageType,
                            EffectType        = effectType,
                            CastData          = BuildCastData(spell, target, info)
                        });
                    }
                    break;
            }
        }
        private static uint DecodeUnsignedEffectAmount(Spell4EffectsEntry entry)
        {
            if (entry.DataBits01 != 0u)
                return entry.DataBits01;

            if (entry.DataBits00 != 0u)
                return entry.DataBits00;

            for (int i = 0; i < entry.ParameterValue.Length; i++)
            {
                float value = entry.ParameterValue[i];
                if (value > 0f)
                    return (uint)Math.Round(value);
            }

            return 0u;
        }

        private static int DecodeSignedEffectAmount(Spell4EffectsEntry entry)
        {
            if (entry.DataBits01 != 0u)
                return unchecked((int)entry.DataBits01);

            if (entry.DataBits00 != 0u)
                return unchecked((int)entry.DataBits00);

            for (int i = 0; i < entry.ParameterValue.Length; i++)
            {
                float value = entry.ParameterValue[i];
                if (value != 0f)
                    return (int)Math.Round(value);
            }

            return 0;
        }

        private static uint ApplySignedDelta(uint current, int delta)
        {
            if (delta >= 0)
            {
                ulong result = (ulong)current + (ulong)delta;
                return (uint)Math.Min(result, uint.MaxValue);
            }

            uint amount = (uint)Math.Min((long)(-(long)delta), uint.MaxValue);
            return amount >= current ? 0u : current - amount;
        }

        private static uint DivideByCountWithRemainder(uint value, int count, int rank)
        {
            if (value == 0u || count <= 1)
                return value;

            uint divisor = (uint)Math.Max(count, 1);
            uint quotient = value / divisor;
            uint remainder = value % divisor;
            return rank < remainder ? quotient + 1u : quotient;
        }
    }
}
