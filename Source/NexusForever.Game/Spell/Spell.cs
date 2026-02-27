using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Spell.Event;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Spell.Event;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Spell
{
    public partial class Spell : ISpell
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public ISpellParameters Parameters { get; }
        public uint CastingId { get; }
        public bool IsCasting => status == SpellStatus.Casting;
        public bool IsFinished => status == SpellStatus.Finished;
        public IReadOnlyCollection<ISpellTargetInfo> Targets => targets;

        public IUnitEntity Caster { get; }

        private SpellStatus status;

        private readonly List<ISpellTargetInfo> targets = new();
        private readonly List<ITelegraph> telegraphs = new();

        private readonly ISpellEventManager events = new SpellEventManager();

        private IScriptCollection scriptCollection;

        public Spell(IUnitEntity caster, ISpellParameters parameters)
        {
            Caster     = caster;
            Parameters = parameters;
            CastingId  = GlobalSpellManager.Instance.NextCastingId;
            status     = SpellStatus.Initiating;

            parameters.RootSpellInfo ??= parameters.SpellInfo;

            scriptCollection = ScriptManager.Instance.InitialiseOwnedScripts<ISpell>(this, parameters.SpellInfo.Entry.Id);
        }

        public void Dispose()
        {
            if (scriptCollection != null)
                ScriptManager.Instance.Unload(scriptCollection);

            scriptCollection = null;
        }

        public void Update(double lastTick)
        {
            scriptCollection?.Invoke<IUpdate>(s => s.Update(lastTick));

            events.Update(lastTick);

            if (status == SpellStatus.Executing && !events.HasPendingEvent)
            {
                // spell effects have finished executing
                status = SpellStatus.Finished;
                log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has finished.");
                SendSpellFinish();
            }
        }

        /// <summary>
        /// Begin cast, checking prerequisites before initiating.
        /// </summary>
        public void Cast()
        {
            if (status != SpellStatus.Initiating)
                throw new InvalidOperationException();

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started initating.");

            CastResult result = CheckCast();
            if (result != CastResult.Ok)
            {
                SendSpellCastResult(result);
                status = SpellStatus.Finished;
                return;
            }

            if (Caster is IPlayer player)
                if (Parameters.SpellInfo.GlobalCooldown != null)
                    player.SpellManager.SetGlobalSpellCooldown(Parameters.SpellInfo.GlobalCooldown.CooldownTime / 1000d);

            // Non-player telegraphs are captured at cast start; moving/rotating attached telegraphs are not yet dynamically updated.
            if (Caster is not IPlayer)
                InitialiseTelegraphs();

            SendSpellStart();

            // enqueue spell to be executed after cast time
            events.EnqueueEvent(new SpellEvent(Parameters.SpellInfo.Entry.CastTime / 1000d, Execute));
            status = SpellStatus.Casting;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started casting.");
        }

        private CastResult CheckCast()
        {
            CastResult preReqCheck = CheckPrerequisites();
            if (preReqCheck != CastResult.Ok)
                return preReqCheck;

            CastResult ccResult = CheckCCConditions();
            if (ccResult != CastResult.Ok)
                return ccResult;

            if (Caster is IPlayer player)
            {
                if (player.SpellManager.GetSpellCooldown(Parameters.SpellInfo.Entry.Id) > 0d)
                    return CastResult.SpellCooldown;

                // this isn't entirely correct, research GlobalCooldownEnum
                if (Parameters.SpellInfo.Entry.GlobalCooldownEnum == 0
                    && player.SpellManager.GetGlobalSpellCooldown() > 0d)
                    return CastResult.SpellGlobalCooldown;

                if (Parameters.CharacterSpell?.MaxAbilityCharges > 0 && Parameters.CharacterSpell?.AbilityCharges == 0)
                    return CastResult.SpellNoCharges;
            }

            return CastResult.Ok;
        }

        private CastResult CheckPrerequisites()
        {
            // Prerequisite checks currently require a player context.
            if (Caster is not IPlayer player)
                return CastResult.Ok;

            if (Parameters.SpellInfo.CasterCastPrerequisite != null && !CheckRunnerOverride(player))
            {
                if (!PrerequisiteManager.Instance.Meets(player, Parameters.SpellInfo.CasterCastPrerequisite.Id))
                    return CastResult.PrereqCasterCast;
            }

            // not sure if this should be for explicit and/or implicit targets
            if (Parameters.SpellInfo.TargetCastPrerequisites != null)
            {
                IUnitEntity target = Parameters.PrimaryTargetId != 0u
                    ? Caster.GetVisible<IUnitEntity>(Parameters.PrimaryTargetId)
                    : null;

                if (target is IPlayer targetPlayer)
                {
                    if (!PrerequisiteManager.Instance.Meets(targetPlayer, Parameters.SpellInfo.TargetCastPrerequisites.Id))
                        return CastResult.PrereqTargetCast;
                }
            }

            // this probably isn't the correct place, name implies this should be constantly checked
            if (Parameters.SpellInfo.CasterPersistencePrerequisites != null)
            {
                if (!PrerequisiteManager.Instance.Meets(player, Parameters.SpellInfo.CasterPersistencePrerequisites.Id))
                    return CastResult.PrereqCasterPersistence;
            }

            if (Parameters.SpellInfo.TargetPersistencePrerequisites != null)
            {
                IUnitEntity target = Parameters.PrimaryTargetId != 0u
                    ? Caster.GetVisible<IUnitEntity>(Parameters.PrimaryTargetId)
                    : null;

                if (target is IPlayer targetPlayer)
                {
                    if (!PrerequisiteManager.Instance.Meets(targetPlayer, Parameters.SpellInfo.TargetPersistencePrerequisites.Id))
                        return CastResult.PrereqTargetPersistence;
                }
            }

            return CastResult.Ok;
        }

        private bool CheckRunnerOverride(IPlayer player)
        {
            foreach (PrerequisiteEntry runnerPrereq in Parameters.SpellInfo.PrerequisiteRunners)
                if (PrerequisiteManager.Instance.Meets(player, runnerPrereq.Id))
                    return true;

            return false;
        }

        private CastResult CheckCCConditions()
        {
            if (Parameters.SpellInfo.CasterCCConditions != null)
            {
                CastResult casterCcResult = CheckCCConditionMask(Parameters.SpellInfo.CasterCCConditions, isCaster: true);
                if (casterCcResult != CastResult.Ok)
                    return casterCcResult;
            }

            // not sure if this should be for explicit and/or implicit targets
            if (Parameters.SpellInfo.TargetCCConditions != null)
            {
                IUnitEntity target = Parameters.PrimaryTargetId != 0
                    ? Caster.GetVisible<IUnitEntity>(Parameters.PrimaryTargetId)
                    : null;

                if (target != null)
                {
                    CastResult targetCcResult = CheckCCConditionMask(Parameters.SpellInfo.TargetCCConditions, isCaster: false, target);
                    if (targetCcResult != CastResult.Ok)
                        return targetCcResult;
                }
            }

            return CastResult.Ok;
        }

        private CastResult CheckCCConditionMask(Spell4CCConditionsEntry conditions, bool isCaster, IUnitEntity entityOverride = null)
        {
            uint currentCcMask = GetCurrentCCMask(entityOverride ?? Caster);

            uint mask = conditions.CcStateMask;
            uint required = conditions.CcStateFlagsRequired;

            for (int bit = 0; bit < sizeof(uint) * 8; bit++)
            {
                uint stateFlag = 1u << bit;
                if ((mask & stateFlag) == 0)
                    continue;

                bool mustHave = (required & stateFlag) != 0;
                bool hasState = (currentCcMask & stateFlag) != 0;

                if (mustHave == hasState)
                    continue;

                if (!Enum.IsDefined(typeof(CCState), bit))
                    continue;

                return ResolveCCCastResult((CCState)bit, isCaster, mustHave);
            }

            return CastResult.Ok;
        }

        private static uint GetCurrentCCMask(IUnitEntity entity)
        {
            return entity.CrowdControlStateMask;
        }

        private static CastResult ResolveCCCastResult(CCState state, bool isCaster, bool mustHave)
        {
            if (isCaster)
            {
                return state switch
                {
                    CCState.Stun                 => mustHave ? CastResult.CasterMustBeStun : CastResult.CasterCannotBeStun,
                    CCState.Sleep                => mustHave ? CastResult.CasterMustBeSleep : CastResult.CasterCannotBeSleep,
                    CCState.Root                 => mustHave ? CastResult.CasterMustBeRoot : CastResult.CasterCannotBeRoot,
                    CCState.Disarm               => mustHave ? CastResult.CasterMustBeDisarm : CastResult.CasterCannotBeDisarm,
                    CCState.Silence              => mustHave ? CastResult.CasterMustBeSilence : CastResult.CasterCannotBeSilence,
                    CCState.Polymorph            => mustHave ? CastResult.CasterMustBePolymorph : CastResult.CasterCannotBePolymorph,
                    CCState.Fear                 => mustHave ? CastResult.CasterMustBeFear : CastResult.CasterCannotBeFear,
                    CCState.Hold                 => mustHave ? CastResult.CasterMustBeHold : CastResult.CasterCannotBeHold,
                    CCState.Knockdown            => mustHave ? CastResult.CasterMustBeKnockdown : CastResult.CasterCannotBeKnockdown,
                    CCState.Vulnerability        => mustHave ? CastResult.CasterMustBeVulnerability : CastResult.CasterCannotBeVulnerability,
                    CCState.Disorient            => mustHave ? CastResult.CasterMustBeDisorient : CastResult.CasterCannotBeDisorient,
                    CCState.Disable              => mustHave ? CastResult.CasterMustBeDisable : CastResult.CasterCannotBeDisable,
                    CCState.Taunt                => mustHave ? CastResult.CasterMustBeTaunt : CastResult.CasterCannotBeTaunt,
                    CCState.DeTaunt              => mustHave ? CastResult.CasterMustBeDeTaunt : CastResult.CasterCannotBeDeTaunt,
                    CCState.Blind                => mustHave ? CastResult.CasterMustBeBlind : CastResult.CasterCannotBeBlind,
                    CCState.Knockback            => mustHave ? CastResult.CasterMustBeKnockback : CastResult.CasterCannotBeKnockback,
                    CCState.Pushback             => mustHave ? CastResult.CasterMustBePushback : CastResult.CasterCannotBePushback,
                    CCState.Pull                 => mustHave ? CastResult.CasterMustBePull : CastResult.CasterCannotBePull,
                    CCState.PositionSwitch       => mustHave ? CastResult.CasterMustBePositionSwitch : CastResult.CasterCannotBePositionSwitch,
                    CCState.Tether               => mustHave ? CastResult.CasterMustBeTether : CastResult.CasterCannotBeTether,
                    CCState.Snare                => mustHave ? CastResult.CasterMustBeSnare : CastResult.CasterCannotBeSnare,
                    CCState.Interrupt            => mustHave ? CastResult.CasterMustBeInterrupt : CastResult.CasterCannotBeInterrupt,
                    CCState.Daze                 => mustHave ? CastResult.CasterMustBeDaze : CastResult.CasterCannotBeDaze,
                    CCState.Subdue               => mustHave ? CastResult.CasterMustBeSubdue : CastResult.CasterCannotBeSubdue,
                    CCState.Grounded             => mustHave ? CastResult.CasterMustBeGrounded : CastResult.CasterCannotBeGrounded,
                    CCState.DisableCinematic     => mustHave ? CastResult.CasterMustBeDisableCinematic : CastResult.CasterCannotBeDisableCinematic,
                    CCState.AbilityRestriction   => mustHave ? CastResult.CasterMustBeAbilityRestriction : CastResult.CasterCannotBeAbilityRestriction,
                    _                            => CastResult.SpellBad
                };
            }

            return state switch
            {
                CCState.Stun                 => mustHave ? CastResult.TargetMustBeStun : CastResult.TargetCannotBeStun,
                CCState.Sleep                => mustHave ? CastResult.TargetMustBeSleep : CastResult.TargetCannotBeSleep,
                CCState.Root                 => mustHave ? CastResult.TargetMustBeRoot : CastResult.TargetCannotBeRoot,
                CCState.Disarm               => mustHave ? CastResult.TargetMustBeDisarm : CastResult.TargetCannotBeDisarm,
                CCState.Silence              => mustHave ? CastResult.TargetMustBeSilence : CastResult.TargetCannotBeSilence,
                CCState.Polymorph            => mustHave ? CastResult.TargetMustBePolymorph : CastResult.TargetCannotBePolymorph,
                CCState.Fear                 => mustHave ? CastResult.TargetMustBeFear : CastResult.TargetCannotBeFear,
                CCState.Hold                 => mustHave ? CastResult.TargetMustBeHold : CastResult.TargetCannotBeHold,
                CCState.Knockdown            => mustHave ? CastResult.TargetMustBeKnockdown : CastResult.TargetCannotBeKnockdown,
                CCState.Vulnerability        => mustHave ? CastResult.TargetMustBeVulnerability : CastResult.TargetCannotBeVulnerability,
                CCState.Disorient            => mustHave ? CastResult.TargetMustBeDisorient : CastResult.TargetCannotBeDisorient,
                CCState.Disable              => mustHave ? CastResult.TargetMustBeDisable : CastResult.TargetCannotBeDisable,
                CCState.Taunt                => mustHave ? CastResult.TargetMustBeTaunt : CastResult.TargetCannotBeTaunt,
                CCState.DeTaunt              => mustHave ? CastResult.TargetMustBeDeTaunt : CastResult.TargetCannotBeDeTaunt,
                CCState.Blind                => mustHave ? CastResult.TargetMustBeBlind : CastResult.TargetCannotBeBlind,
                CCState.Knockback            => mustHave ? CastResult.TargetMustBeKnockback : CastResult.TargetCannotBeKnockback,
                CCState.Pushback             => mustHave ? CastResult.TargetMustBePushback : CastResult.TargetCannotBePushback,
                CCState.Pull                 => mustHave ? CastResult.TargetMustBePull : CastResult.TargetCannotBePull,
                CCState.PositionSwitch       => mustHave ? CastResult.TargetMustBePositionSwitch : CastResult.TargetCannotBePositionSwitch,
                CCState.Tether               => mustHave ? CastResult.TargetMustBeTether : CastResult.TargetCannotBeTether,
                CCState.Snare                => mustHave ? CastResult.TargetMustBeSnare : CastResult.TargetCannotBeSnare,
                CCState.Interrupt            => mustHave ? CastResult.TargetMustBeInterrupt : CastResult.TargetCannotBeInterrupt,
                CCState.Daze                 => mustHave ? CastResult.TargetMustBeDaze : CastResult.TargetCannotBeDaze,
                CCState.Subdue               => mustHave ? CastResult.TargetMustBeSubdue : CastResult.TargetCannotBeSubdue,
                CCState.Grounded             => mustHave ? CastResult.TargetMustBeGrounded : CastResult.TargetCannotBeGrounded,
                CCState.DisableCinematic     => mustHave ? CastResult.TargetMustBeDisableCinematic : CastResult.TargetCannotBeDisableCinematic,
                CCState.AbilityRestriction   => mustHave ? CastResult.TargetMustBeAbilityRestriction : CastResult.TargetCannotBeAbilityRestriction,
                _                            => CastResult.SpellBad
            };
        }

        private void InitialiseTelegraphs()
        {
            telegraphs.Clear();
            foreach (TelegraphDamageEntry telegraphDamageEntry in Parameters.SpellInfo.Telegraphs)
                telegraphs.Add(new Telegraph(telegraphDamageEntry, Caster, Caster.Position, Caster.Rotation));
        }

        /// <summary>
        /// Cancel cast with supplied <see cref="CastResult"/>.
        /// </summary>
        public void CancelCast(CastResult result)
        {
            if (status != SpellStatus.Casting)
                throw new InvalidOperationException();

            if (Caster is IPlayer player && !player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new Server07F9
                {
                    ServerUniqueId = CastingId,
                    CastResult     = result,
                    CancelCast     = true
                });
            }

            events.CancelEvents();
            status = SpellStatus.Executing;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} cast was cancelled.");
        }

        public void EnqueueEvent(double delay, Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            events.EnqueueEvent(new SpellEvent(Math.Max(0d, delay), callback));
        }

        private void Execute()
        {
            status = SpellStatus.Executing;
            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} has started executing.");

            if (Caster is IPlayer player)
            {
                if (Parameters.SpellInfo.Entry.SpellCoolDown != 0u)
                    player.SpellManager.SetSpellCooldown(Parameters.SpellInfo.Entry.Id, Parameters.SpellInfo.Entry.SpellCoolDown / 1000d);

                // Update SpellSuccess quest objectives when spell is cast
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess, Parameters.SpellInfo.Entry.Id, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess2, Parameters.SpellInfo.Entry.Id, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess3, Parameters.SpellInfo.Entry.Id, 1u);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SpellSuccess4, Parameters.SpellInfo.Entry.Id, 1u);
            }

            SelectTargets();
            ExecuteEffects();
            CostSpell();

            SendSpellGo();
        }

        private void CostSpell()
        {
            if (Parameters.CharacterSpell?.MaxAbilityCharges > 0)
                Parameters.CharacterSpell.UseCharge();
        }

        private void SelectTargets()
        {
            targets.Clear();
            AddOrMergeTarget(SpellEffectTargetFlags.Caster, Caster);

            if (Parameters.PrimaryTargetId != 0)
            {
                IUnitEntity primaryTargetEntity = Caster.GetVisible<IUnitEntity>(Parameters.PrimaryTargetId);
                AddOrMergeTarget(SpellEffectTargetFlags.Target, primaryTargetEntity);
            }

            if (Caster is IPlayer)
                InitialiseTelegraphs();

            foreach (ITelegraph telegraph in telegraphs)
            {
                foreach (IUnitEntity entity in telegraph.GetTargets())
                    AddOrMergeTarget(SpellEffectTargetFlags.Telegraph, entity);
            }
        }

        private void AddOrMergeTarget(SpellEffectTargetFlags flags, IUnitEntity entity)
        {
            if (entity == null)
                return;

            // Merge cross-source flags for the same entity (e.g. explicit target + telegraph).
            // Preserve same-flag duplicates (notably multiple telegraphs) until those semantics are implemented.
            SpellTargetInfo existingTarget = targets
                .OfType<SpellTargetInfo>()
                .FirstOrDefault(t => t.Entity.Guid == entity.Guid && (t.Flags & flags) == 0);

            if (existingTarget != null)
            {
                existingTarget.AddFlags(flags);
                return;
            }

            targets.Add(new SpellTargetInfo(flags, entity));
        }

        private void ExecuteEffects()
        {
            foreach (Spell4EffectsEntry spell4EffectsEntry in Parameters.SpellInfo.Effects)
            {
                // select targets for effect
                List<ISpellTargetInfo> effectTargets = targets
                    .Where(t => (t.Flags & (SpellEffectTargetFlags)spell4EffectsEntry.TargetFlags) != 0)
                    .ToList();

                SpellEffectDelegate handler = GlobalSpellManager.Instance.GetEffectHandler((SpellEffectType)spell4EffectsEntry.EffectType);
                if (handler == null)
                    log.Warn($"Unhandled spell effect {(SpellEffectType)spell4EffectsEntry.EffectType}");
                else
                {
                    uint effectId = GlobalSpellManager.Instance.NextEffectId;
                    foreach (SpellTargetInfo effectTarget in effectTargets)
                    {
                        if (!MeetsEffectApplyPrerequisites(effectTarget.Entity, spell4EffectsEntry))
                            continue;

                        var info = new SpellTargetInfo.SpellTargetEffectInfo(effectId, spell4EffectsEntry);
                        effectTarget.Effects.Add(info);

                        try
                        {
                            handler.Invoke(this, effectTarget.Entity, info);
                        }
                        catch (Exception e)
                        {
                            info.DropEffect = true;
                            log.Error(e, $"Unhandled exception in spell effect handler {(SpellEffectType)spell4EffectsEntry.EffectType} for spell {Parameters.SpellInfo.Entry.Id}.");
                        }
                    }
                }
            }
        }

        private bool MeetsEffectApplyPrerequisites(IUnitEntity target, Spell4EffectsEntry effect)
        {
            if (Caster is IPlayer casterPlayer
                && effect.PrerequisiteIdCasterApply != 0u
                && !PrerequisiteManager.Instance.Meets(casterPlayer, effect.PrerequisiteIdCasterApply))
            {
                return false;
            }

            if (target is IPlayer targetPlayer
                && effect.PrerequisiteIdTargetApply != 0u
                && !PrerequisiteManager.Instance.Meets(targetPlayer, effect.PrerequisiteIdTargetApply))
            {
                return false;
            }

            return true;
        }

        public bool IsMovingInterrupted()
        {
            return Parameters.SpellInfo.Entry.CastTime > 0u
                || Parameters.SpellInfo.Entry.ChannelInitialDelay > 0u
                || Parameters.SpellInfo.Entry.ChannelMaxTime > 0u;
        }

        private void SendSpellCastResult(CastResult castResult)
        {
            if (castResult == CastResult.Ok)
                return;

            log.Trace($"Spell {Parameters.SpellInfo.Entry.Id} failed to cast {castResult}.");

            if (Caster is IPlayer player && !player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerSpellCastResult
                {
                    Spell4Id   = Parameters.SpellInfo.Entry.Id,
                    CastResult = castResult
                });
            }
        }

        private void SendSpellStart()
        {
            IUnitEntity primaryTarget = Parameters.PrimaryTargetId > 0
                ? Caster.GetVisible<IUnitEntity>(Parameters.PrimaryTargetId)
                : null;

            var spellStart = new ServerSpellStart
            {
                CastingId              = CastingId,
                CasterId               = Caster.Guid,
                PrimaryTargetId        = primaryTarget?.Guid ?? Caster.Guid,
                Spell4Id               = Parameters.SpellInfo.Entry.Id,
                RootSpell4Id           = Parameters.RootSpellInfo?.Entry.Id ?? 0,
                ParentSpell4Id         = Parameters.ParentSpellInfo?.Entry.Id ?? 0,
                FieldPosition          = new Position(Caster.Position),
                Yaw                    = Caster.Rotation.X,
                UserInitiatedSpellCast = Parameters.UserInitiatedSpellCast,
                InitialPositionData    = new List<ServerSpellStart.InitialPosition>(),
                TelegraphPositionData  = new List<ServerSpellStart.TelegraphPosition>()
            };

            var unitsCasting = new List<IUnitEntity>();
            if (primaryTarget != null)
                unitsCasting.Add(primaryTarget ?? Caster);
            else
                unitsCasting.Add(Caster);

            foreach (IUnitEntity unit in unitsCasting)
            {
                spellStart.InitialPositionData.Add(new ServerSpellStart.InitialPosition
                {
                    UnitId      = unit.Guid,
                    Position    = new Position(unit.Position),
                    TargetFlags = 3,
                    Yaw         = unit.Rotation.X
                });
            }

            foreach (IUnitEntity unit in unitsCasting)
            {
                foreach (ITelegraph telegraph in telegraphs)
                {
                    spellStart.TelegraphPositionData.Add(new ServerSpellStart.TelegraphPosition
                    {
                        TelegraphId    = (ushort)telegraph.TelegraphDamage.Id,
                        AttachedUnitId = unit.Guid,
                        TargetFlags    = 3,
                        Position       = new Position(telegraph.Position),
                        Yaw            = telegraph.Rotation.X
                    });
                }
            }

            Caster.EnqueueToVisible(spellStart, true);
        }

        private void SendSpellFinish()
        {
            if (status != SpellStatus.Finished)
                return;

            Caster.EnqueueToVisible(new ServerSpellFinish
            {
                ServerUniqueId = CastingId,
            }, true);
        }

        private void SendSpellGo()
        {
            List<ICombatLog> combatLogs = [];

            var serverSpellGo = new ServerSpellGo
            {
                ServerUniqueId     = CastingId,
                PrimaryDestination = new Position(Caster.Position),
                Phase              = -1
            };

            foreach (ISpellTargetInfo targetInfo in targets
                .Where(t => t.Effects.Count > 0))
            {
                if (!targetInfo.Effects.Any(x => x.DropEffect == false))
                {
                    combatLogs.AddRange(targetInfo.Effects.SelectMany(i => i.CombatLogs));
                    continue;
                }

                var networkTargetInfo = new TargetInfo
                {
                    UnitId        = targetInfo.Entity.Guid,
                    TargetFlags   = 1,
                    InstanceCount = 1,
                    CombatResult  = CombatResult.Hit
                };

                foreach (ISpellTargetEffectInfo targetEffectInfo in targetInfo.Effects)
                {
                    if (targetEffectInfo.DropEffect)
                    {
                        combatLogs.AddRange(targetEffectInfo.CombatLogs);
                        continue;
                    }

                    if (targetEffectInfo.Entry.EffectType == SpellEffectType.Proxy)
                        continue;

                    var networkTargetEffectInfo = new TargetInfo.EffectInfo
                    {
                        Spell4EffectId = targetEffectInfo.Entry.Id,
                        EffectUniqueId = targetEffectInfo.EffectId,
                        TimeRemaining  = -1
                    };

                    if (targetEffectInfo.Damage != null)
                    {
                        networkTargetEffectInfo.InfoType = 1;
                        networkTargetEffectInfo.DamageDescriptionData = new TargetInfo.EffectInfo.DamageDescription
                        {
                            RawDamage          = targetEffectInfo.Damage.RawDamage,
                            RawScaledDamage    = targetEffectInfo.Damage.RawScaledDamage,
                            AbsorbedAmount     = targetEffectInfo.Damage.AbsorbedAmount,
                            ShieldAbsorbAmount = targetEffectInfo.Damage.ShieldAbsorbAmount,
                            AdjustedDamage     = targetEffectInfo.Damage.AdjustedDamage,
                            OverkillAmount     = targetEffectInfo.Damage.OverkillAmount,
                            KilledTarget       = targetEffectInfo.Damage.KilledTarget,
                            CombatResult       = targetEffectInfo.Damage.CombatResult,
                            DamageType         = targetEffectInfo.Damage.DamageType
                        };
                    }

                    networkTargetInfo.EffectInfoData.Add(networkTargetEffectInfo);

                    combatLogs.AddRange(targetEffectInfo.CombatLogs);
                }

                serverSpellGo.TargetInfoData.Add(networkTargetInfo);
            }

            var unitsCasting = new List<IUnitEntity>
            {
                Caster
            };

            foreach (IUnitEntity unit in unitsCasting)
            {
                serverSpellGo.InitialPositionData.Add(new InitialPosition
                {
                    UnitId      = unit.Guid,
                    Position    = new Position(unit.Position),
                    TargetFlags = 3,
                    Yaw         = unit.Rotation.X
                });
            }

            foreach (IUnitEntity unit in unitsCasting)
            {
                foreach (ITelegraph telegraph in telegraphs)
                {
                    serverSpellGo.TelegraphPositionData.Add(new TelegraphPosition
                    {
                        TelegraphId    = (ushort)telegraph.TelegraphDamage.Id,
                        AttachedUnitId = unit.Guid,
                        TargetFlags    = 3,
                        Position       = new Position(telegraph.Position),
                        Yaw            = telegraph.Rotation.X
                    });
                }
            }

            foreach (ICombatLog combatLog in combatLogs)
            {
                Caster.EnqueueToVisible(new ServerCombatLog
                {
                    CombatLog = combatLog
                }, true);
            }

            Caster.EnqueueToVisible(serverSpellGo, true);

        }

        private void SendRemoveBuff(uint unitId)
        {
            if (!Parameters.SpellInfo.BaseInfo.HasIcon)
                throw new InvalidOperationException();

            Caster.EnqueueToVisible(new ServerSpellBuffRemove
            {
                CastingId = CastingId,
                CasterId  = unitId
            }, true);
        }
    }
}
