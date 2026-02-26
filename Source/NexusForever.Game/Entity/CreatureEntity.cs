using System.Linq;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity.Movement.Generator;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// An <see cref="ICreatureEntity"/> is an extension to <see cref="IUnitEntity"/> which contains logic specific to non player controlled combat entities.
    /// </summary>
    public abstract class CreatureEntity : UnitEntity, ICreatureEntity
    {
        /// <summary>
        /// Radius within which the NPC will auto-aggro hostile units.
        /// </summary>
        public float AggroRadius { get; protected set; } = 10f;

        // Extra melee buffer added to the sum of caster and target HitRadius to determine "in attack range".
        private const float MeleeAttackBuffer = 1.5f;

        // Movement speed when chasing a combat target.
        private const float ChaseSpeed = 7f;

        // Movement speed when returning to spawn after evading.
        private const float ReturnSpeed = 7f;

        // Walk speed used during out-of-combat wander.
        private const float WanderSpeed = 2.5f;

        // Seconds the corpse remains visible before being removed from the map.
        private const double CorpseDuration = 5.0;

        // Seconds after corpse removal before the creature respawns.
        private const double RespawnDelay = 30.0;

        // AI decision tick interval in seconds.
        private readonly UpdateTimer aiUpdateTimer = new(0.5);

        // Melee auto-attack swing timer. Default: 2 seconds, matching most MMO baseline swing speed.
        private readonly UpdateTimer meleeSwingTimer = new(2.0);

        // Out-of-combat wander timer for entities that have no DB spline assigned.
        // 20-second interval between random movement picks.
        private readonly UpdateTimer wanderTimer = new(20.0);

        // True once the DB patrol spline has been launched so it is not re-launched every AI tick.
        private bool patrolLaunched;

        // True while the NPC is walking back to its spawn position after evading.
        private bool isReturning;

        // Countdown from death to corpse removal. Only ticks when started via OnDeath().
        private readonly UpdateTimer corpseDespawnTimer = new(CorpseDuration, start: false);

        // Guard: prevents HandleCorpseDespawn firing more than once per death.
        private bool corpseDespawnFired;

        // Creature2Action entries from this creature's action set that have a valid Spell4Id in ActionData00.
        // Null until Initialise() runs; empty if the creature has no action set or no spell actions.
        private List<Creature2ActionEntry> spellActions;

        // Per-action cooldown tracker: actionEntry.Id → remaining seconds until next cast.
        private readonly Dictionary<uint, double> spellCooldowns = new();

        // Default spell cooldown used when a spell has no DelayMS.
        private const double DefaultSpellCooldown = 10.0;

        #region Dependency Injection

        public CreatureEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);

            scriptCollection = ScriptManager.Instance.InitialiseEntityScripts<ICreatureEntity>(this);
            LoadSpellActions();
        }

        /// <summary>
        /// Loads spell actions from this creature's <see cref="Creature2ActionSetEntry"/>.
        /// Only entries whose <see cref="Creature2ActionEntry.ActionData00"/> resolves to a valid
        /// <see cref="Spell4Entry"/> are retained — invalid IDs are silently skipped.
        /// </summary>
        private void LoadSpellActions()
        {
            uint actionSetId = CreatureEntry?.Creature2ActionSetId ?? 0u;
            if (actionSetId == 0u)
            {
                spellActions = [];
                return;
            }

            spellActions = GameTableManager.Instance.Creature2Action.Entries
                .Where(e => e != null
                    && e.CreatureActionSetId == actionSetId
                    && e.ActionData00 != 0u
                    && GameTableManager.Instance.Spell4.GetEntry(e.ActionData00) != null)
                .OrderBy(e => e.OrderIndex)
                .ToList();
        }

        /// <summary>
        /// Invoked when the entity dies. Starts the corpse despawn timer for DB-spawned creatures.
        /// </summary>
        protected override void OnDeath()
        {
            base.OnDeath();

            // Only respawn entities that were initialised from a DB record.
            if (SpawnModel == null)
                return;

            corpseDespawnFired = false;
            corpseDespawnTimer.Reset(start: true);
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            if (!IsAlive)
            {
                if (corpseDespawnTimer.IsTicking)
                {
                    corpseDespawnTimer.Update(lastTick);
                    if (corpseDespawnTimer.HasElapsed)
                        HandleCorpseDespawn();
                }
                return;
            }

            meleeSwingTimer.Update(lastTick);
            wanderTimer.Update(lastTick);

            // Tick down per-spell cooldowns.
            foreach (uint key in spellCooldowns.Keys.ToList())
            {
                spellCooldowns[key] -= lastTick;
                if (spellCooldowns[key] <= 0d)
                    spellCooldowns.Remove(key);
            }

            aiUpdateTimer.Update(lastTick);
            if (aiUpdateTimer.HasElapsed)
            {
                HandleAiUpdate(lastTick);
                aiUpdateTimer.Reset();
            }
        }

        /// <summary>
        /// Removes the corpse from the map and schedules a respawn after <see cref="RespawnDelay"/> seconds.
        /// </summary>
        private void HandleCorpseDespawn()
        {
            if (corpseDespawnFired)
                return;

            corpseDespawnFired = true;

            if (SpawnModel != null)
                Map?.ScheduleRespawn(SpawnModel, RespawnDelay);

            Map?.EnqueueRemove(this);
        }

        /// <summary>
        /// Core AI decision loop, ticked every <see cref="aiUpdateTimer"/> interval.
        /// </summary>
        protected virtual void HandleAiUpdate(double lastTick)
        {
            if (!IsAlive)
                return;

            // Wait until we have fully returned to the leash position before resuming normal AI.
            if (isReturning)
            {
                if (Vector3.Distance(Position, LeashPosition) < 1.5f)
                    isReturning = false;
                return;
            }

            if (!InCombat)
            {
                HandleIdleUpdate(lastTick);
                return;
            }

            // Leash check — if we have wandered too far from spawn, evade and reset.
            if (Vector3.Distance(Position, LeashPosition) > LeashRange)
            {
                Evade();
                return;
            }

            // Pick the unit at the top of the threat list as the primary combat target.
            IHostileEntity topThreat = ThreatManager.GetTopHostile();
            if (topThreat == null)
                return;

            IUnitEntity target = GetVisible<IUnitEntity>(topThreat.HatedUnitId);
            if (target == null || !target.IsAlive)
            {
                // Target left vision range or is dead — drop them from the threat list.
                ThreatManager.RemoveHostile(topThreat.HatedUnitId);
                return;
            }

            // Keep the client-visible target indicator in sync.
            if (TargetGuid != target.Guid)
                SetTarget(target);

            float attackRange = HitRadius + target.HitRadius + MeleeAttackBuffer;
            float distance    = Vector3.Distance(Position, target.Position);

            if (distance > attackRange)
                ChaseTarget(target);
            else
                EngageTarget(target, lastTick);
        }

        /// <summary>
        /// Runs each AI tick when the NPC is alive, out of combat, and has finished returning from an evade.
        /// Handles aggro scanning, patrol or wander movement, and script idle callbacks.
        /// </summary>
        private void HandleIdleUpdate(double lastTick)
        {
            ScanForAggroTargets();

            if (Spline != null)
            {
                // Entity has a DB-authored patrol spline — launch it once and let the movement manager loop it.
                if (!patrolLaunched)
                {
                    MovementManager.SetRotationDefaults();
                    MovementManager.LaunchSpline(Spline.SplineId, Spline.Mode, Spline.Speed, true);
                    patrolLaunched = true;
                }
            }
            else if (wanderTimer.HasElapsed)
            {
                // No patrol path — wander to a random nearby point so the NPC isn't completely static.
                float wanderRange = Math.Min(AggroRadius * 0.5f, 8f);
                MovementManager.LaunchGenerator(new RandomMovementGenerator
                {
                    Begin = Position,
                    Leash = LeashPosition,
                    Range = wanderRange,
                    Map   = Map
                }, WanderSpeed, SplineMode.OneShot);
                wanderTimer.Reset();
            }

            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnIdleUpdate(lastTick));
        }

        /// <summary>
        /// Scans visible entities and aggroes any hostile unit within <see cref="AggroRadius"/>.
        /// </summary>
        private void ScanForAggroTargets()
        {
            foreach (IGridEntity entity in visibleEntities.Values)
            {
                if (entity is not IUnitEntity unit)
                    continue;

                if (!CanAttack(unit))
                    continue;

                if (Vector3.Distance(Position, entity.Position) <= AggroRadius)
                {
                    // Adding initial threat pulls the entity into combat via UpdateCombatState.
                    ThreatManager.UpdateThreat(unit, 1);
                    break;
                }
            }
        }

        /// <summary>
        /// Move toward <paramref name="target"/> using a direct linear path.
        /// </summary>
        private void ChaseTarget(IUnitEntity target)
        {
            MovementManager.SetRotationFaceUnit(target.Guid);
            MovementManager.LaunchGenerator(new DirectMovementGenerator
            {
                Begin = Position,
                Final = target.Position,
                Map   = Map
            }, ChaseSpeed, SplineMode.OneShot);
        }

        /// <summary>
        /// Called each AI tick when within melee range of <paramref name="target"/>.
        /// Faces the target, fires a melee swing if the timer is ready, and gives scripts
        /// the opportunity to execute their own combat logic.
        /// </summary>
        private void EngageTarget(IUnitEntity target, double lastTick)
        {
            MovementManager.SetRotationFaceUnit(target.Guid);

            if (meleeSwingTimer.HasElapsed)
            {
                TryMeleeSwing(target);
                meleeSwingTimer.Reset();
            }

            TryCastSpellAction(target);

            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnCombatUpdate(lastTick));
        }

        /// <summary>
        /// Attempts to cast the highest-priority available spell from this creature's action set.
        /// Only one spell is attempted per AI tick. Entries are ordered by <see cref="Creature2ActionEntry.OrderIndex"/>.
        /// </summary>
        private void TryCastSpellAction(IUnitEntity target)
        {
            if (spellActions == null || spellActions.Count == 0)
                return;

            foreach (Creature2ActionEntry action in spellActions)
            {
                if (spellCooldowns.ContainsKey(action.Id))
                    continue;

                CastSpell(action.ActionData00, new SpellParameters { PrimaryTargetId = target.Guid });

                double cooldown = action.DelayMS > 0u ? action.DelayMS / 1000.0 : DefaultSpellCooldown;
                spellCooldowns[action.Id] = cooldown;
                break;
            }
        }

        /// <summary>
        /// Attempt a melee auto-attack against <paramref name="target"/>.
        /// Returns immediately if the hit is deflected (null damage description).
        /// </summary>
        private void TryMeleeSwing(IUnitEntity target)
        {
            var factory = LegacyServiceProvider.Provider.GetService<IFactory<IDamageCalculator>>();
            IDamageDescription desc = factory?.Resolve()?.CalculateMeleeDamage(this, target);
            if (desc != null)
                target.TakeDamage(this, desc);
        }

        /// <summary>
        /// Clears combat state, fully heals, and walks the NPC back to its spawn position.
        /// </summary>
        private void Evade()
        {
            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnEvade());

            ThreatManager.ClearThreatList();
            SetTarget((IWorldEntity)null);

            // Full heal on evade — consistent with most MMO expectations.
            Health = MaxHealth;
            Shield = MaxShieldCapacity;

            // Reset all spell cooldowns so the NPC starts fresh after returning to spawn.
            spellCooldowns.Clear();

            // Patrol will relaunch from the beginning once we reach the leash position.
            patrolLaunched = false;

            isReturning = true;

            MovementManager.LaunchGenerator(new DirectMovementGenerator
            {
                Begin = Position,
                Final = LeashPosition,
                Map   = Map
            }, ReturnSpeed, SplineMode.OneShot);
        }

        /// <summary>
        /// Set target to supplied <see cref="IUnitEntity"/>.
        /// </summary>
        /// <remarks>
        /// A null target will clear the current target.
        /// </remarks>
        public override void SetTarget(IWorldEntity target, uint threat = 0)
        {
            base.SetTarget(target, threat);

            if (target is IPlayer player)
            {
                // plays aggro sound at client, maybe more??
                player.Session.EnqueueMessageEncrypted(new ServerEntityAggroSwitch
                {
                    UnitId   = Guid,
                    TargetId = TargetGuid.Value
                });
            }
        }

        /// <summary>
        /// Invoked when <see cref="ICreatureEntity"/> is targeted by another <see cref="IUnitEntity"/>.
        /// </summary>
        public override void OnTargeted(IUnitEntity source)
        {
            base.OnTargeted(source);

            // client only processes threat list message if the source matches the current target
            if (InCombat && source is IPlayer player)
                ThreatManager.SendThreatList(player.Session);
        }

        /// <summary>
        /// Invoked when a new <see cref="IHostileEntity"/> is added to the threat list.
        /// </summary>
        public override void OnThreatAddTarget(IHostileEntity hostile)
        {
            ThreatManager.BroadcastThreatList();
            base.OnThreatAddTarget(hostile);
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is removed from the threat list.
        /// </summary>
        public override void OnThreatRemoveTarget(IHostileEntity hostile)
        {
            ThreatManager.BroadcastThreatList();
            base.OnThreatRemoveTarget(hostile);
        }

        /// <summary>
        /// Invoked when an existing <see cref="IHostileEntity"/> is update on the threat list.
        /// </summary>
        public override void OnThreatChange(IHostileEntity hostile)
        {
            ThreatManager.BroadcastThreatList();
            base.OnThreatChange(hostile);
        }
    }
}
