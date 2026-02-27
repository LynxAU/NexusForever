using System.Numerics;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Loot;
using NexusForever.Game.Spell;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Template;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity
{
    public abstract class UnitEntity : WorldEntity, IUnitEntity
    {
        public float HitRadius { get; protected set; } = 1f;

        /// <summary>
        /// Guid of the <see cref="IUnitEntity"/> currently targeted.
        /// </summary>
        public uint? TargetGuid { get; private set; }

        /// <summary>
        /// Determines whether or not this <see cref="IUnitEntity"/> is alive.
        /// </summary>
        public bool IsAlive => Health > 0u && deathState == null;

        protected EntityDeathState? DeathState
        {
            get => deathState;
            set
            {
                deathState = value;

                if (deathState is null or EntityDeathState.JustDied)
                {
                    EnqueueToVisible(new ServerEntityDeathState
                    {
                        UnitId    = Guid,
                        Dead      = !IsAlive,
                        Reason    = 0, // client does nothing with this value
                        RezHealth = IsAlive ? Health : 0u
                    }, true);
                }
            }
        }

        private EntityDeathState? deathState;

        /// <summary>
        /// Determines whether or not this <see cref="IUnitEntity"/> is in combat.
        /// </summary>
        public bool InCombat
        {
            get => inCombat;
            private set
            {
                if (inCombat == value)
                    return;

                inCombat = value;

                EnqueueToVisible(new ServerUnitEnteredCombat
                {
                    UnitId   = Guid,
                    InCombat = value
                }, true);
            }
        }

        private bool inCombat;

        public uint CrowdControlStateMask => crowdControlStateMask;

        public IThreatManager ThreatManager { get; private set; }

        /// <summary>
        /// Initial stab at a timer to regenerate Health & Shield values.
        /// </summary>
        private UpdateTimer statUpdateTimer = new UpdateTimer(0.25); // TODO: Long-term this should be absorbed into individual timers for each Stat regeneration method

        private readonly List<ISpell> pendingSpells = new();
        private readonly List<ActiveCrowdControlState> activeCrowdControlStates = new();
        private readonly List<ActiveTimedAura> activeTimedAuras = new();
        private readonly List<ActiveProcTrigger> activeProcTriggers = new();
        private readonly Dictionary<uint, ActiveDiminishingReturnsState> diminishingReturnsStates = new();
        private uint damageAbsorptionPool;
        private uint healingAbsorptionPool;
        private uint crowdControlStateMask;
        private ulong nextTimedAuraId = 1;
        private bool isProcessingProcTriggers;

        private Dictionary<Property, Dictionary</*spell4Id*/uint, ISpellPropertyModifier>> spellProperties = new();
        private const double DiminishingReturnsWindowSeconds = 15d;

        private sealed class ActiveCrowdControlState
        {
            public CCState State { get; init; }
            public double RemainingDuration { get; set; }
            public uint SourceCasterId { get; init; }
            public uint DiminishingReturnsId { get; init; }
        }

        private sealed class ActiveDiminishingReturnsState
        {
            public byte Applications { get; set; }
            public double RemainingWindow { get; set; }
        }

        [Flags]
        public enum ProcEventMask : uint
        {
            None         = 0u,
            DamageDone   = 1u << 0,
            DamageTaken  = 1u << 1,
            HealDone     = 1u << 2,
            HealTaken    = 1u << 3
        }

        private sealed class ActiveProcTrigger
        {
            public ulong AuraId { get; init; }
            public uint TriggerSpell4Id { get; init; }
            public uint SourceCasterId { get; init; }
            public ProcEventMask EventMask { get; init; }
            public double Chance01 { get; init; }
            public double InternalCooldown { get; init; }
            public double RemainingCooldown { get; set; }
        }

        public sealed class ActiveTimedAura
        {
            public ulong AuraId { get; init; }
            public uint SpellId { get; init; }
            public SpellEffectType EffectType { get; init; }
            public uint SourceCasterId { get; init; }
            public uint StackGroupId { get; init; }
            public uint StackCap { get; init; }
            public uint StackTypeEnum { get; set; }
            public bool IsDispellable { get; set; }
            public bool IsDebuff { get; set; }
            public bool IsBuff { get; set; }
            public double Duration { get; set; }
            public double Elapsed { get; set; }
            public double TickInterval { get; set; }
            public double NextTickAt { get; set; }
            public Action OnTick { get; set; }
            public Action OnRemove { get; set; }
        }

        public uint DamageAbsorptionPool => damageAbsorptionPool;
        public uint HealingAbsorptionPool => healingAbsorptionPool;

        #region Dependency Injection

        public UnitEntity(IMovementManager movementManager)
            : base(movementManager)
        {
            ThreatManager = new ThreatManager(this);

            InitialiseHitRadius();
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();

            foreach (ISpell spell in pendingSpells)
                spell.Dispose();

            activeTimedAuras.Clear();
            activeProcTriggers.Clear();
        }

        private void InitialiseHitRadius()
        {
            if (CreatureEntry == null)
                return;

            Creature2ModelInfoEntry modelInfoEntry = GameTableManager.Instance.Creature2ModelInfo.GetEntry(CreatureEntry.Creature2ModelInfoId);
            if (modelInfoEntry != null)
                HitRadius = modelInfoEntry.HitRadius * CreatureEntry.ModelScale;
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            foreach (ISpell spell in pendingSpells.ToArray())
            {
                spell.Update(lastTick);
                if (spell.IsFinished)
                {
                    spell.Dispose();
                    pendingSpells.Remove(spell);
                }
            }

            UpdateCrowdControlStates(lastTick);
            UpdateTimedAuras(lastTick);
            UpdateProcTriggers(lastTick);
            UpdateDiminishingReturns(lastTick);

            statUpdateTimer.Update(lastTick);
            if (statUpdateTimer.HasElapsed)
            {
                HandleStatUpdate(lastTick);
                statUpdateTimer.Reset();
            }
        }

        /// <summary>
        /// Remove tracked <see cref="IGridEntity"/> that is no longer in vision range.
        /// </summary>
        public override void RemoveVisible(IGridEntity entity)
        {
            if (entity.Guid == TargetGuid)
                SetTarget((IWorldEntity)null);

            ThreatManager.RemoveHostile(entity.Guid);

            base.RemoveVisible(entity);
        }

        /// <summary>
        /// Add a <see cref="Property"/> modifier given a Spell4Id and <see cref="ISpellPropertyModifier"/> instance.
        /// </summary>
        public void AddSpellModifierProperty(ISpellPropertyModifier spellModifier, uint spell4Id)
        {
            if (spellProperties.TryGetValue(spellModifier.Property, out Dictionary<uint, ISpellPropertyModifier> spellDict))
            {
                if (spellDict.ContainsKey(spell4Id))
                    spellDict[spell4Id] = spellModifier;
                else
                    spellDict.Add(spell4Id, spellModifier);
            }
            else
            {
                spellProperties.Add(spellModifier.Property, new Dictionary<uint, ISpellPropertyModifier>
                {
                    { spell4Id, spellModifier }
                });
            }

            CalculateProperty(spellModifier.Property);
        }

        /// <summary>
        /// Remove a <see cref="Property"/> modifier by a Spell that is currently affecting this <see cref="IUnitEntity"/>.
        /// </summary>
        public void RemoveSpellProperty(Property property, uint spell4Id)
        {
            if (spellProperties.TryGetValue(property, out Dictionary<uint, ISpellPropertyModifier> spellDict))
                spellDict.Remove(spell4Id);

            CalculateProperty(property);
        }

        /// <summary>
        /// Remove all <see cref="Property"/> modifiers by a Spell that is currently affecting this <see cref="IUnitEntity"/>
        /// </summary>
        public void RemoveSpellProperties(uint spell4Id)
        {
            List<Property> propertiesWithSpell = spellProperties.Where(i => i.Value.ContainsKey(spell4Id)).Select(p => p.Key).ToList();

            foreach (Property property in propertiesWithSpell)
                RemoveSpellProperty(property, spell4Id);
        }

        public ulong AddTimedAura(uint spellId, SpellEffectType effectType, uint sourceCasterId, double duration, double tickInterval, Action onApply = null, Action onTick = null, Action onRemove = null, uint stackGroupId = 0u, uint stackCap = 0u, uint stackTypeEnum = 0u, bool isDispellable = false, bool isDebuff = false, bool isBuff = false)
        {
            if (duration <= 0d)
            {
                onApply?.Invoke();
                onRemove?.Invoke();
                return 0u;
            }

            if (stackGroupId != 0u)
            {
                bool sharedAcrossSources = stackTypeEnum is 4u or 5u;
                List<ActiveTimedAura> stackGroupAuras = activeTimedAuras
                    .Where(a => a.StackGroupId == stackGroupId
                             && (sharedAcrossSources || (a.EffectType == effectType && a.SourceCasterId == sourceCasterId)))
                    .OrderBy(a => a.AuraId)
                    .ToList();
                ActiveTimedAura existingAura = stackGroupAuras.LastOrDefault();

                // Basic stack behavior baseline:
                // If cap is 0/1, refresh existing aura rather than duplicating.
                if (existingAura != null && (stackCap == 0u || stackCap == 1u))
                {
                    existingAura.Duration = duration;
                    existingAura.Elapsed = 0d;
                    existingAura.TickInterval = tickInterval > 0d ? tickInterval : 0d;
                    existingAura.NextTickAt = tickInterval > 0d ? tickInterval : double.MaxValue;
                    existingAura.StackTypeEnum = stackTypeEnum;
                    existingAura.IsDispellable = isDispellable;
                    existingAura.IsDebuff = isDebuff;
                    existingAura.IsBuff = isBuff;
                    existingAura.OnTick = onTick;
                    existingAura.OnRemove = onRemove;
                    return existingAura.AuraId;
                }

                if (stackCap > 1u && stackGroupAuras.Count >= stackCap)
                {
                    ActiveTimedAura auraToRefresh = stackTypeEnum switch
                    {
                        0u or 1u or 4u => stackGroupAuras.First(),
                        _ => stackGroupAuras.Last()
                    };

                    auraToRefresh.Duration = duration;
                    auraToRefresh.Elapsed = 0d;
                    auraToRefresh.TickInterval = tickInterval > 0d ? tickInterval : 0d;
                    auraToRefresh.NextTickAt = tickInterval > 0d ? tickInterval : double.MaxValue;
                    auraToRefresh.StackTypeEnum = stackTypeEnum;
                    auraToRefresh.IsDispellable = isDispellable;
                    auraToRefresh.IsDebuff = isDebuff;
                    auraToRefresh.IsBuff = isBuff;
                    auraToRefresh.OnTick = onTick;
                    auraToRefresh.OnRemove = onRemove;
                    return auraToRefresh.AuraId;
                }
            }

            onApply?.Invoke();

            var aura = new ActiveTimedAura
            {
                AuraId        = nextTimedAuraId++,
                SpellId       = spellId,
                EffectType    = effectType,
                SourceCasterId = sourceCasterId,
                StackGroupId  = stackGroupId,
                StackCap      = stackCap,
                StackTypeEnum = stackTypeEnum,
                IsDispellable = isDispellable,
                IsDebuff      = isDebuff,
                IsBuff        = isBuff,
                Duration      = duration,
                TickInterval  = tickInterval > 0d ? tickInterval : 0d,
                NextTickAt    = tickInterval > 0d ? tickInterval : double.MaxValue,
                OnTick        = onTick,
                OnRemove      = onRemove
            };

            activeTimedAuras.Add(aura);
            return aura.AuraId;
        }

        public bool RemoveTimedAura(ulong auraId)
        {
            ActiveTimedAura aura = activeTimedAuras.LastOrDefault(a => a.AuraId == auraId);
            if (aura == null)
                return false;

            activeTimedAuras.Remove(aura);
            activeProcTriggers.RemoveAll(p => p.AuraId == auraId);
            aura.OnRemove?.Invoke();
            return true;
        }

        public uint RemoveTimedAuras(uint spellId, SpellEffectType effectType, uint sourceCasterId = 0u)
        {
            List<ActiveTimedAura> matching = activeTimedAuras
                .Where(a => a.SpellId == spellId && a.EffectType == effectType && (sourceCasterId == 0u || a.SourceCasterId == sourceCasterId))
                .ToList();

            foreach (ActiveTimedAura aura in matching)
                RemoveTimedAura(aura.AuraId);

            return (uint)matching.Count;
        }

        public uint RemoveTimedAurasBySpellId(uint spellId, uint sourceCasterId = 0u)
        {
            if (spellId == 0u)
                return 0u;

            List<ActiveTimedAura> matching = activeTimedAuras
                .Where(a => a.SpellId == spellId && (sourceCasterId == 0u || a.SourceCasterId == sourceCasterId))
                .ToList();

            foreach (ActiveTimedAura aura in matching)
                RemoveTimedAura(aura.AuraId);

            return (uint)matching.Count;
        }

        public uint RemoveTimedAurasByEffectType(SpellEffectType effectType, uint sourceCasterId = 0u)
        {
            List<ActiveTimedAura> matching = activeTimedAuras
                .Where(a => a.EffectType == effectType && (sourceCasterId == 0u || a.SourceCasterId == sourceCasterId))
                .ToList();

            foreach (ActiveTimedAura aura in matching)
                RemoveTimedAura(aura.AuraId);

            return (uint)matching.Count;
        }

        public uint ApplyCrowdControlState(CCState state, uint durationMs, uint sourceCasterId, uint diminishingReturnsId = 0u)
        {
            double multiplier = ConsumeDiminishingReturnsMultiplier(diminishingReturnsId);
            if (multiplier <= 0d)
                return 0u;

            uint scaledDurationMs = (uint)Math.Round(durationMs * multiplier);
            double duration = Math.Max(0.05d, scaledDurationMs / 1000d);
            activeCrowdControlStates.Add(new ActiveCrowdControlState
            {
                State             = state,
                RemainingDuration = duration,
                SourceCasterId    = sourceCasterId,
                DiminishingReturnsId = diminishingReturnsId
            });

            crowdControlStateMask |= 1u << (int)state;
            return scaledDurationMs;
        }

        public bool RemoveCrowdControlState(CCState state, uint sourceCasterId = 0u)
        {
            ActiveCrowdControlState active = activeCrowdControlStates.LastOrDefault(s => s.State == state);
            if (active == null)
                return false;

            activeCrowdControlStates.Remove(active);
            if (!activeCrowdControlStates.Any(s => s.State == state))
                crowdControlStateMask &= ~(1u << (int)state);

            SendCrowdControlBreakLog(sourceCasterId != 0u ? sourceCasterId : active.SourceCasterId, state);
            return true;
        }

        public uint RemoveAllCrowdControlStates(uint sourceCasterId = 0u)
        {
            if (activeCrowdControlStates.Count == 0)
                return 0u;

            uint removedCount = 0u;
            foreach (ActiveCrowdControlState active in activeCrowdControlStates.ToList())
            {
                if (RemoveCrowdControlState(active.State, sourceCasterId != 0u ? sourceCasterId : active.SourceCasterId))
                    removedCount++;
            }

            return removedCount;
        }

        public uint RemoveCrowdControlStatesByMask(uint stateMask, uint sourceCasterId = 0u)
        {
            if (stateMask == 0u || activeCrowdControlStates.Count == 0)
                return 0u;

            uint removedCount = 0u;
            foreach (ActiveCrowdControlState active in activeCrowdControlStates.ToList())
            {
                uint stateBit = 1u << (int)active.State;
                if ((stateMask & stateBit) == 0u)
                    continue;

                if (RemoveCrowdControlState(active.State, sourceCasterId != 0u ? sourceCasterId : active.SourceCasterId))
                    removedCount++;
            }

            return removedCount;
        }

        public uint RemoveDispelledAuras(bool removeBuffs, bool removeDebuffs, uint maxInstances = uint.MaxValue)
        {
            if (activeTimedAuras.Count == 0 || maxInstances == 0u)
                return 0u;

            List<ActiveTimedAura> matching = activeTimedAuras
                .Where(a => a.IsDispellable
                            && ((removeBuffs && a.IsBuff) || (removeDebuffs && a.IsDebuff)))
                .OrderByDescending(a => a.AuraId)
                .Take((int)Math.Min(maxInstances, int.MaxValue))
                .ToList();

            foreach (ActiveTimedAura aura in matching)
                RemoveTimedAura(aura.AuraId);

            return (uint)matching.Count;
        }

        public ulong AddProcTrigger(
            uint ownerSpellId,
            uint sourceCasterId,
            uint triggerSpell4Id,
            double durationSeconds,
            double chance01,
            ProcEventMask eventMask,
            double internalCooldownSeconds,
            uint stackGroupId = 0u,
            uint stackCap = 0u,
            uint stackTypeEnum = 0u,
            bool isDispellable = false,
            bool isDebuff = false,
            bool isBuff = false)
        {
            ProcEventMask resolvedMask = eventMask == ProcEventMask.None ? ProcEventMask.DamageDone : eventMask;
            double resolvedDuration = durationSeconds > 0d ? durationSeconds : 15d;
            double resolvedChance = Math.Clamp(chance01, 0d, 1d);
            double resolvedCooldown = Math.Max(0d, internalCooldownSeconds);

            ulong auraId = AddTimedAura(
                ownerSpellId,
                SpellEffectType.Proc,
                sourceCasterId,
                resolvedDuration,
                0d,
                stackGroupId: stackGroupId,
                stackCap: stackCap,
                stackTypeEnum: stackTypeEnum,
                isDispellable: isDispellable,
                isDebuff: isDebuff,
                isBuff: isBuff);

            if (auraId == 0u)
                return 0u;

            activeProcTriggers.Add(new ActiveProcTrigger
            {
                AuraId = auraId,
                TriggerSpell4Id = triggerSpell4Id,
                SourceCasterId = sourceCasterId,
                EventMask = resolvedMask,
                Chance01 = resolvedChance,
                InternalCooldown = resolvedCooldown,
                RemainingCooldown = 0d
            });

            return auraId;
        }

        public void NotifyProcDamageDone(IUnitEntity target, uint amount, uint sourceSpell4Id)
        {
            if (amount == 0u)
                return;

            NotifyProcEvent(ProcEventMask.DamageDone, target, sourceSpell4Id);
        }

        public void NotifyProcDamageTaken(IUnitEntity attacker, uint amount, uint sourceSpell4Id)
        {
            if (amount == 0u)
                return;

            NotifyProcEvent(ProcEventMask.DamageTaken, attacker, sourceSpell4Id);
        }

        public void NotifyProcHealDone(IUnitEntity target, uint amount, uint sourceSpell4Id)
        {
            if (amount == 0u)
                return;

            NotifyProcEvent(ProcEventMask.HealDone, target, sourceSpell4Id);
        }

        public void NotifyProcHealTaken(IUnitEntity healer, uint amount, uint sourceSpell4Id)
        {
            if (amount == 0u)
                return;

            NotifyProcEvent(ProcEventMask.HealTaken, healer, sourceSpell4Id);
        }

        /// <summary>
        /// Return all <see cref="IPropertyModifier"/> for this <see cref="IUnitEntity"/>'s <see cref="Property"/>
        /// </summary>
        private IEnumerable<ISpellPropertyModifier> GetSpellPropertyModifiers(Property property)
        {
            return spellProperties.ContainsKey(property) ? spellProperties[property].Values : Enumerable.Empty<ISpellPropertyModifier>();
        }

        protected override void CalculatePropertyValue(IPropertyValue propertyValue)
        {
            base.CalculatePropertyValue(propertyValue);

            // Run through spell adjustments first because they could adjust base properties
            // dataBits01 appears to be some form of Priority or Math Operator
            foreach (ISpellPropertyModifier spellModifier in GetSpellPropertyModifiers(propertyValue.Property)
                .OrderByDescending(s => s.Priority))
            {
                foreach (IPropertyModifier alteration in spellModifier.Alterations)
                {
                    // TODO: Add checks to ensure we're not modifying FlatValue and Percentage in the same effect?
                    switch (alteration.ModType)
                    {
                        case ModType.FlatValue:
                        case ModType.LevelScale:
                            propertyValue.Value += alteration.GetValue(Level);
                            break;
                        case ModType.Percentage:
                            propertyValue.Value *= alteration.GetValue();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles regeneration of Stat Values. Used to provide a hook into the Update method, for future implementation.
        /// </summary>
        private void HandleStatUpdate(double lastTick)
        {
            if (!IsAlive)
                return;

            // TODO: This should probably get moved to a Calculation Library/Manager at some point. There will be different timers on Stat refreshes, but right now the timer is hardcoded to every 0.25s.
            // Probably worth considering an Attribute-grouped Class that allows us to run differentt regeneration methods & calculations for each stat.

            if (Health < MaxHealth)
                ModifyHealth((uint)(MaxHealth / 200f), DamageType.Heal, null);

            if (Shield < MaxShieldCapacity)
            {
                uint regen = (uint)Math.Min(
                    MaxShieldCapacity * GetPropertyValue(Property.ShieldRegenPct) * statUpdateTimer.Duration,
                    (double)(MaxShieldCapacity - Shield));
                Shield += regen;
            }
        }

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied spell id and <see cref="ISpellParameters"/>.
        /// </summary>
        public void CastSpell(uint spell4Id, ISpellParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException();

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                throw new ArgumentOutOfRangeException();

            CastSpell(spell4Entry.Spell4BaseIdBaseSpell, (byte)spell4Entry.TierIndex, parameters);
        }

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied spell base id, tier and <see cref="ISpellParameters"/>.
        /// </summary>
        public void CastSpell(uint spell4BaseId, byte tier, ISpellParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException();

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
                throw new ArgumentOutOfRangeException();

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier);
            if (spellInfo == null)
                throw new ArgumentOutOfRangeException();

            parameters.SpellInfo = spellInfo;
            CastSpell(parameters);
        }

        /// <summary>
        /// Cast a <see cref="ISpell"/> with the supplied <see cref="ISpellParameters"/>.
        /// </summary>
        public void CastSpell(ISpellParameters parameters)
        {
            if (!IsAlive)
                return;

            if (parameters == null)
                throw new ArgumentNullException();

            if (DisableManager.Instance.IsDisabled(DisableType.BaseSpell, parameters.SpellInfo.BaseInfo.Entry.Id))
            {
                if (this is IPlayer player)
                    player.SendSystemMessage($"Unable to cast base spell {parameters.SpellInfo.BaseInfo.Entry.Id} because it is disabled.");
                return;
            }

            if (DisableManager.Instance.IsDisabled(DisableType.Spell, parameters.SpellInfo.Entry.Id))
            {
                if (this is IPlayer player)
                    player.SendSystemMessage($"Unable to cast spell {parameters.SpellInfo.Entry.Id} because it is disabled.");
                return;
            }

            if (parameters.UserInitiatedSpellCast)
            {
                if (this is IPlayer player)
                    player.Dismount();
            }

            var spell = new Spell.Spell(this, parameters);
            spell.Cast();

            // Failed casts can finish during initial validation. Don't track them.
            if (spell.IsFinished)
            {
                spell.Dispose();
                return;
            }

            pendingSpells.Add(spell);
        }

        /// <summary>
        /// Cancel any <see cref="ISpell"/>'s that are interrupted by movement.
        /// </summary>
        public void CancelSpellsOnMove()
        {
            foreach (ISpell spell in pendingSpells)
                if (spell.IsMovingInterrupted() && spell.IsCasting)
                    spell.CancelCast(CastResult.CasterMovement);
        }

        /// <summary>
        /// Cancel an <see cref="ISpell"/> based on its casting id.
        /// </summary>
        /// <param name="castingId">Casting ID of the spell to cancel</param>
        public void CancelSpellCast(uint castingId)
        {
            CancelSpellCast(castingId, CastResult.SpellCancelled);
        }

        /// <summary>
        /// Cancel an <see cref="ISpell"/> based on its casting id with supplied <see cref="CastResult"/>.
        /// </summary>
        public void CancelSpellCast(uint castingId, CastResult result)
        {
            ISpell spell = pendingSpells.SingleOrDefault(s => s.CastingId == castingId);
            if (spell == null || !spell.IsCasting)
                return;

            CastResult cancelResult = result == CastResult.SpellInterrupted
                ? CastResult.SpellInterrupted
                : CastResult.SpellCancelled;

            spell.CancelCast(cancelResult);
        }

        /// <summary>
        /// Returns an active <see cref="ISpell"/> that is affecting this <see cref="IUnitEntity"/>
        /// </summary>
        public ISpell GetActiveSpell(Func<ISpell, bool> func)
        {
            return pendingSpells.FirstOrDefault(func);
        }

        /// <summary>
        /// Determine if this <see cref="IUnitEntity"/> can attack supplied <see cref="IUnitEntity"/>.
        /// </summary>
        public virtual bool CanAttack(IUnitEntity target)
        {
            if (!IsAlive || target == null)
                return false;

            if (!target.IsValidAttackTarget() || !IsValidAttackTarget())
                return false;

            return GetDispositionTo(target.Faction1) < Disposition.Friendly;
        }

        /// <summary>
        /// Returns whether or not this <see cref="IUnitEntity"/> is an attackable target.
        /// </summary>
        public bool IsValidAttackTarget()
        {
            // TODO: Expand on this. There's bound to be flags or states that should prevent an entity from being attacked.
            return IsAlive && (this is IPlayer or INonPlayerEntity);
        }

        /// <summary>
        /// Deal damage to this <see cref="IUnitEntity"/> from the supplied <see cref="IUnitEntity"/>.
        /// </summary>
        public void TakeDamage(IUnitEntity attacker, IDamageDescription damageDescription)
        {
            if (!IsAlive || !attacker.IsAlive)
                return;

            uint absorbedAmount = ConsumeDamageAbsorption(damageDescription.AdjustedDamage);
            if (absorbedAmount != 0u)
            {
                damageDescription.AbsorbedAmount = (uint)Math.Min((ulong)uint.MaxValue, (ulong)damageDescription.AbsorbedAmount + absorbedAmount);
                damageDescription.AdjustedDamage -= absorbedAmount;
            }

            uint threatAmount = (uint)Math.Min((ulong)int.MaxValue, (ulong)damageDescription.ShieldAbsorbAmount + damageDescription.AdjustedDamage);
            if (threatAmount != 0u)
            {
                // TODO: Calculate Threat properly
                ThreatManager.UpdateThreat(attacker, (int)threatAmount);
            }

            Shield = Shield > damageDescription.ShieldAbsorbAmount ? Shield - damageDescription.ShieldAbsorbAmount : 0u;
            ModifyHealth(damageDescription.AdjustedDamage, damageDescription.DamageType, attacker);
        }

        public void AddDamageAbsorption(uint amount)
        {
            if (amount == 0u)
                return;

            damageAbsorptionPool = (uint)Math.Min((ulong)uint.MaxValue, (ulong)damageAbsorptionPool + amount);
        }

        public void AddHealingAbsorption(uint amount)
        {
            if (amount == 0u)
                return;

            healingAbsorptionPool = (uint)Math.Min((ulong)uint.MaxValue, (ulong)healingAbsorptionPool + amount);
        }

        public uint ConsumeDamageAbsorption(uint amount)
        {
            if (amount == 0u || damageAbsorptionPool == 0u)
                return 0u;

            uint consumedAmount = Math.Min(amount, damageAbsorptionPool);
            damageAbsorptionPool -= consumedAmount;
            return consumedAmount;
        }

        public uint ConsumeHealingAbsorption(uint amount)
        {
            if (amount == 0u || healingAbsorptionPool == 0u)
                return 0u;

            uint consumedAmount = Math.Min(amount, healingAbsorptionPool);
            healingAbsorptionPool -= consumedAmount;
            return consumedAmount;
        }

        /// <summary>
        /// Modify the health of this <see cref="IUnitEntity"/> by the supplied amount.
        /// </summary>
        /// <remarks>
        /// If the <see cref="DamageType"/> is <see cref="DamageType.Heal"/> amount is added to current health otherwise subtracted.
        /// </remarks>
        public virtual void ModifyHealth(uint amount, DamageType type, IUnitEntity source)
        {
            long newHealth = Health;
            if (type == DamageType.Heal)
            {
                amount -= ConsumeHealingAbsorption(amount);
                newHealth += amount;
            }
            else
                newHealth -= amount;

            Health = (uint)Math.Clamp(newHealth, 0u, MaxHealth);

            if (Health == 0)
                OnDeath();
        }

        protected virtual void OnDeath()
        {
            DeathState = EntityDeathState.JustDied;

            foreach (ISpell spell in pendingSpells)
            {
                if (spell.IsCasting)
                    spell.CancelCast(CastResult.CasterCannotBeDead);
            }

            GenerateRewards();
            ThreatManager.ClearThreatList();
            RemoveAllCrowdControlStates();
            activeTimedAuras.Clear();
            activeProcTriggers.Clear();
            damageAbsorptionPool = 0u;
            healingAbsorptionPool = 0u;
            diminishingReturnsStates.Clear();

            deathState = EntityDeathState.Dead;
        }

        private void UpdateCrowdControlStates(double lastTick)
        {
            foreach (ActiveCrowdControlState state in activeCrowdControlStates.ToList())
            {
                state.RemainingDuration -= lastTick;
                if (state.RemainingDuration > 0d)
                    continue;

                RemoveCrowdControlState(state.State, state.SourceCasterId);
            }
        }

        private void UpdateTimedAuras(double lastTick)
        {
            foreach (ActiveTimedAura aura in activeTimedAuras.ToList())
            {
                aura.Elapsed += lastTick;

                if (aura.TickInterval > 0d && aura.OnTick != null)
                {
                    while (aura.NextTickAt <= aura.Elapsed && aura.NextTickAt <= aura.Duration)
                    {
                        aura.OnTick.Invoke();
                        aura.NextTickAt += aura.TickInterval;
                    }
                }

                if (aura.Elapsed < aura.Duration)
                    continue;

                RemoveTimedAura(aura.AuraId);
            }
        }

        private void SendCrowdControlBreakLog(uint casterId, CCState state)
        {
            if (casterId == 0u)
                casterId = Guid;

            EnqueueToVisible(new ServerCombatLog
            {
                CombatLog = new CombatLogCCStateBreak
                {
                    CasterId = casterId,
                    State    = state
                }
            }, true);
        }

        private void UpdateProcTriggers(double lastTick)
        {
            foreach (ActiveProcTrigger proc in activeProcTriggers)
            {
                if (proc.RemainingCooldown <= 0d)
                    continue;

                proc.RemainingCooldown = Math.Max(0d, proc.RemainingCooldown - lastTick);
            }
        }

        private void NotifyProcEvent(ProcEventMask eventMask, IUnitEntity eventTarget, uint sourceSpell4Id)
        {
            if (isProcessingProcTriggers || activeProcTriggers.Count == 0)
                return;

            isProcessingProcTriggers = true;
            try
            {
                foreach (ActiveProcTrigger proc in activeProcTriggers.ToList())
                {
                    if ((proc.EventMask & eventMask) == ProcEventMask.None)
                        continue;

                    if (proc.RemainingCooldown > 0d)
                        continue;

                    if (proc.Chance01 < 1d && Random.Shared.NextDouble() > proc.Chance01)
                        continue;

                    if (!TryCastProcSpell(proc.TriggerSpell4Id, eventTarget))
                        continue;

                    proc.RemainingCooldown = proc.InternalCooldown;
                }
            }
            finally
            {
                isProcessingProcTriggers = false;
            }
        }

        private bool TryCastProcSpell(uint triggerSpell4Id, IUnitEntity eventTarget)
        {
            if (triggerSpell4Id == 0u)
                return false;

            if (GameTableManager.Instance.Spell4.GetEntry(triggerSpell4Id) != null)
            {
                CastSpell(triggerSpell4Id, new SpellParameters
                {
                    PrimaryTargetId = eventTarget?.Guid ?? Guid
                });
                return true;
            }

            Spell4BaseEntry triggerBase = GameTableManager.Instance.Spell4Base.GetEntry(triggerSpell4Id);
            if (triggerBase == null)
                return false;

            CastSpell(triggerBase.Id, 1, new SpellParameters
            {
                PrimaryTargetId = eventTarget?.Guid ?? Guid
            });
            return true;
        }

        private void UpdateDiminishingReturns(double lastTick)
        {
            List<uint> expired = [];
            foreach ((uint key, ActiveDiminishingReturnsState state) in diminishingReturnsStates)
            {
                state.RemainingWindow -= lastTick;
                if (state.RemainingWindow <= 0d)
                    expired.Add(key);
            }

            foreach (uint key in expired)
                diminishingReturnsStates.Remove(key);
        }

        private double ConsumeDiminishingReturnsMultiplier(uint diminishingReturnsId)
        {
            if (diminishingReturnsId == 0u)
                return 1d;

            if (!diminishingReturnsStates.TryGetValue(diminishingReturnsId, out ActiveDiminishingReturnsState state))
            {
                state = new ActiveDiminishingReturnsState();
                diminishingReturnsStates.Add(diminishingReturnsId, state);
            }

            double multiplier = state.Applications switch
            {
                0 => 1d,
                1 => 0.5d,
                2 => 0.25d,
                _ => 0d
            };

            state.Applications = (byte)Math.Min(4, state.Applications + 1);
            state.RemainingWindow = DiminishingReturnsWindowSeconds;
            return multiplier;
        }

        private void GenerateRewards()
        {
            foreach (IHostileEntity hostile in ThreatManager)
            {
                IUnitEntity entity = GetVisible<IUnitEntity>(hostile.HatedUnitId);
                if (entity is IPlayer player)
                    RewardKiller(player);
            }
        }

        protected virtual void RewardKiller(IPlayer player)
        {
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillCreature, CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillCreature2, CreatureId, 1u);

            // Update public event kill objectives for this creature.
            // KillEventUnit / KillEventObjectiveUnit cover generic "kill N enemies" objectives (ObjectId = 0 in table).
            // KillClusterEventObjectiveUnit covers cluster-variant "kill N enemies" objectives.
            // Exterminate covers sweep-style "eliminate all" objectives.
            Map?.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.KillEventUnit, CreatureId, 1);
            Map?.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.KillEventObjectiveUnit, CreatureId, 1);
            Map?.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.KillClusterEventObjectiveUnit, CreatureId, 1);
            Map?.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.Exterminate, CreatureId, 1);

            foreach (uint targetGroupId in AssetManager.Instance.GetTargetGroupsForCreatureId(CreatureId))
            {
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillTargetGroup, targetGroupId, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.KillTargetGroups, targetGroupId, 1u);
                // KillTargetGroup / KillClusterTargetGroup cover boss-specific kill objectives keyed by TargetGroupId.
                Map?.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.KillTargetGroup, targetGroupId, 1);
                Map?.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.KillClusterTargetGroup, targetGroupId, 1);
            }

            // Trigger PvPKills quest objectives when a player kills another player
            // This is separate from regular creature kills
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.PvPKills, 0, 1u);

            // Trigger Unknown10 objectives - appears to be related to difficulty-based kills
            // ObjectiveTexts mention completing challenges or dungeons on certain difficulty
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown10, CreatureId, 1u);

            // Trigger Unknown15 objectives - appears to be for named/specific creature kills
            // Similar to KillCreature but for specific named creatures
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown15, CreatureId, 1u);

            // Trigger CombatMomentum objectives - completing combat momentum actions
            // These are combat-based objectives that track specific combat actions
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CombatMomentum, 0, 1u);

            // Trigger BeginMatrix objectives - using the Primal Matrix
            // Data is 0, triggers when player engages with matrix/combat system
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.BeginMatrix, 0, 1u);

            // Grant kill XP. Use 10% of the level's BaseQuestXpPerLevel as a baseline
            // approximation — pending research into the exact WildStar formula.
            XpPerLevelEntry xpEntry = GameTableManager.Instance.XpPerLevel.GetEntry(Level);
            if (xpEntry != null)
            {
                uint killXp = Math.Max(1u, (uint)(xpEntry.BaseQuestXpPerLevel * 0.10f));
                player.XpManager.GrantXp(killXp, ExpReason.KillCreature);
            }

            // Grant kill credits. Quadratic scale: Level² × 3, floored at 1.
            // Approximation — replace with actual loot table data when available.
            ulong killCredits = Math.Max(1u, (ulong)(Level * Level * 3u));
            player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, killCredits, isLoot: true);

            RewardKillLoot(player);

            // Handle kill-related achievements
            player.AchievementManager.CheckAchievements(player, AchievementType.KillCreatureEntry, CreatureId);
            foreach (uint targetGroupId in AssetManager.Instance.GetTargetGroupsForCreatureId(CreatureId))
            {
                player.AchievementManager.CheckAchievements(player, AchievementType.KillCreatureGroup, targetGroupId);
            }
        }

        private void RewardKillLoot(IPlayer player)
        {
            IEnumerable<LootDrop> drops = DatabaseLootSourceProvider.Instance.RollCreatureLoot(CreatureId);
            foreach (LootDrop drop in drops)
                player.Inventory.ItemCreate(InventoryLocation.Inventory, drop.ItemId, drop.Count, ItemUpdateReason.Loot);
        }

        /// <summary>
        /// Set target to supplied target guid.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        public void SetTarget(uint? target, uint threat = 0u)
        {
            SetTarget(target != null ? GetVisible<IWorldEntity>(target.Value) : null, threat);
        }

        /// <summary>
        /// Set target to supplied <see cref="IUnitEntity"/>.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        public virtual void SetTarget(IWorldEntity target, uint threat = 0u)
        {
            // notify current target they are no longer the target
            if (TargetGuid != null)
                GetVisible<IWorldEntity>(TargetGuid.Value)?.OnUntargeted(this);

            target?.OnTargeted(this);

            EnqueueToVisible(new ServerEntityTargetUnit
            {
                UnitId      = Guid,
                NewTargetId = target?.Guid ?? 0u,
                ThreatLevel = threat
            });

            TargetGuid = target?.Guid;
        }

        /// <summary>
        /// Invoked when a new <see cref="IHostileEntity"/> is added to the threat list.
        /// </summary>
        public virtual void OnThreatAddTarget(IHostileEntity hostile)
        {
            UpdateCombatState();
            scriptCollection?.Invoke<IUnitScript>(s => s.OnThreatAddTarget(hostile));
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is removed from the threat list.
        /// </summary>
        public virtual void OnThreatRemoveTarget(IHostileEntity hostile)
        {
            UpdateCombatState();
            scriptCollection?.Invoke<IUnitScript>(s => s.OnThreatRemoveTarget(hostile));
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is update on the threat list.
        /// </summary>
        public virtual void OnThreatChange(IHostileEntity hostile)
        {
            scriptCollection?.Invoke<IUnitScript>(s => s.OnThreatChange(hostile));
        }

        private void UpdateCombatState()
        {
            // ensure conditions for combat state change are met
            if (ThreatManager.IsThreatened == InCombat)
                return;

            bool wasInCombat = InCombat;

            InCombat   = ThreatManager.IsThreatened;
            Sheathed   = !inCombat;
            StandState = inCombat ? StandState.Stand : StandState.State0;

            if (!wasInCombat && InCombat)
                OnEnterCombat();
            else if (wasInCombat && !InCombat)
                OnExitCombat();
        }

        /// <summary>
        /// Invoked when this <see cref="IUnitEntity"/> transitions from out-of-combat to in-combat.
        /// </summary>
        protected virtual void OnEnterCombat()
        {
            scriptCollection?.Invoke<IUnitScript>(s => s.OnEnterCombat());
        }

        /// <summary>
        /// Invoked when this <see cref="IUnitEntity"/> transitions from in-combat to out-of-combat.
        /// </summary>
        protected virtual void OnExitCombat()
        {
            scriptCollection?.Invoke<IUnitScript>(s => s.OnExitCombat());
        }
    }
}
