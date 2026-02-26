using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity.Movement.Generator;
using NexusForever.Game.Static.Entity.Movement.Spline;
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
        // Fires immediately on first idle tick (starts elapsed), then resets to 20 s.
        private readonly UpdateTimer wanderTimer = new(0.0);

        // True once the DB patrol spline has been launched so it is not re-launched every AI tick.
        private bool patrolLaunched;

        // True while the NPC is walking back to its spawn position after evading.
        private bool isReturning;

        // Countdown from death to corpse removal. Only ticks when started via OnDeath().
        private readonly UpdateTimer corpseDespawnTimer = new(CorpseDuration, start: false);

        // Guard: prevents HandleCorpseDespawn firing more than once per death.
        private bool corpseDespawnFired;

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
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            meleeSwingTimer.Update(lastTick);
            wanderTimer.Update(lastTick);

            aiUpdateTimer.Update(lastTick);
            if (aiUpdateTimer.HasElapsed)
            {
                HandleAiUpdate(lastTick);
                aiUpdateTimer.Reset();
            }
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

            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnCombatUpdate(lastTick));
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
