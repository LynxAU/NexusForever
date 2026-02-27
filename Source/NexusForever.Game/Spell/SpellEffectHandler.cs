using System;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Hazard;
using NexusForever.Game.Map;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Reputation;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model.Hazard;
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
            SchedulePeriodicTicks(spell, target, info, tickInfo =>
                HandleEffectDamageInternal(spell, target, tickInfo, splitCount: 1, shieldOnly: false, applyTransferenceSideEffects: false));
        }

        private static void HandleEffectDamageInternal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, int splitCount, bool shieldOnly, bool applyTransferenceSideEffects)
        {
            if (!spell.Caster.CanAttack(target))
                return;

            // NOTE: static handlers resolve calculators through the legacy provider.
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

            if (spell.Caster is UnitEntity casterUnit)
                casterUnit.NotifyProcDamageDone(target, info.Damage.AdjustedDamage + info.Damage.ShieldAbsorbAmount, spell.Parameters.SpellInfo.Entry.Id);
            if (target is UnitEntity targetUnit)
                targetUnit.NotifyProcDamageTaken(spell.Caster, info.Damage.AdjustedDamage + info.Damage.ShieldAbsorbAmount, spell.Parameters.SpellInfo.Entry.Id);

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

        [SpellEffectHandler(SpellEffectType.ForcedMove)]
        public static void HandleEffectForcedMove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null || spell.Caster == null || target.Guid == spell.Caster.Guid)
                return;

            if (!TryResolveForcedMoveDistance(info.Entry, out float distance))
                return;

            uint travelMs = ResolveForcedMoveTravelMs(info.Entry);
            if (travelMs == 0u)
                return;

            Vector3 direction = Vector3.Normalize(target.Position - spell.Caster.Position);
            if (!IsFinite(direction) || direction.LengthSquared() < 0.0001f)
            {
                Vector3 fallback = target.Position - spell.Caster.Position;
                if (fallback.LengthSquared() < 0.0001f)
                    return;

                direction = Vector3.Normalize(fallback);
            }

            bool pullTowardCaster = info.Entry.DataBits00 is 3u or 9u or 18u;
            Vector3 offset = direction * (pullTowardCaster ? -distance : distance);
            Vector3 destination = target.Position + offset;

            target.CancelSpellsOnMove();
            target.MovementManager.SetState(NexusForever.Game.Static.Entity.Movement.Command.State.StateFlags.Move);
            target.MovementManager.SetPositionKeys(
                [0u, travelMs],
                [target.Position, destination]);
        }

        [SpellEffectHandler(SpellEffectType.Activate)]
        public static void HandleEffectActivate(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            if (candidates.Count == 0)
                return;

            IUnitEntity activationSource = target ?? spell.Caster;
            if (activationSource == null || !activationSource.IsAlive)
                return;

            activationSource.CastSpell(candidates[0], new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false,
                PrimaryTargetId        = spell.Caster?.Guid ?? activationSource.Guid
            });
        }

        [SpellEffectHandler(SpellEffectType.NpcExecutionDelay)]
        public static void HandleEffectNpcExecutionDelay(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint delayMs = info.Entry.DataBits00;
            if (delayMs == 0u || delayMs == uint.MaxValue)
                delayMs = info.Entry.DurationTime;
            if (delayMs == 0u || delayMs == uint.MaxValue)
                delayMs = 250u;

            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            uint linkedSpellId = candidates.FirstOrDefault(id => id != spell.Parameters.SpellInfo.Entry.Id);

            if (linkedSpellId == 0u)
            {
                spell.EnqueueEvent(delayMs / 1000d, () => { });
                return;
            }

            IUnitEntity activationSource = target ?? spell.Caster;
            spell.EnqueueEvent(delayMs / 1000d, () =>
            {
                if (activationSource == null || !activationSource.IsAlive)
                    return;

                activationSource.CastSpell(linkedSpellId, new SpellParameters
                {
                    ParentSpellInfo        = spell.Parameters.SpellInfo,
                    RootSpellInfo          = spell.Parameters.RootSpellInfo,
                    UserInitiatedSpellCast = false,
                    PrimaryTargetId        = spell.Caster?.Guid ?? activationSource.Guid
                });
            });
        }

        [SpellEffectHandler(SpellEffectType.FactionSet)]
        public static void HandleEffectFactionSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            if (!Enum.IsDefined(typeof(Faction), (int)info.Entry.DataBits00))
                return;

            Faction faction = (Faction)info.Entry.DataBits00;
            if (info.Entry.DurationTime == 0u)
            {
                target.SetFaction(faction);
                return;
            }

            target.SetTemporaryFaction(faction);
            spell.EnqueueEvent(info.Entry.DurationTime / 1000d, target.RemoveTemporaryFaction);
        }

        [SpellEffectHandler(SpellEffectType.ChangePhase)]
        public static void HandleEffectChangePhase(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint phaseMask = info.Entry.DataBits00 != 0u
                ? info.Entry.DataBits00
                : (info.Entry.DataBits01 != 0u ? info.Entry.DataBits01 : 1u);

            player.Session.EnqueueMessageEncrypted(new ServerPhaseVisibilityWorldLocation
            {
                PhasesIPerceive      = phaseMask,
                PhasesThatPerceiveMe = phaseMask
            });
        }

        [SpellEffectHandler(SpellEffectType.ActionBarSet)]
        public static void HandleEffectActionBarSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            ushort actionBarShortcutSetId = (ushort)(info.Entry.DataBits00 & 0x3FFFu);
            if (actionBarShortcutSetId == 0u)
                actionBarShortcutSetId = (ushort)(info.Entry.DataBits01 & 0x3FFFu);
            if (actionBarShortcutSetId == 0u)
                return;

            ShortcutSet shortcutSet = Enum.IsDefined(typeof(ShortcutSet), (int)info.Entry.DataBits01)
                ? (ShortcutSet)info.Entry.DataBits01
                : ShortcutSet.FloatingSpellBar;

            player.Session.EnqueueMessageEncrypted(new ServerShowActionBar
            {
                ShortcutSet            = shortcutSet,
                ActionBarShortcutSetId = actionBarShortcutSetId,
                AssociatedUnitId       = player.Guid
            });
        }

        [SpellEffectHandler(SpellEffectType.SetBusy)]
        public static void HandleEffectSetBusy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            bool setBusy = info.Entry.DataBits00 != 0u || info.Entry.DurationTime > 0u || info.Entry.DataBits01 > 0u;
            if (!setBusy)
            {
                target.RemoveCrowdControlState(CCState.AbilityRestriction, spell.Caster.Guid);
                return;
            }

            uint durationMs = info.Entry.DurationTime > 0u
                ? info.Entry.DurationTime
                : (info.Entry.DataBits01 > 0u ? info.Entry.DataBits01 : 1000u);

            uint appliedDurationMs = target.ApplyCrowdControlState(CCState.AbilityRestriction, durationMs, spell.Caster.Guid, 0u);
            if (appliedDurationMs == 0u)
                return;

            spell.Caster.EnqueueToVisible(new ServerEntityCCStateSet
            {
                UnitId              = target.Guid,
                CCType              = CCState.AbilityRestriction,
                SpellEffectUniqueId = info.EffectId
            }, true);
        }

        [SpellEffectHandler(SpellEffectType.Heal)]
        public static void HandleEffectHeal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectHealInternal(spell, target, info);
            SchedulePeriodicTicks(spell, target, info, tickInfo => HandleEffectHealInternal(spell, target, tickInfo));
        }

        private static void HandleEffectHealInternal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
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
            uint overheal = healAfterAbsorption > effectiveHeal ? healAfterAbsorption - effectiveHeal : 0u;

            info.Damage.AdjustedDamage = effectiveHeal;
            info.Damage.AbsorbedAmount = healAbsorption;
            info.Damage.OverkillAmount = 0u;
            info.Damage.KilledTarget = false;

            if (spell.Caster is UnitEntity casterUnit)
                casterUnit.NotifyProcHealDone(target, effectiveHeal, spell.Parameters.SpellInfo.Entry.Id);
            if (target is UnitEntity targetUnit)
                targetUnit.NotifyProcHealTaken(spell.Caster, effectiveHeal, spell.Parameters.SpellInfo.Entry.Id);

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
            HandleEffectHealShieldsInternal(spell, target, info);
            SchedulePeriodicTicks(spell, target, info, tickInfo => HandleEffectHealShieldsInternal(spell, target, tickInfo));
        }

        private static void HandleEffectHealShieldsInternal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
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

            uint effectiveShieldHeal = target.Shield >= shieldBefore ? target.Shield - shieldBefore : 0u;
            uint overheal = shieldHealAfterAbsorption >= effectiveShieldHeal ? shieldHealAfterAbsorption - effectiveShieldHeal : 0u;
            info.Damage.AdjustedDamage = effectiveShieldHeal;
            info.Damage.AbsorbedAmount = healAbsorption;
            info.Damage.OverkillAmount = 0u;
            info.Damage.KilledTarget = false;

            if (spell.Caster is UnitEntity casterUnit)
                casterUnit.NotifyProcHealDone(target, effectiveShieldHeal, spell.Parameters.SpellInfo.Entry.Id);
            if (target is UnitEntity targetUnit)
                targetUnit.NotifyProcHealTaken(spell.Caster, effectiveShieldHeal, spell.Parameters.SpellInfo.Entry.Id);

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

        [SpellEffectHandler(SpellEffectType.Proc)]
        public static void HandleEffectProc(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not UnitEntity unitEntity)
                return;

            uint triggerSpellId = info.Entry.DataBits01;
            if (triggerSpellId == 0u)
                return;

            double durationSeconds = info.Entry.DurationTime / 1000d;
            double chance01 = ResolveProcChance(info.Entry);
            UnitEntity.ProcEventMask eventMask = ResolveProcEventMask(info.Entry.DataBits03);
            double internalCooldownSeconds = ResolveProcCooldownSeconds(info.Entry.DataBits04);
            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;

            unitEntity.AddProcTrigger(
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Caster.Guid,
                triggerSpellId,
                durationSeconds,
                chance01,
                eventMask,
                internalCooldownSeconds,
                stackGroupId,
                stackCap,
                stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.UnitStateSet)]
        public static void HandleEffectUnitStateSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            if (!Enum.IsDefined(typeof(CCState), (int)info.Entry.DataBits00))
                return;

            var state = (CCState)info.Entry.DataBits00;
            uint durationMs = info.Entry.DurationTime > 0u
                ? info.Entry.DurationTime
                : info.Entry.DataBits01;
            if (durationMs == 0u)
                return;

            CCStatesEntry ccStateEntry = GameTableManager.Instance.CCStates.GetEntry((uint)state);
            uint diminishingReturnsId = ccStateEntry?.CcStateDiminishingReturnsId ?? 0u;
            uint appliedDurationMs = target.ApplyCrowdControlState(state, durationMs, spell.Caster.Guid, diminishingReturnsId);
            if (appliedDurationMs == 0u)
                return;

            spell.Caster.EnqueueToVisible(new ServerEntityCCStateSet
            {
                UnitId              = target.Guid,
                CCType              = state,
                SpellEffectUniqueId = info.EffectId
            }, true);

            info.AddCombatLog(new CombatLogCCState
            {
                State                       = state,
                BRemoved                    = false,
                InterruptArmorTaken         = 0u,
                Result                      = CCStateApplyRulesResult.Ok,
                CcStateDiminishingReturnsId = (ushort)diminishingReturnsId,
                CastData                    = BuildCastData(spell, target, info)
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

        [SpellEffectHandler(SpellEffectType.ThreatModification)]
        public static void HandleEffectThreatModification(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null || spell.Caster == null || !target.IsAlive)
                return;

            int threatDelta = DecodeSignedEffectAmount(info.Entry);
            if (threatDelta == 0)
                return;

            target.ThreatManager.UpdateThreat(spell.Caster, threatDelta);
        }

        [SpellEffectHandler(SpellEffectType.ThreatTransfer)]
        public static void HandleEffectThreatTransfer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (spell.Caster == null || target == null || spell.Caster.Guid == target.Guid)
                return;

            // Approximation:
            // DataBits00/DataBits01 may encode transfer percentage depending on spell data.
            // Favor DataBits01 first, then DataBits00, then first parameter value.
            int percent = DecodeSignedEffectAmount(info.Entry);
            if (percent == 0 && info.Entry.ParameterValue.Length > 0)
                percent = (int)Math.Round(info.Entry.ParameterValue[0] * 100f);
            percent = Math.Clamp(percent, 0, 100);
            if (percent == 0)
                return;

            foreach (IHostileEntity hostile in spell.Caster.ThreatManager.ToList())
            {
                IUnitEntity hostileUnit = spell.Caster.GetVisible<IUnitEntity>(hostile.HatedUnitId);
                if (hostileUnit == null || !hostileUnit.IsAlive)
                    continue;

                IHostileEntity casterThreat = hostileUnit.ThreatManager.GetHostile(spell.Caster.Guid);
                if (casterThreat == null || casterThreat.Threat == 0u)
                    continue;

                uint transferAmount = (uint)Math.Min((ulong)int.MaxValue, (ulong)casterThreat.Threat * (ulong)percent / 100UL);
                if (transferAmount == 0u)
                    continue;

                hostileUnit.ThreatManager.UpdateThreat(spell.Caster, -(int)transferAmount);
                hostileUnit.ThreatManager.UpdateThreat(target, (int)transferAmount);
            }
        }

        [SpellEffectHandler(SpellEffectType.CCStateSet)]
        public static void HandleEffectCCStateSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            if (!Enum.IsDefined(typeof(CCState), (int)info.Entry.DataBits00))
                return;

            var state = (CCState)info.Entry.DataBits00;
            uint durationMs = info.Entry.DurationTime != 0u
                ? info.Entry.DurationTime
                : info.Entry.DataBits01;

            CCStatesEntry ccStateEntry = GameTableManager.Instance.CCStates.GetEntry((uint)state);
            uint diminishingReturnsId = ccStateEntry?.CcStateDiminishingReturnsId ?? 0u;
            uint appliedDurationMs = target.ApplyCrowdControlState(state, durationMs, spell.Caster.Guid, diminishingReturnsId);

            if (appliedDurationMs == 0u)
            {
                info.AddCombatLog(new CombatLogCCState
                {
                    State                       = state,
                    BRemoved                    = false,
                    InterruptArmorTaken         = 0u,
                    Result                      = CCStateApplyRulesResult.DiminishingReturnsTriggerCap,
                    CcStateDiminishingReturnsId = (ushort)diminishingReturnsId,
                    CastData                    = BuildCastData(spell, target, info)
                });
                return;
            }

            spell.Caster.EnqueueToVisible(new ServerEntityCCStateSet
            {
                UnitId              = target.Guid,
                CCType              = state,
                SpellEffectUniqueId = info.EffectId
            }, true);

            info.AddCombatLog(new CombatLogCCState
            {
                State                    = state,
                BRemoved                 = false,
                InterruptArmorTaken      = 0u,
                Result                   = CCStateApplyRulesResult.Ok,
                CcStateDiminishingReturnsId = (ushort)diminishingReturnsId,
                CastData                 = BuildCastData(spell, target, info)
            });
        }

        [SpellEffectHandler(SpellEffectType.CCStateBreak)]
        public static void HandleEffectCCStateBreak(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            uint payload = info.Entry.DataBits00;
            uint removedCount;
            if (payload <= (uint)CCState.AbilityRestriction && Enum.IsDefined(typeof(CCState), (int)payload))
            {
                removedCount = target.RemoveCrowdControlState((CCState)payload, spell.Caster.Guid) ? 1u : 0u;
            }
            else
            {
                removedCount = target.RemoveCrowdControlStatesByMask(payload, spell.Caster.Guid);
            }

            if (removedCount == 0u && payload > (uint)CCState.AbilityRestriction)
                target.RemoveAllCrowdControlStates(spell.Caster.Guid);
        }

        [SpellEffectHandler(SpellEffectType.SpellDispel)]
        public static void HandleEffectSpellDispel(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            (bool removeBuffs, bool removeDebuffs) = ResolveDispelClassTargets(info.Entry.DataBits03);
            uint maxInstances = ResolveDispelInstanceLimit(info.Entry);

            uint removed = 0u;
            if (target is UnitEntity unitEntity)
                removed += unitEntity.RemoveDispelledAuras(removeBuffs, removeDebuffs, maxInstances);

            if (removeDebuffs)
                removed += target.RemoveAllCrowdControlStates(spell.Caster.Guid);

            if (removed == 0u)
                return;

            info.AddCombatLog(new CombatLogDispel
            {
                BRemovesSingleInstance = removed == 1u,
                InstancesRemoved       = removed,
                SpellRemovedId         = 0u
            });
        }

        [SpellEffectHandler(SpellEffectType.SpellForceRemoveChanneled)]
        public static void HandleEffectSpellForceRemoveChanneled(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            uint spellFilterId = candidates.FirstOrDefault(id => id != spell.Parameters.SpellInfo.Entry.Id);
            ISpell activeSpell = target.GetActiveSpell(s =>
                s.IsCasting && (spellFilterId == 0u || s.Parameters.SpellInfo.Entry.Id == spellFilterId));
            if (activeSpell == null)
                return;

            target.CancelSpellCast(activeSpell.CastingId, Network.World.Message.Static.CastResult.SpellInterrupted);
        }

        [SpellEffectHandler(SpellEffectType.SpellForceRemove)]
        public static void HandleEffectSpellForceRemove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            uint spellIdToRemove = candidates.FirstOrDefault(id => id != spell.Parameters.SpellInfo.Entry.Id);
            if (spellIdToRemove == 0u)
                return;

            if (target is UnitEntity unitEntity)
                unitEntity.RemoveTimedAurasBySpellId(spellIdToRemove);

            target.RemoveSpellProperties(spellIdToRemove);

            ISpell activeSpell = target.GetActiveSpell(s => s.Parameters.SpellInfo.Entry.Id == spellIdToRemove);
            if (activeSpell?.IsCasting == true)
                target.CancelSpellCast(activeSpell.CastingId, Network.World.Message.Static.CastResult.SpellCancelled);
        }

        [SpellEffectHandler(SpellEffectType.ModifySpellCooldown)]
        public static void HandleEffectModifySpellCooldown(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint targetSpellId = ResolveEffectTargetSpellId(info.Entry, spell.Parameters.SpellInfo.Entry.Id);
            double deltaSeconds = ResolveCooldownDeltaSeconds(info.Entry, targetSpellId);
            if (deltaSeconds == 0d)
                return;

            double currentCooldown = player.SpellManager.GetSpellCooldown(targetSpellId);
            double newCooldown = Math.Max(0d, currentCooldown + deltaSeconds);
            player.SpellManager.SetSpellCooldown(targetSpellId, newCooldown);
        }

        [SpellEffectHandler(SpellEffectType.ModifySpell)]
        public static void HandleEffectModifySpell(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target != null
                && info.Entry.DataBits00 == 0u
                && info.Entry.DataBits01 <= byte.MaxValue
                && Enum.IsDefined(typeof(Property), (byte)info.Entry.DataBits01))
            {
                // Data-driven shape used by many ModifySpell rows:
                // DataBits01=Property, DataBits02=Value, DataBits03=ModType.
                Spell4EffectsEntry remapped = CloneEffectEntry(info.Entry);
                remapped.DataBits00 = info.Entry.DataBits01;
                remapped.DataBits01 = info.Entry.DataBits03 <= 3u ? info.Entry.DataBits03 : 0u;
                remapped.DataBits02 = info.Entry.DataBits02;
                remapped.DataBits03 = info.Entry.DataBits03 > 3u ? info.Entry.DataBits03 : info.Entry.DataBits04;
                remapped.DataBits04 = 0u;

                var remappedInfo = new SpellTargetInfo.SpellTargetEffectInfo(info.EffectId, remapped);
                HandleEffectPropertyModifier(spell, target, remappedInfo);
                return;
            }

            HandleEffectModifySpellCooldown(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.ModifySpellEffect)]
        public static void HandleEffectModifySpellEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint targetSpellId = ResolveEffectTargetSpellId(info.Entry, 0u);
            if (targetSpellId == 0u)
            {
                // Avoid applying arbitrary cooldown deltas when payload does not identify a spell.
                return;
            }

            HandleEffectModifySpellCooldown(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.CooldownReset)]
        public static void HandleEffectCooldownReset(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint targetSpellId = ResolveEffectTargetSpellId(info.Entry, 0u);
            if (targetSpellId != 0u)
            {
                player.SpellManager.SetSpellCooldown(targetSpellId, 0d);
                return;
            }

            player.SpellManager.ResetAllSpellCooldowns();
        }

        [SpellEffectHandler(SpellEffectType.ActivateSpellCooldown)]
        public static void HandleEffectActivateSpellCooldown(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint targetSpellId = ResolveEffectTargetSpellId(info.Entry, spell.Parameters.SpellInfo.Entry.Id);

            double cooldownSeconds = 0d;
            if (info.Entry.DataBits01 > 0u)
            {
                cooldownSeconds = info.Entry.DataBits01 > 1000u
                    ? info.Entry.DataBits01 / 1000d
                    : info.Entry.DataBits01;
            }
            else
            {
                Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(targetSpellId);
                cooldownSeconds = spell4Entry?.SpellCoolDown > 0u
                    ? spell4Entry.SpellCoolDown / 1000d
                    : 0d;
            }

            if (cooldownSeconds <= 0d)
                return;

            player.SpellManager.SetSpellCooldown(targetSpellId, cooldownSeconds);
        }

        [SpellEffectHandler(SpellEffectType.AddSpell)]
        public static void HandleEffectAddSpell(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint rawSpellId = info.Entry.DataBits00;
            if (rawSpellId == 0u)
                return;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(rawSpellId);
            if (spell4Entry != null)
            {
                uint baseId = spell4Entry.Spell4BaseIdBaseSpell;
                if (player.SpellManager.GetSpell(baseId) == null)
                    player.SpellManager.AddSpell(baseId);
                return;
            }

            Spell4BaseEntry spellBaseEntry = GameTableManager.Instance.Spell4Base.GetEntry(rawSpellId);
            if (spellBaseEntry != null && player.SpellManager.GetSpell(spellBaseEntry.Id) == null)
                player.SpellManager.AddSpell(spellBaseEntry.Id);
        }

        [SpellEffectHandler(SpellEffectType.Resurrect)]
        public static void HandleEffectResurrect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.ResurrectionManager.ResurrectRequest(spell.Caster.Guid);
        }

        [SpellEffectHandler(SpellEffectType.TradeSkillProfession)]
        public static void HandleEffectTradeSkillProfession(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            // DataBits00 is expected to carry the tradeskill id when present.
            uint tradeskillId = info.Entry.DataBits00;
            if (Enum.IsDefined(typeof(TradeskillType), (int)tradeskillId))
            {
                player.TryLearnTradeskill((TradeskillType)tradeskillId);
                player.Session.EnqueueMessageEncrypted(new ServerProfessionUpdate
                {
                    Tradeskill = new Network.World.Message.Model.Shared.TradeskillInfo
                    {
                        TradeskillId = (TradeskillType)tradeskillId,
                        IsActive     = 1u
                    }
                });
            }

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.LearnTradeskill, tradeskillId, 1u);
        }

        [SpellEffectHandler(SpellEffectType.GiveSchematic)]
        public static void HandleEffectGiveSchematic(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            // DataBits00 is expected to carry the tradeskill schematic id when present.
            uint schematicId = info.Entry.DataBits00;
            TradeskillSchematic2Entry entry = GameTableManager.Instance.TradeskillSchematic2.GetEntry(schematicId);
            if (entry != null && player.TryLearnSchematic(schematicId) && Enum.IsDefined(typeof(TradeskillType), (int)entry.TradeSkillId))
            {
                player.Session.EnqueueMessageEncrypted(new ServerSchematicAddLearned
                {
                    TradeskillId = (TradeskillType)entry.TradeSkillId,
                    TradeskillSchematic2Id = schematicId
                });
            }

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ObtainSchematic, schematicId, 1u);
        }

        [SpellEffectHandler(SpellEffectType.CraftItem)]
        public static void HandleEffectCraftItem(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            // DataBits00 is expected to carry the tradeskill schematic id when present.
            uint schematicId = info.Entry.DataBits00;
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, schematicId, 1u);
        }

        [SpellEffectHandler(SpellEffectType.ForcedAction)]
        public static void HandleEffectForcedAction(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            uint forcedSpellId = info.Entry.DataBits00 != 0u
                ? info.Entry.DataBits00
                : info.Entry.DataBits01;
            if (forcedSpellId == 0u)
                return;

            target.CastSpell(forcedSpellId, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false,
                PrimaryTargetId        = spell.Caster.Guid
            });
        }

        [SpellEffectHandler(SpellEffectType.DelayDeath)]
        public static void HandleEffectDelayDeath(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not UnitEntity unitEntity)
                return;

            double durationSeconds = info.Entry.DurationTime > 0u
                ? info.Entry.DurationTime / 1000d
                : 5d;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddTimedAura(
                spell.Parameters.SpellInfo.Entry.Id,
                SpellEffectType.DelayDeath,
                spell.Caster.Guid,
                durationSeconds,
                0d,
                stackGroupId: stackGroupId,
                stackCap: stackCap,
                stackTypeEnum: stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);

            info.AddCombatLog(new CombatLogDelayDeath
            {
                CastData = BuildCastData(spell, target, info)
            });
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

        [SpellEffectHandler(SpellEffectType.SummonCreature)]
        public static void HandleEffectSummonCreature(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            uint creatureId = info.Entry.DataBits00;
            if (creatureId == 0u || spell.Caster?.Map == null)
                return;

            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();
            var creature = factory?.CreateEntity<INonPlayerEntity>();
            if (creature == null)
                return;

            creature.Initialise(creatureId);

            var position = new MapPosition
            {
                Position = target?.Position ?? spell.Caster.Position
            };

            if (spell.Caster.Map.CanEnter(creature, position))
                spell.Caster.Map.EnqueueAdd(creature, position);
        }

        [SpellEffectHandler(SpellEffectType.SummonTrap)]
        public static void HandleEffectSummonTrap(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectSummonCreature(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.DespawnUnit)]
        public static void HandleEffectDespawnUnit(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            target.RemoveFromMap();
        }

        [SpellEffectHandler(SpellEffectType.ItemVisualSwap)]
        public static void HandleEffectItemVisualSwap(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            if (!TryResolveItemVisualSwapSlot(info.Entry.DataBits00, out ItemSlot slot))
                return;

            ushort newDisplayId = (ushort)Math.Min(info.Entry.DataBits01, ushort.MaxValue);
            IItemVisual existingVisual = target.GetVisuals().FirstOrDefault(v => v.Slot == slot);
            ushort? oldDisplayId = existingVisual?.DisplayId;

            Action apply = () =>
            {
                if (newDisplayId == 0u)
                    target.RemoveVisual(slot);
                else
                    target.AddVisual(slot, newDisplayId);
            };

            Action revert = () =>
            {
                if (oldDisplayId.HasValue && oldDisplayId.Value > 0u)
                    target.AddVisual(slot, oldDisplayId.Value);
                else
                    target.RemoveVisual(slot);
            };

            if (info.Entry.DurationTime > 0u && target is UnitEntity unitEntity)
            {
                uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
                uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
                uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
                unitEntity.AddTimedAura(
                    spell.Parameters.SpellInfo.Entry.Id,
                    info.Entry.EffectType,
                    spell.Caster.Guid,
                    info.Entry.DurationTime / 1000d,
                    0d,
                    onApply: apply,
                    onRemove: revert,
                    stackGroupId: stackGroupId,
                    stackCap: stackCap,
                    stackTypeEnum: stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                return;
            }

            apply();
        }

        [SpellEffectHandler(SpellEffectType.Proxy)]
        public static void HandleEffectProxy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            if (candidates.Count == 0)
                return;

            IUnitEntity castSource = target ?? spell.Caster;
            castSource.CastSpell(candidates[0], new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false
            });
        }

        [SpellEffectHandler(SpellEffectType.ProxyLinearAE)]
        public static void HandleEffectProxyLinearAE(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            if (candidates.Count == 0)
                return;

            IUnitEntity castSource = target ?? spell.Caster;
            castSource.CastSpell(candidates[0], new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false
            });
        }

        [SpellEffectHandler(SpellEffectType.ProxyChannel)]
        public static void HandleEffectProxyChannel(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectProxyChannelVariableTime(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.ProxyChannelVariableTime)]
        public static void HandleEffectProxyChannelVariableTime(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            if (candidates.Count == 0)
                return;

            IUnitEntity castSource = target ?? spell.Caster;
            castSource.CastSpell(candidates[0], new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false
            });
        }

        [SpellEffectHandler(SpellEffectType.ProxyRandomExclusive)]
        public static void HandleEffectProxyRandomExclusive(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            if (candidates.Count == 0)
                return;

            uint selectedSpellId = candidates[Random.Shared.Next(candidates.Count)];
            IUnitEntity castSource = target ?? spell.Caster;
            castSource.CastSpell(selectedSpellId, new SpellParameters
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

        [SpellEffectHandler(SpellEffectType.DisguiseOutfit)]
        public static void HandleEffectDisguiseOutfit(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            uint oldDisplayInfo = target.DisplayInfo;
            ushort oldOutfitInfo = target.OutfitInfo;

            uint newDisplayInfo = oldDisplayInfo;
            ushort newOutfitInfo = oldOutfitInfo;
            if (info.Entry.DataBits00 != 0u && GameTableManager.Instance.Creature2OutfitInfo.GetEntry(info.Entry.DataBits00) != null)
                newOutfitInfo = (ushort)Math.Min(info.Entry.DataBits00, ushort.MaxValue);

            if (info.Entry.DataBits01 != 0u && GameTableManager.Instance.Creature2DisplayInfo.GetEntry(info.Entry.DataBits01) != null)
                newDisplayInfo = info.Entry.DataBits01;

            if (newDisplayInfo == oldDisplayInfo && newOutfitInfo == oldOutfitInfo)
                return;

            Action apply = () => target.SetVisualInfo(newDisplayInfo, newOutfitInfo);
            Action revert = () => target.SetVisualInfo(oldDisplayInfo, oldOutfitInfo);

            if (info.Entry.DurationTime > 0u && target is UnitEntity unitEntity)
            {
                uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
                uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
                uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
                unitEntity.AddTimedAura(
                    spell.Parameters.SpellInfo.Entry.Id,
                    info.Entry.EffectType,
                    spell.Caster.Guid,
                    info.Entry.DurationTime / 1000d,
                    0d,
                    onApply: apply,
                    onRemove: revert,
                    stackGroupId: stackGroupId,
                    stackCap: stackCap,
                    stackTypeEnum: stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                return;
            }

            apply();
        }

        [SpellEffectHandler(SpellEffectType.SummonMount)]
        public static void HandleEffectSummonMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            if (!player.CanMount())
                return;

            // NOTE: static handlers resolve entity creation through the legacy provider.
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

            player.CastSpell(52539, new SpellParameters());
            player.CastSpell(80530, new SpellParameters());
        }

        [SpellEffectHandler(SpellEffectType.SummonVehicle)]
        public static void HandleEffectSummonVehicle(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectSummonMount(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.Disembark)]
        public static void HandleEffectDisembark(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.Dismount();
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

        [SpellEffectHandler(SpellEffectType.HousingTeleport)]
        public static void HandleEffectHousingTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectTeleport(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.FullScreenEffect)]
        public static void HandleFullScreenEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (info.Entry.DurationTime == 0u)
                return;

            // Keep the spell active for the screen effect duration before finish can be sent.
            spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => { });
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

        [SpellEffectHandler(SpellEffectType.UnlockInlaidAugment)]
        public static void HandleEffectUnlockInlaidAugment(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            ushort unlockId = (ushort)(info.Entry.DataBits00 != 0u ? info.Entry.DataBits00 : info.Entry.DataBits01);
            if (unlockId == 0u)
                return;

            player.Account.GenericUnlockManager.Unlock(unlockId);
        }

        [SpellEffectHandler(SpellEffectType.UnlockPetFlair)]
        public static void HandleEffectUnlockPetFlair(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.PetCustomisationManager.UnlockFlair((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.GrantLevelScaledXP)]
        public static void HandleEffectGrantLevelScaledXp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
            {
                XpPerLevelEntry xpEntry = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level);
                amount = xpEntry != null
                    ? Math.Max(1u, (uint)(xpEntry.BaseQuestXpPerLevel * 0.10f))
                    : 1u;
            }

            player.XpManager.GrantXp(amount, Network.World.Message.Static.ExpReason.Cheat);
        }

        [SpellEffectHandler(SpellEffectType.GrantLevelScaledPrestige)]
        public static void HandleEffectGrantLevelScaledPrestige(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
                amount = Math.Max(1u, (uint)player.Level);

            player.CurrencyManager.CurrencyAddAmount(CurrencyType.Prestige, amount, isLoot: true);
        }

        [SpellEffectHandler(SpellEffectType.GrantXP)]
        public static void HandleEffectGrantXp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
                return;

            player.XpManager.GrantXp(amount, Network.World.Message.Static.ExpReason.Cheat);
        }

        [SpellEffectHandler(SpellEffectType.PathXpModify)]
        public static void HandleEffectPathXpModify(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            if (amount == 0u)
                return;

            player.PathManager.AddXp(amount);
        }

        [SpellEffectHandler(SpellEffectType.UnlockActionBar)]
        public static void HandleEffectUnlockActionBar(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.SpellManager.SendServerAbilityPoints();
        }

        [SpellEffectHandler(SpellEffectType.RestedXpDecorBonus)]
        public static void HandleEffectRestedXpDecorBonus(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            int delta = DecodeSignedEffectAmount(info.Entry);
            if (delta == 0)
                delta = 1;

            player.XpManager.ModifyRestBonusXp(delta);
        }

        [SpellEffectHandler(SpellEffectType.SetMatchingEligibility)]
        public static void HandleEffectSetMatchingEligibility(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint flags = info.Entry.DataBits00;
            if (flags == 0u || flags == uint.MaxValue)
                flags = info.Entry.DataBits01;
            if (flags == uint.MaxValue)
                flags = 0u;

            player.Session.EnqueueMessageEncrypted(new ServerMatchingEligibilityChanged
            {
                MatchingEligibilityFlags = flags
            });
        }

        [SpellEffectHandler(SpellEffectType.ChangePlane)]
        public static void HandleEffectChangePlane(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint planesCanSee = info.Entry.DataBits00;
            uint planesCanBeSeen = info.Entry.DataBits01;

            if (planesCanSee == 0u || planesCanSee == uint.MaxValue)
                planesCanSee = planesCanBeSeen != 0u && planesCanBeSeen != uint.MaxValue ? planesCanBeSeen : 1u;

            if (planesCanBeSeen == 0u || planesCanBeSeen == uint.MaxValue)
                planesCanBeSeen = planesCanSee;

            player.Session.EnqueueMessageEncrypted(new ServerPlaneVisibilitySpellSlingerVoid
            {
                PlanesCanSee    = planesCanSee,
                PlanesCanBeSeen = planesCanBeSeen
            });
        }

        [SpellEffectHandler(SpellEffectType.SharedHealthPool)]
        public static void HandleEffectSharedHealthPool(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<IUnitEntity> members = ResolveSharedHealthPoolMembers(spell);
            if (members.Count < 2)
                return;

            void sync()
            {
                // Baseline: maintain a shared health percentage across linked targets.
                // This approximates retail pooled-health behavior closely enough for encounter scripting.
                float lowestPercent = members
                    .Where(u => u.IsAlive && u.MaxHealth > 0u)
                    .Select(u => u.Health / (float)u.MaxHealth)
                    .DefaultIfEmpty(1f)
                    .Min();

                foreach (IUnitEntity member in members)
                {
                    if (!member.IsAlive || member.MaxHealth == 0u)
                        continue;

                    uint desiredHealth = (uint)Math.Round(member.MaxHealth * lowestPercent);
                    desiredHealth = Math.Min(desiredHealth, member.MaxHealth);

                    if (member.Health > desiredHealth)
                    {
                        uint damage = member.Health - desiredHealth;
                        member.ModifyHealth(damage, DamageType.Physical, spell.Caster);
                    }
                    else if (member.Health < desiredHealth)
                    {
                        uint heal = desiredHealth - member.Health;
                        member.ModifyHealth(heal, DamageType.Heal, spell.Caster);
                    }
                }
            }

            sync();

            uint durationMs = info.Entry.DurationTime;
            if (durationMs == 0u)
                return;

            const double syncTickSeconds = 0.20d;
            foreach (UnitEntity member in members.OfType<UnitEntity>())
            {
                uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
                uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
                uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
                member.AddTimedAura(
                    spell.Parameters.SpellInfo.Entry.Id,
                    info.Entry.EffectType,
                    spell.Caster.Guid,
                    durationMs / 1000d,
                    syncTickSeconds,
                    onTick: sync,
                    stackGroupId: stackGroupId,
                    stackCap: stackCap,
                    stackTypeEnum: stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
            }
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

            // NOTE: static handlers resolve entity creation through the legacy provider.
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

        [SpellEffectHandler(SpellEffectType.SummonPet)]
        public static void HandleEffectSummonPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint creatureId = info.Entry.DataBits00;
            if (creatureId == 0u)
                return;

            // Replace existing summoned pet entity for deterministic spell-driven pet visuals.
            if (player.VanityPetGuid != null)
            {
                IPetEntity oldPet = player.GetVisible<IPetEntity>(player.VanityPetGuid.Value);
                oldPet?.RemoveFromMap();
                player.VanityPetGuid = 0u;
            }

            var factory = LegacyServiceProvider.Provider.GetService<IEntityFactory>();
            var pet = factory.CreateEntity<IPetEntity>();
            pet.Initialise(player, creatureId);

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(pet, position))
                player.Map.EnqueueAdd(pet, position);
        }

        [SpellEffectHandler(SpellEffectType.PetCastSpell)]
        public static void HandleEffectPetCastSpell(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint spellId = info.Entry.DataBits00;
            if (spellId == 0u || player.VanityPetGuid == null)
                return;

            IPetEntity pet = player.GetVisible<IPetEntity>(player.VanityPetGuid.Value);
            if (pet is not IUnitEntity petUnit)
                return;

            petUnit.CastSpell(spellId, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false,
                PrimaryTargetId        = player.Guid
            });
        }

        [SpellEffectHandler(SpellEffectType.TitleGrant)]
        public static void HandleEffectTitleGrant(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.TitleManager.AddTitle((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.TitleRevoke)]
        public static void HandleEffectTitleRevoke(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.TitleManager.RevokeTitle((ushort)info.Entry.DataBits00);
        }

        [SpellEffectHandler(SpellEffectType.VendorPriceModifier)]
        public static void HandleEffectVendorPriceModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectPropertyModifier(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.ApplyLASChanges)]
        public static void HandleEffectApplyLasChanges(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.SpellManager.SendServerAbilityPoints();
        }

        [SpellEffectHandler(SpellEffectType.GiveAbilityPointsToPlayer)]
        public static void HandleEffectGiveAbilityPointsToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.SpellManager.SendServerAbilityPoints();
        }

        [SpellEffectHandler(SpellEffectType.PathActionExplorerDig)]
        public static void HandleEffectPathActionExplorerDig(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            player.PathManager.AddXp(Math.Max(1u, amount));
        }

        [SpellEffectHandler(SpellEffectType.ModifyAbilityCharges)]
        public static void HandleEffectModifyAbilityCharges(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint targetSpellId = info.Entry.DataBits00 != 0u
                ? info.Entry.DataBits00
                : spell.Parameters.SpellInfo.Entry.Id;

            if (!TryResolveCharacterSpell(player, targetSpellId, out ICharacterSpell characterSpell))
                return;

            int delta = DecodeSignedEffectAmount(info.Entry);
            if (delta == 0 && info.Entry.DataBits01 != 0u)
                delta = unchecked((int)info.Entry.DataBits01);
            if (delta == 0)
                return;

            characterSpell.ModifyCharges(delta);
        }

        [SpellEffectHandler(SpellEffectType.SettlerCampfire)]
        public static void HandleEffectSettlerCampfire(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (info.Entry.DurationTime == 0u)
                return;

            // Keep spell execution alive during campfire runtime.
            spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => { });
        }

        [SpellEffectHandler(SpellEffectType.RewardBuffModifier)]
        public static void HandleEffectRewardBuffModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectPropertyModifier(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.HousingEscape)]
        public static void HandleEffectHousingEscape(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectHousingTeleport(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.NPCForceAIMovement)]
        public static void HandleEffectNpcForceAiMovement(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectForcedMove(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.PathMissionIncrement)]
        public static void HandleEffectPathMissionIncrement(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint amount = DecodeUnsignedEffectAmount(info.Entry);
            player.PathManager.AddXp(Math.Max(1u, amount));
        }

        [SpellEffectHandler(SpellEffectType.WarplotPlugUpgrade)]
        public static void HandleEffectWarplotPlugUpgrade(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            ushort unlockId = (ushort)(info.Entry.DataBits00 != 0u ? info.Entry.DataBits00 : info.Entry.DataBits01);
            if (unlockId == 0u)
                return;

            player.Account.GenericUnlockManager.Unlock(unlockId);
        }

        [SpellEffectHandler(SpellEffectType.GiveAugmentPowerToPlayer)]
        public static void HandleEffectGiveAugmentPowerToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint essenceId = info.Entry.DataBits00 != 0u ? info.Entry.DataBits00 : info.Entry.DataBits01;
            uint amount = info.Entry.DataBits01 != 0u && essenceId == info.Entry.DataBits00
                ? info.Entry.DataBits01
                : Math.Max(1u, info.Entry.DataBits02);
            if (essenceId == 0u)
                return;

            player.PrimalMatrixManager.AddEssence(essenceId, amount);
        }

        [SpellEffectHandler(SpellEffectType.TemporarilyUnflagPvp)]
        public static void HandleEffectTemporarilyUnflagPvp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // PvP flagging is map/matchmaking-owned in current stack; keep cast timing semantics only.
            if (info.Entry.DurationTime > 0u)
                spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => { });
        }

        [SpellEffectHandler(SpellEffectType.SupportStuck)]
        public static void HandleEffectSupportStuck(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectTeleport(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.MiniMapIcon)]
        public static void HandleEffectMiniMapIcon(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (info.Entry.DurationTime == 0u)
                return;

            spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => { });
        }

        [SpellEffectHandler(SpellEffectType.ModifyRestedXP)]
        public static void HandleEffectModifyRestedXp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            int delta = DecodeSignedEffectAmount(info.Entry);
            if (delta == 0)
                return;

            player.XpManager.ModifyRestBonusXp(delta);
        }

        [SpellEffectHandler(SpellEffectType.HousingPlantSeed)]
        public static void HandleEffectHousingPlantSeed(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint itemId = info.Entry.DataBits00;
            if (itemId == 0u)
                itemId = info.Entry.DataBits01;
            if (itemId == 0u || GameTableManager.Instance.Item.GetEntry(itemId) == null)
                return;

            uint count = info.Entry.DataBits02 == 0u ? 1u : info.Entry.DataBits02;
            player.Inventory.ItemCreate(InventoryLocation.Inventory, itemId, count, ItemUpdateReason.Loot);
        }

        [SpellEffectHandler(SpellEffectType.VacuumLoot)]
        public static void HandleEffectVacuumLoot(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectGiveLootTableToPlayer(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.NpcLootTableModify)]
        public static void HandleEffectNpcLootTableModify(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectGiveLootTableToPlayer(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.WarplotTeleport)]
        public static void HandleEffectWarplotTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectTeleport(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.UnitPropertyConversion)]
        public static void HandleEffectUnitPropertyConversion(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectPropertyModifier(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.SpellCounter)]
        public static void HandleEffectSpellCounter(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            uint spellFilterId = candidates.FirstOrDefault(id => id != spell.Parameters.SpellInfo.Entry.Id);
            ISpell activeSpell = target.GetActiveSpell(s =>
                s.IsCasting && (spellFilterId == 0u || s.Parameters.SpellInfo.Entry.Id == spellFilterId));
            if (activeSpell == null)
                return;

            target.CancelSpellCast(activeSpell.CastingId, Network.World.Message.Static.CastResult.SpellInterrupted);
        }

        [SpellEffectHandler(SpellEffectType.ForceFacing)]
        public static void HandleEffectForceFacing(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null || spell.Caster == null || target.Guid == spell.Caster.Guid)
                return;

            target.MovementManager.SetRotationFaceUnit(spell.Caster.Guid);

            if (target is not UnitEntity unitEntity || info.Entry.DurationTime == 0u)
                return;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddTimedAura(
                spell.Parameters.SpellInfo.Entry.Id,
                info.Entry.EffectType,
                spell.Caster.Guid,
                info.Entry.DurationTime / 1000d,
                0.25d,
                onTick: () => target.MovementManager.SetRotationFaceUnit(spell.Caster.Guid),
                onRemove: () => target.MovementManager.SetRotationDefaults(),
                stackGroupId: stackGroupId,
                stackCap: stackCap,
                stackTypeEnum: stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.NpcForceFacing)]
        public static void HandleEffectNpcForceFacing(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectForceFacing(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.VectorSlide)]
        public static void HandleEffectVectorSlide(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Approximation: most observed VectorSlide payloads align with ForcedMove semantics.
            HandleEffectForcedMove(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.Stealth)]
        public static void HandleEffectStealth(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            info.AddCombatLog(new CombatLogStealth
            {
                UnitId = target.Guid,
                BExiting = false
            });

            if (target is not UnitEntity unitEntity || info.Entry.DurationTime == 0u)
                return;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddTimedAura(
                spell.Parameters.SpellInfo.Entry.Id,
                SpellEffectType.Stealth,
                spell.Caster.Guid,
                info.Entry.DurationTime / 1000d,
                0d,
                onRemove: () => spell.Caster.EnqueueToVisible(new ServerCombatLog
                {
                    CombatLog = new CombatLogStealth
                    {
                        UnitId = target.Guid,
                        BExiting = true
                    }
                }, true),
                stackGroupId: stackGroupId,
                stackCap: stackCap,
                stackTypeEnum: stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.RemoveStealth)]
        public static void HandleEffectRemoveStealth(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            if (target is UnitEntity unitEntity)
                unitEntity.RemoveTimedAurasByEffectType(SpellEffectType.Stealth);

            info.AddCombatLog(new CombatLogStealth
            {
                UnitId = target.Guid,
                BExiting = true
            });
        }

        [SpellEffectHandler(SpellEffectType.AggroImmune)]
        public static void HandleEffectAggroImmune(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not UnitEntity unitEntity)
                return;

            void clearThreat() => target.ThreatManager.ClearThreatList();
            clearThreat();

            if (info.Entry.DurationTime == 0u)
                return;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddTimedAura(
                spell.Parameters.SpellInfo.Entry.Id,
                SpellEffectType.AggroImmune,
                spell.Caster.Guid,
                info.Entry.DurationTime / 1000d,
                0.25d,
                onTick: clearThreat,
                stackGroupId: stackGroupId,
                stackCap: stackCap,
                stackTypeEnum: stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.SpellImmunity)]
        public static void HandleEffectSpellImmunity(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not UnitEntity unitEntity)
                return;

            uint immuneSpellId = info.Entry.DataBits00 != 0u
                ? info.Entry.DataBits00
                : info.Entry.DataBits01;
            if (immuneSpellId == 0u)
                return;

            double durationSeconds = info.Entry.DurationTime > 0u
                ? info.Entry.DurationTime / 1000d
                : 15d;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddSpellImmunity(
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Caster.Guid,
                immuneSpellId,
                durationSeconds,
                stackGroupId,
                stackCap,
                stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.SpellEffectImmunity)]
        public static void HandleEffectSpellEffectImmunity(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not UnitEntity unitEntity)
                return;

            uint rawEffectType = info.Entry.DataBits00 != 0u
                ? info.Entry.DataBits00
                : info.Entry.DataBits01;
            if (!Enum.IsDefined(typeof(SpellEffectType), (int)rawEffectType))
                return;

            double durationSeconds = info.Entry.DurationTime > 0u
                ? info.Entry.DurationTime / 1000d
                : 15d;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddSpellEffectImmunity(
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Caster.Guid,
                (SpellEffectType)rawEffectType,
                durationSeconds,
                stackGroupId,
                stackCap,
                stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.ReputationModify)]
        public static void HandleEffectReputationModify(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            Faction faction = (Faction)info.Entry.DataBits00;
            if (FactionManager.Instance.GetFaction(faction) == null)
                return;

            int amount = DecodeSignedEffectAmount(info.Entry);
            if (amount == 0)
                return;

            player.ReputationManager.UpdateReputation(faction, amount);
        }

        [SpellEffectHandler(SpellEffectType.AchievementAdvance)]
        public static void HandleEffectAchievementAdvance(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint achievementId = info.Entry.DataBits00;
            if (achievementId == 0u || achievementId > ushort.MaxValue)
                return;

            player.AchievementManager.GrantAchievement((ushort)achievementId);
        }

        [SpellEffectHandler(SpellEffectType.RavelSignal)]
        public static void HandleEffectRavelSignal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IUnitEntity subject = target ?? spell.Caster;
            if (subject == null)
                return;

            uint mode = info.Entry.DataBits00;
            List<uint> linkedSpellIds = ResolveRavelSignalLinkedSpellIds(info.Entry);
            if (linkedSpellIds.Count == 0)
                return;

            bool isRemoveMode = mode is 4u or 5u;
            uint delayMs = info.Entry.DataBits02;
            if (delayMs == uint.MaxValue)
                delayMs = 0u;

            Action apply = () =>
            {
                foreach (uint linkedSpellId in linkedSpellIds)
                {
                    if (isRemoveMode)
                    {
                        if (subject is UnitEntity unitEntity)
                            unitEntity.RemoveTimedAurasBySpellId(linkedSpellId);

                        subject.RemoveSpellProperties(linkedSpellId);
                        ISpell activeSpell = subject.GetActiveSpell(s => s.Parameters.SpellInfo.Entry.Id == linkedSpellId);
                        if (activeSpell?.IsCasting == true)
                            subject.CancelSpellCast(activeSpell.CastingId, Network.World.Message.Static.CastResult.SpellCancelled);
                        continue;
                    }

                    subject.CastSpell(linkedSpellId, new SpellParameters
                    {
                        ParentSpellInfo        = spell.Parameters.SpellInfo,
                        RootSpellInfo          = spell.Parameters.RootSpellInfo,
                        UserInitiatedSpellCast = false,
                        PrimaryTargetId        = target?.Guid ?? spell.Caster.Guid
                    });
                }
            };

            if (delayMs > 0u)
            {
                spell.EnqueueEvent(delayMs / 1000d, apply);
                return;
            }

            apply();
        }

        [SpellEffectHandler(SpellEffectType.FacilityModification)]
        public static void HandleEffectFacilityModification(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IUnitEntity subject = target ?? spell.Caster;
            if (subject == null)
                return;

            uint linkedSpellId = info.Entry.DataBits00;
            if (linkedSpellId == 0u || GameTableManager.Instance.Spell4.GetEntry(linkedSpellId) == null)
                return;

            uint mode = info.Entry.DataBits02;
            bool isRemoveMode = mode is 2u or 3u;
            uint delayMs = info.Entry.TickTime;
            if (delayMs == uint.MaxValue)
                delayMs = 0u;

            Action apply = () =>
            {
                if (isRemoveMode)
                {
                    if (subject is UnitEntity unitEntity)
                        unitEntity.RemoveTimedAurasBySpellId(linkedSpellId);

                    subject.RemoveSpellProperties(linkedSpellId);
                    ISpell activeSpell = subject.GetActiveSpell(s => s.Parameters.SpellInfo.Entry.Id == linkedSpellId);
                    if (activeSpell?.IsCasting == true)
                        subject.CancelSpellCast(activeSpell.CastingId, Network.World.Message.Static.CastResult.SpellCancelled);
                    return;
                }

                subject.CastSpell(linkedSpellId, new SpellParameters
                {
                    ParentSpellInfo        = spell.Parameters.SpellInfo,
                    RootSpellInfo          = spell.Parameters.RootSpellInfo,
                    UserInitiatedSpellCast = false,
                    PrimaryTargetId        = target?.Guid ?? spell.Caster.Guid
                });

                if (info.Entry.DurationTime > 0u)
                {
                    spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () =>
                    {
                        if (subject is UnitEntity unitEntity)
                            unitEntity.RemoveTimedAurasBySpellId(linkedSpellId);
                        subject.RemoveSpellProperties(linkedSpellId);
                    });
                }
            };

            if (delayMs > 0u)
            {
                spell.EnqueueEvent(delayMs / 1000d, apply);
                return;
            }

            apply();
        }

        [SpellEffectHandler(SpellEffectType.Script)]
        public static void HandleEffectScript(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Most Script rows are payload-empty and rely on script collection side effects.
            // For rows with a concrete spell id payload, execute that linked spell.
            IUnitEntity subject = target ?? spell.Caster;
            if (subject == null)
                return;

            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            uint linkedSpellId = candidates.FirstOrDefault(id => id != spell.Parameters.SpellInfo.Entry.Id);
            if (linkedSpellId == 0u)
                return;

            subject.CastSpell(linkedSpellId, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                UserInitiatedSpellCast = false,
                PrimaryTargetId        = target?.Guid ?? spell.Caster.Guid
            });
        }

        [SpellEffectHandler(SpellEffectType.ModifyCreatureFlags)]
        public static void HandleEffectModifyCreatureFlags(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null || info.Entry.DataBits00 == 0u)
                return;

            EntityCreateFlag flags = (EntityCreateFlag)info.Entry.DataBits00;
            EntityCreateFlag previousFlags = target.CreateFlags;
            bool alreadySet = (previousFlags & flags) == flags;

            Action apply = () => target.CreateFlags |= flags;
            Action remove = () =>
            {
                if (!alreadySet)
                    target.CreateFlags &= ~flags;
            };

            if (info.Entry.DurationTime > 0u && target is UnitEntity unitEntity)
            {
                uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
                uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
                uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
                unitEntity.AddTimedAura(
                    spell.Parameters.SpellInfo.Entry.Id,
                    info.Entry.EffectType,
                    spell.Caster.Guid,
                    info.Entry.DurationTime / 1000d,
                    0d,
                    onApply: apply,
                    onRemove: remove,
                    stackGroupId: stackGroupId,
                    stackCap: stackCap,
                    stackTypeEnum: stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                return;
            }

            apply();
        }

        [SpellEffectHandler(SpellEffectType.ShieldOverload)]
        public static void HandleEffectShieldOverload(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null || target.Shield == 0u)
                return;

            target.Shield = 0u;
        }

        [SpellEffectHandler(SpellEffectType.HazardEnable)]
        public static void HandleEffectHazardEnable(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!TryResolveHazardRecipient(spell, target, out IPlayer recipient))
                return;

            if (!TryResolveHazardId(info.Entry, out uint hazardId))
                return;

            recipient.Session.EnqueueMessageEncrypted(new ServerHazardAction
            {
                HazardId = hazardId,
                Action   = HazardAction.Enable
            });

            if (info.Entry.DurationTime > 0u)
            {
                spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () =>
                {
                    recipient.Session.EnqueueMessageEncrypted(new ServerHazardAction
                    {
                        HazardId = hazardId,
                        Action   = HazardAction.Remove
                    });
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.HazardModify)]
        public static void HandleEffectHazardModify(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!TryResolveHazardRecipient(spell, target, out IPlayer recipient))
                return;

            if (!TryResolveHazardModifier(info.Entry, out HazardModifier modifier))
                return;

            var payload = new ServerHazardModifiers();
            if (TryResolveHazardId(info.Entry, out uint hazardId))
            {
                EnsureHazardModifierCapacity(payload.HazardIdModifiers, (int)hazardId + 1);
                payload.HazardIdModifiers[(int)hazardId] = modifier;
            }
            else if (TryResolveHazardType(info.Entry, out HazardType hazardType))
            {
                EnsureHazardModifierCapacity(payload.HazardTypeModifiers, (int)hazardType + 1);
                payload.HazardTypeModifiers[(int)hazardType] = modifier;
            }
            else
            {
                payload.HazardTypeModifiers.Add(modifier);
            }

            recipient.Session.EnqueueMessageEncrypted(payload);
        }

        [SpellEffectHandler(SpellEffectType.HazardSuspend)]
        public static void HandleEffectHazardSuspend(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!TryResolveHazardRecipient(spell, target, out IPlayer recipient))
                return;

            var modifier = new HazardModifier
            {
                Multiplier = 1f,
                Offset     = 0f,
                Suspended  = true
            };

            var payload = new ServerHazardModifiers();
            if (TryResolveHazardId(info.Entry, out uint hazardId))
            {
                EnsureHazardModifierCapacity(payload.HazardIdModifiers, (int)hazardId + 1);
                payload.HazardIdModifiers[(int)hazardId] = modifier;
            }
            else if (TryResolveHazardType(info.Entry, out HazardType hazardType))
            {
                EnsureHazardModifierCapacity(payload.HazardTypeModifiers, (int)hazardType + 1);
                payload.HazardTypeModifiers[(int)hazardType] = modifier;
            }
            else
            {
                payload.HazardTypeModifiers.Add(modifier);
            }

            recipient.Session.EnqueueMessageEncrypted(payload);
        }

        [SpellEffectHandler(SpellEffectType.MimicDisguise)]
        public static void HandleEffectMimicDisguise(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDisguiseOutfit(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.MimicDisplayName)]
        public static void HandleEffectMimicDisplayName(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Placeholder: display-name mutation is currently client/localized-data driven.
        }

        [SpellEffectHandler(SpellEffectType.ChangeDisplayName)]
        public static void HandleEffectChangeDisplayName(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Placeholder: display-name mutation is currently client/localized-data driven.
        }

        [SpellEffectHandler(SpellEffectType.ChangeIcon)]
        public static void HandleEffectChangeIcon(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Placeholder: icon mutation is a UI concern in current stack.
        }

        [SpellEffectHandler(SpellEffectType.AddSpellEffect)]
        public static void HandleEffectAddSpellEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IUnitEntity activationSource = target ?? spell.Caster;
            if (activationSource == null || !activationSource.IsAlive)
                return;

            // Data-driven baseline: treat payload as one or more spell activations.
            // This mirrors retail "graft effect via extra spell rows" behavior well enough for gameplay.
            List<uint> candidates = ResolveProxyCandidateSpellIds(info.Entry);
            foreach (uint candidateSpellId in candidates)
            {
                if (candidateSpellId == 0u || candidateSpellId == spell.Parameters.SpellInfo.Entry.Id)
                    continue;
                if (GameTableManager.Instance.Spell4.GetEntry(candidateSpellId) == null)
                    continue;

                activationSource.CastSpell(candidateSpellId, new SpellParameters
                {
                    ParentSpellInfo        = spell.Parameters.SpellInfo,
                    RootSpellInfo          = spell.Parameters.RootSpellInfo,
                    UserInitiatedSpellCast = false,
                    PrimaryTargetId        = target?.Guid ?? spell.Caster.Guid
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.SuppressSpellEffect)]
        public static void HandleEffectSuppressSpellEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not UnitEntity unitEntity)
                return;

            uint rawPrimary = info.Entry.DataBits00;
            uint rawSecondary = info.Entry.DataBits01;
            uint rawDuration = info.Entry.DurationTime != 0u
                ? info.Entry.DurationTime
                : info.Entry.DataBits02;

            double durationSeconds = rawDuration > 0u
                ? Math.Min(3600d, rawDuration / 1000d)
                : 15d;

            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;

            bool applied = false;

            void applySpellSuppression(uint candidateSpellId)
            {
                if (candidateSpellId == 0u || GameTableManager.Instance.Spell4.GetEntry(candidateSpellId) == null)
                    return;

                unitEntity.AddSpellImmunity(
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.Caster.Guid,
                    candidateSpellId,
                    durationSeconds,
                    stackGroupId,
                    stackCap,
                    stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                applied = true;
            }

            void applyEffectSuppression(uint candidateEffectType)
            {
                if (!Enum.IsDefined(typeof(SpellEffectType), (int)candidateEffectType))
                    return;

                unitEntity.AddSpellEffectImmunity(
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.Caster.Guid,
                    (SpellEffectType)candidateEffectType,
                    durationSeconds,
                    stackGroupId,
                    stackCap,
                    stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                applied = true;
            }

            applyEffectSuppression(rawPrimary);
            applySpellSuppression(rawPrimary);
            applyEffectSuppression(rawSecondary);
            applySpellSuppression(rawSecondary);

            if (!applied && rawPrimary == 0u && rawSecondary == 0u)
                unitEntity.AddSpellEffectImmunity(
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.Caster.Guid,
                    SpellEffectType.Damage,
                    durationSeconds,
                    stackGroupId,
                    stackCap,
                    stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.GiveLootTableToPlayer)]
        public static void HandleEffectGiveLootTableToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            // Baseline fallback: treat payload as direct item grant when it resolves to a valid item id.
            uint candidateItemId = info.Entry.DataBits00;
            if (GameTableManager.Instance.Item.GetEntry(candidateItemId) == null)
                candidateItemId = info.Entry.DataBits01;

            if (candidateItemId == 0u || GameTableManager.Instance.Item.GetEntry(candidateItemId) == null)
                return;

            uint count = info.Entry.DataBits02 == 0u ? 1u : info.Entry.DataBits02;
            player.Inventory.ItemCreate(InventoryLocation.Inventory, candidateItemId, count, ItemUpdateReason.Loot);
        }

        [SpellEffectHandler(SpellEffectType.RewardPropertyModifier)]
        public static void HandleEffectRewardPropertyModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Baseline: reward-property modifiers share duration/stack behavior with generic unit property modifiers.
            HandleEffectPropertyModifier(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.PersonalDmgHealMod)]
        public static void HandleEffectPersonalDmgHealMod(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Baseline: model this as a unit property modifier until full retail semantics are reconstructed.
            HandleEffectPropertyModifier(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.Fluff)]
        public static void HandleEffectFluff(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            List<uint> linkedSpellIds = ResolveProxyCandidateSpellIds(info.Entry);
            if (linkedSpellIds.Count > 0)
            {
                uint linkedSpellId = linkedSpellIds[0];
                if (linkedSpellId != spell.Parameters.SpellInfo.Entry.Id)
                {
                    IUnitEntity castSource = target ?? spell.Caster;
                    castSource.CastSpell(linkedSpellId, new SpellParameters
                    {
                        ParentSpellInfo        = spell.Parameters.SpellInfo,
                        RootSpellInfo          = spell.Parameters.RootSpellInfo,
                        UserInitiatedSpellCast = false
                    });
                }
            }

            if (info.Entry.DurationTime > 0u)
                spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => { });
        }

        [SpellEffectHandler(SpellEffectType.Scale)]
        public static void HandleEffectScale(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target == null)
                return;

            if (!TryDecodePositiveScale(info.Entry.DataBits00, out float scaleValue))
                return;

            double durationSeconds = info.Entry.DurationTime / 1000d;
            if (durationSeconds <= 0d || target is not UnitEntity unitEntity)
            {
                target.MovementManager.SetScale(scaleValue);
                return;
            }

            float originalScale = target.MovementManager.GetScale();
            uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
            uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
            uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
            unitEntity.AddTimedAura(
                spell.Parameters.SpellInfo.Entry.Id,
                info.Entry.EffectType,
                spell.Caster.Guid,
                durationSeconds,
                0d,
                onApply: () => target.MovementManager.SetScale(scaleValue),
                onRemove: () => target.MovementManager.SetScale(originalScale),
                stackGroupId: stackGroupId,
                stackCap: stackCap,
                stackTypeEnum: stackTypeEnum,
                isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
        }

        [SpellEffectHandler(SpellEffectType.UnitPropertyModifier)]
        public static void HandleEffectPropertyModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // NOTE: property modifiers are cheap value objects; construct per effect instance.
            SpellPropertyModifier modifier = 
                new SpellPropertyModifier((Property)info.Entry.DataBits00, 
                    info.Entry.DataBits01, 
                    DecodeFlexibleEffectNumber(info.Entry.DataBits02), 
                    DecodeFlexibleEffectNumber(info.Entry.DataBits03), 
                    DecodeFlexibleEffectNumber(info.Entry.DataBits04));
            uint modifierInstanceId = spell.CastingId;
            target.AddSpellModifierProperty(modifier, modifierInstanceId);

            if (info.Entry.DurationTime > 0u)
            {
                Property property = (Property)info.Entry.DataBits00;
                if (target is UnitEntity unitEntity)
                {
                    uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
                    uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
                    uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
                    unitEntity.AddTimedAura(
                        spell.Parameters.SpellInfo.Entry.Id,
                        SpellEffectType.UnitPropertyModifier,
                        spell.Caster.Guid,
                        info.Entry.DurationTime / 1000d,
                        0d,
                        onRemove: () => target.RemoveSpellProperty(property, modifierInstanceId),
                        stackGroupId: stackGroupId,
                        stackCap: stackCap,
                        stackTypeEnum: stackTypeEnum,
                        isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                        isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                        isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                }
                else
                {
                    spell.EnqueueEvent(info.Entry.DurationTime / 1000d, () => target.RemoveSpellProperty(property, modifierInstanceId));
                }
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

            // Split damage across effect targets while preserving total.
            // Apply split to pre-mitigation values (RawDamage, RawScaledDamage) separately from
            // post-mitigation values (AdjustedDamage, AbsorbedAmount, ShieldAbsorbAmount, GlanceAmount)
            // to more accurately represent damage distribution through the mitigation pipeline.

            // Split pre-mitigation damage components
            damage.RawDamage = DivideByCountWithRemainder(damage.RawDamage, splitCount, splitRank);
            damage.RawScaledDamage = DivideByCountWithRemainder(damage.RawScaledDamage, splitCount, splitRank);

            // Split post-mitigation damage components
            // These represent the actual damage dealt after all mitigation (armor, shields, etc.)
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

        private static void SchedulePeriodicTicks(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, Action<ISpellTargetEffectInfo> tickHandler)
        {
            if (target == null || tickHandler == null)
                return;

            uint tickMs = info.Entry.TickTime;
            uint durationMs = info.Entry.DurationTime;
            if (tickMs == 0u || durationMs == 0u || durationMs < tickMs)
                return;

            if (target is UnitEntity unitEntity)
            {
                uint stackGroupId = spell.Parameters.SpellInfo.StackGroup?.Id ?? 0u;
                uint stackCap = spell.Parameters.SpellInfo.StackGroup?.StackCap ?? 0u;
                uint stackTypeEnum = spell.Parameters.SpellInfo.StackGroup?.StackTypeEnum ?? 0u;
                unitEntity.AddTimedAura(
                    spell.Parameters.SpellInfo.Entry.Id,
                    info.Entry.EffectType,
                    spell.Caster.Guid,
                    durationMs / 1000d,
                    tickMs / 1000d,
                    onTick: () =>
                    {
                        if (!target.IsAlive)
                            return;
                        if (!MeetsEffectPersistencePrerequisites(spell, target, info.Entry))
                            return;
                        if (IsEffectSuspendedForTarget(target, info.Entry))
                            return;

                        var tickInfo = new SpellTargetInfo.SpellTargetEffectInfo(GlobalSpellManager.Instance.NextEffectId, info.Entry);
                        tickHandler.Invoke(tickInfo);
                        EmitCombatLogs(spell, tickInfo.CombatLogs);
                    },
                    stackGroupId: stackGroupId,
                    stackCap: stackCap,
                    stackTypeEnum: stackTypeEnum,
                    isDispellable: spell.Parameters.SpellInfo.BaseInfo.IsDispellable,
                    isDebuff: spell.Parameters.SpellInfo.BaseInfo.IsDebuff,
                    isBuff: spell.Parameters.SpellInfo.BaseInfo.IsBuff);
                return;
            }

            uint tickCount = durationMs / tickMs;
            for (uint i = 1u; i <= tickCount; i++)
            {
                double delay = (tickMs * i) / 1000d;
                spell.EnqueueEvent(delay, () =>
                {
                    if (!target.IsAlive)
                        return;
                    if (!MeetsEffectPersistencePrerequisites(spell, target, info.Entry))
                            return;
                    if (IsEffectSuspendedForTarget(target, info.Entry))
                        return;

                    var tickInfo = new SpellTargetInfo.SpellTargetEffectInfo(GlobalSpellManager.Instance.NextEffectId, info.Entry);
                    tickHandler.Invoke(tickInfo);
                    EmitCombatLogs(spell, tickInfo.CombatLogs);
                });
            }
        }

        private static void EmitCombatLogs(ISpell spell, IEnumerable<ICombatLog> logs)
        {
            if (logs == null)
                return;

            foreach (ICombatLog combatLog in logs)
            {
                spell.Caster.EnqueueToVisible(new ServerCombatLog
                {
                    CombatLog = combatLog
                }, true);
            }
        }

        private static bool TryResolveHazardRecipient(ISpell spell, IUnitEntity target, out IPlayer recipient)
        {
            recipient = target as IPlayer ?? spell.Caster as IPlayer;
            return recipient != null;
        }

        private static bool TryResolveCharacterSpell(IPlayer player, uint rawSpellId, out ICharacterSpell characterSpell)
        {
            characterSpell = null;
            if (rawSpellId == 0u)
                return false;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(rawSpellId);
            if (spell4Entry != null)
            {
                characterSpell = player.SpellManager.GetSpell(spell4Entry.Spell4BaseIdBaseSpell);
                if (characterSpell != null)
                    return true;
            }

            Spell4BaseEntry spell4BaseEntry = GameTableManager.Instance.Spell4Base.GetEntry(rawSpellId);
            if (spell4BaseEntry != null)
            {
                characterSpell = player.SpellManager.GetSpell(spell4BaseEntry.Id);
                return characterSpell != null;
            }

            characterSpell = player.SpellManager.GetSpell(rawSpellId);
            return characterSpell != null;
        }

        private static bool TryResolveHazardId(Spell4EffectsEntry entry, out uint hazardId)
        {
            hazardId = entry.DataBits00;
            if (hazardId == 0u || hazardId > 16383u)
                hazardId = entry.DataBits01;

            return hazardId is > 0u and <= 16383u;
        }

        private static bool TryResolveHazardType(Spell4EffectsEntry entry, out HazardType hazardType)
        {
            if (Enum.IsDefined(typeof(HazardType), (int)entry.DataBits00))
            {
                hazardType = (HazardType)entry.DataBits00;
                return true;
            }

            if (Enum.IsDefined(typeof(HazardType), (int)entry.DataBits01))
            {
                hazardType = (HazardType)entry.DataBits01;
                return true;
            }

            hazardType = default;
            return false;
        }

        private static bool TryResolveHazardModifier(Spell4EffectsEntry entry, out HazardModifier modifier)
        {
            modifier = new HazardModifier
            {
                Multiplier = 1f,
                Offset     = 0f,
                Suspended  = false
            };

            bool changed = false;

            float multiplier = BitConverter.UInt32BitsToSingle(entry.DataBits02);
            if (float.IsFinite(multiplier) && multiplier > 0f && multiplier < 100f)
            {
                modifier.Multiplier = multiplier;
                changed = true;
            }

            float offset = BitConverter.UInt32BitsToSingle(entry.DataBits03);
            if (float.IsFinite(offset) && Math.Abs(offset) < 100000f)
            {
                modifier.Offset = offset;
                changed = true;
            }

            if (entry.DataBits04 != 0u)
            {
                modifier.Suspended = true;
                changed = true;
            }

            if (!changed)
            {
                for (int i = 0; i < entry.ParameterValue.Length; i++)
                {
                    float value = entry.ParameterValue[i];
                    if (!float.IsFinite(value))
                        continue;

                    if (i == 0 && value > 0f && value < 100f)
                    {
                        modifier.Multiplier = value;
                        changed = true;
                    }
                    else if (i == 1 && Math.Abs(value) < 100000f)
                    {
                        modifier.Offset = value;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static void EnsureHazardModifierCapacity(List<HazardModifier> modifiers, int count)
        {
            while (modifiers.Count < count)
                modifiers.Add(new HazardModifier { Multiplier = 1f, Offset = 0f, Suspended = false });
        }

        private static (bool removeBuffs, bool removeDebuffs) ResolveDispelClassTargets(uint spellClassRaw)
        {
            SpellClass spellClass = (SpellClass)spellClassRaw;
            return spellClass switch
            {
                SpellClass.BuffDispellable => (true, false),
                SpellClass.DebuffDispellable => (false, true),
                _ => (true, true)
            };
        }

        private static uint ResolveDispelInstanceLimit(Spell4EffectsEntry entry)
        {
            uint limit = entry.DataBits00;
            if (limit == 0u || limit == uint.MaxValue)
                return uint.MaxValue;

            return Math.Min(limit, 50u);
        }

        private static double ResolveProcChance(Spell4EffectsEntry entry)
        {
            float chanceAsFloat = BitConverter.UInt32BitsToSingle(entry.DataBits02);
            if (!float.IsNaN(chanceAsFloat) && !float.IsInfinity(chanceAsFloat) && chanceAsFloat > 0f && chanceAsFloat <= 1f)
                return chanceAsFloat;

            if (entry.DataBits02 > 0u && entry.DataBits02 <= 100u)
                return entry.DataBits02 / 100d;

            if (entry.ParameterValue.Length > 0 && entry.ParameterValue[0] > 0f && entry.ParameterValue[0] <= 1f)
                return entry.ParameterValue[0];

            return 1d;
        }

        private static UnitEntity.ProcEventMask ResolveProcEventMask(uint rawMask)
        {
            UnitEntity.ProcEventMask mask = (UnitEntity.ProcEventMask)(rawMask & 0xFu);
            return mask == UnitEntity.ProcEventMask.None
                ? UnitEntity.ProcEventMask.DamageDone
                : mask;
        }

        private static double ResolveProcCooldownSeconds(uint rawValue)
        {
            if (rawValue == 0u || rawValue == uint.MaxValue)
                return 0d;

            if (rawValue <= 100u)
                return rawValue;

            return rawValue / 1000d;
        }

        private static bool TryDecodePositiveScale(uint rawValue, out float scale)
        {
            scale = BitConverter.UInt32BitsToSingle(rawValue);
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                return false;

            if (scale > 20f)
                return false;

            return true;
        }

        private static List<uint> ResolveProxyCandidateSpellIds(Spell4EffectsEntry entry)
        {
            var candidates = new List<uint>(8);

            void add(uint spellId)
            {
                if (spellId == 0u || spellId == uint.MaxValue)
                    return;

                if (IsKnownSpellId(spellId))
                    candidates.Add(spellId);
            }

            add(entry.DataBits00);
            add(entry.DataBits01);
            add(entry.DataBits02);
            add(entry.DataBits03);
            add(entry.DataBits04);

            for (int i = 0; i < entry.ParameterValue.Length; i++)
            {
                float value = entry.ParameterValue[i];
                if (value <= 0f)
                    continue;

                add((uint)Math.Round(value));
            }

            return candidates.Distinct().ToList();
        }

        private static bool IsKnownSpellId(uint spellId)
        {
            if (spellId == 0u || spellId == uint.MaxValue)
                return false;

            return GameTableManager.Instance.Spell4.GetEntry(spellId) != null
                || GameTableManager.Instance.Spell4Base.GetEntry(spellId) != null;
        }

        private static uint ResolveEffectTargetSpellId(Spell4EffectsEntry entry, uint fallbackSpellId)
        {
            if (IsKnownSpellId(entry.DataBits00))
                return entry.DataBits00;

            if (IsKnownSpellId(entry.DataBits01))
                return entry.DataBits01;

            if (IsKnownSpellId(entry.DataBits02))
                return entry.DataBits02;

            if (IsKnownSpellId(entry.DataBits03))
                return entry.DataBits03;

            if (IsKnownSpellId(entry.DataBits04))
                return entry.DataBits04;

            return fallbackSpellId;
        }

        private static double ResolveCooldownDeltaSeconds(Spell4EffectsEntry entry, uint targetSpellId)
        {
            bool dataBits01LooksLikeSpellId = entry.DataBits01 != 0u && IsKnownSpellId(entry.DataBits01);

            if (!dataBits01LooksLikeSpellId && entry.DataBits01 != 0u)
            {
                int rawDelta = unchecked((int)entry.DataBits01);
                return Math.Abs(rawDelta) > 1000 ? rawDelta / 1000d : rawDelta;
            }

            float payloadDelta = DecodeFlexibleEffectNumber(entry.DataBits03);
            if (payloadDelta == 0f && entry.DataBits02 != 0u && entry.DataBits02 != targetSpellId)
                payloadDelta = DecodeFlexibleEffectNumber(entry.DataBits02);

            if (payloadDelta != 0f)
                return Math.Abs(payloadDelta) > 1000f ? payloadDelta / 1000d : payloadDelta;

            for (int i = 0; i < entry.ParameterValue.Length; i++)
            {
                if (entry.ParameterValue[i] == 0f)
                    continue;

                return Math.Abs(entry.ParameterValue[i]) > 1000f
                    ? entry.ParameterValue[i] / 1000d
                    : entry.ParameterValue[i];
            }

            return 0d;
        }

        private static List<uint> ResolveRavelSignalLinkedSpellIds(Spell4EffectsEntry entry)
        {
            var candidates = new List<uint>(6);

            void addIfSpell4(uint spellId)
            {
                if (spellId == 0u || spellId == uint.MaxValue)
                    return;

                if (GameTableManager.Instance.Spell4.GetEntry(spellId) != null)
                    candidates.Add(spellId);
            }

            // Ravel payloads are predominantly mode-driven where DataBits00 is an opcode-like value (1/4/5),
            // and DataBits01+ carry linked spell references when present.
            addIfSpell4(entry.DataBits01);
            addIfSpell4(entry.DataBits02);
            addIfSpell4(entry.DataBits03);
            addIfSpell4(entry.DataBits04);

            if (entry.DataBits00 > 8u)
                addIfSpell4(entry.DataBits00);

            for (int i = 0; i < entry.ParameterValue.Length; i++)
            {
                float value = entry.ParameterValue[i];
                if (value <= 0f)
                    continue;

                addIfSpell4((uint)Math.Round(value));
            }

            return candidates.Distinct().ToList();
        }

        private static bool TryResolveItemVisualSwapSlot(uint rawSlot, out ItemSlot slot)
        {
            if (Enum.IsDefined(typeof(ItemSlot), (int)rawSlot))
            {
                slot = (ItemSlot)rawSlot;
                return true;
            }

            // Data-driven mapping observed in ItemVisualSwap payloads.
            slot = rawSlot switch
            {
                0u => ItemSlot.ArmorChest,
                1u => ItemSlot.ArmorLegs,
                2u => ItemSlot.ArmorHead,
                3u => ItemSlot.ArmorShoulder,
                4u => ItemSlot.ArmorFeet,
                5u => ItemSlot.ArmorHands,
                6u => ItemSlot.WeaponPrimary,
                16u => ItemSlot.WeaponPrimary,
                _ => default
            };

            return slot != default;
        }

        private static bool TryResolveForcedMoveDistance(Spell4EffectsEntry entry, out float distance)
        {
            static bool IsReasonableDistance(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f && value <= 80f;

            float value1 = BitConverter.UInt32BitsToSingle(entry.DataBits01);
            if (IsReasonableDistance(value1))
            {
                distance = value1;
                return true;
            }

            float value2 = BitConverter.UInt32BitsToSingle(entry.DataBits02);
            if (IsReasonableDistance(value2))
            {
                distance = value2;
                return true;
            }

            for (int i = 0; i < entry.ParameterValue.Length; i++)
            {
                if (IsReasonableDistance(entry.ParameterValue[i]))
                {
                    distance = entry.ParameterValue[i];
                    return true;
                }
            }

            distance = 0f;
            return false;
        }

        private static float DecodeFlexibleEffectNumber(uint raw)
        {
            if (raw == 0u)
                return 0f;

            float asFloat = BitConverter.UInt32BitsToSingle(raw);
            if (float.IsFinite(asFloat) && Math.Abs(asFloat) >= 0.0001f && Math.Abs(asFloat) <= 1_000_000f)
                return asFloat;

            int asSigned = unchecked((int)raw);
            if (asSigned is > -1_000_000 and < 1_000_000)
                return asSigned;

            if (raw <= 1_000_000u)
                return raw;

            return asFloat;
        }

        private static Spell4EffectsEntry CloneEffectEntry(Spell4EffectsEntry source)
        {
            var clone = new Spell4EffectsEntry
            {
                Id = source.Id,
                SpellId = source.SpellId,
                TargetFlags = source.TargetFlags,
                EffectType = source.EffectType,
                DamageType = source.DamageType,
                DelayTime = source.DelayTime,
                TickTime = source.TickTime,
                DurationTime = source.DurationTime,
                Flags = source.Flags,
                DataBits00 = source.DataBits00,
                DataBits01 = source.DataBits01,
                DataBits02 = source.DataBits02,
                DataBits03 = source.DataBits03,
                DataBits04 = source.DataBits04,
                DataBits05 = source.DataBits05,
                DataBits06 = source.DataBits06,
                DataBits07 = source.DataBits07,
                DataBits08 = source.DataBits08,
                DataBits09 = source.DataBits09,
                InnateCostPerTickType0 = source.InnateCostPerTickType0,
                InnateCostPerTickType1 = source.InnateCostPerTickType1,
                InnateCostPerTick0 = source.InnateCostPerTick0,
                InnateCostPerTick1 = source.InnateCostPerTick1,
                EmmComparison = source.EmmComparison,
                EmmValue = source.EmmValue,
                ThreatMultiplier = source.ThreatMultiplier,
                Spell4EffectGroupListId = source.Spell4EffectGroupListId,
                PrerequisiteIdCasterApply = source.PrerequisiteIdCasterApply,
                PrerequisiteIdTargetApply = source.PrerequisiteIdTargetApply,
                PrerequisiteIdCasterPersistence = source.PrerequisiteIdCasterPersistence,
                PrerequisiteIdTargetPersistence = source.PrerequisiteIdTargetPersistence,
                PrerequisiteIdTargetSuspend = source.PrerequisiteIdTargetSuspend,
                PhaseFlags = source.PhaseFlags,
                OrderIndex = source.OrderIndex,
                ParameterType = (SpellEffectParameterType[])source.ParameterType.Clone(),
                ParameterValue = (float[])source.ParameterValue.Clone()
            };
            return clone;
        }

        private static uint ResolveForcedMoveTravelMs(Spell4EffectsEntry entry)
        {
            uint raw = entry.DataBits03 != 0u ? entry.DataBits03 : entry.DurationTime;
            if (raw == 0u || raw == uint.MaxValue)
                return 300u;

            return Math.Min(5000u, Math.Max(50u, raw));
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.X)
                   && float.IsFinite(value.Y)
                   && float.IsFinite(value.Z);
        }

        private static bool MeetsEffectPersistencePrerequisites(ISpell spell, IUnitEntity target, Spell4EffectsEntry entry)
        {
            if (spell.Caster is IPlayer casterPlayer
                && entry.PrerequisiteIdCasterPersistence != 0u
                && !PrerequisiteManager.Instance.Meets(casterPlayer, entry.PrerequisiteIdCasterPersistence))
            {
                return false;
            }

            if (target is IPlayer targetPlayer
                && entry.PrerequisiteIdTargetPersistence != 0u
                && !PrerequisiteManager.Instance.Meets(targetPlayer, entry.PrerequisiteIdTargetPersistence))
            {
                return false;
            }

            return true;
        }

        private static bool IsEffectSuspendedForTarget(IUnitEntity target, Spell4EffectsEntry entry)
        {
            if (entry.PrerequisiteIdTargetSuspend == 0u)
                return false;

            // Approximation: treat target suspend prerequisite as a "pause while condition is met" gate.
            // This prevents periodic ticks from applying when data marks a suspend condition.
            if (target is not IPlayer targetPlayer)
                return false;

            return PrerequisiteManager.Instance.Meets(targetPlayer, entry.PrerequisiteIdTargetSuspend);
        }

        private static List<IUnitEntity> ResolveSharedHealthPoolMembers(ISpell spell)
        {
            var members = spell.Targets
                .Select(t => t.Entity)
                .Where(e => e != null && e.IsAlive)
                .DistinctBy(e => e.Guid)
                .ToList();

            if (members.All(e => e.Guid != spell.Caster.Guid) && spell.Caster.IsAlive)
                members.Add(spell.Caster);

            return members;
        }

        /// <summary>
        /// Give an item to the player (spell effect 0x2B).
        /// </summary>
        [SpellEffectHandler(SpellEffectType.GiveItemToPlayer)]
        public static void HandleEffectGiveItemToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint itemId = info.Entry.DataBits00;
            uint count = info.Entry.DataBits01 == 0u ? 1u : info.Entry.DataBits01;
            if (itemId == 0u)
                return;

            // Create the item in player's inventory
            player.Inventory.ItemCreate(InventoryLocation.Inventory, itemId, count);
        }

        /// <summary>
        /// Advance a quest objective (spell effect 0x2A).
        /// </summary>
        [SpellEffectHandler(SpellEffectType.QuestAdvanceObjective)]
        public static void HandleEffectQuestAdvanceObjective(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            uint questId = info.Entry.DataBits00;
            uint objectiveIndex = info.Entry.DataBits01;
            uint count = info.Entry.DataBits02;

            if (questId == 0u)
                return;

            // Advance CompleteQuest objective type with the quest ID
            // This is a simplification - proper implementation would need to map to specific objective indices
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteQuest, questId, Math.Max(1u, count));
        }
    }
}
