using System.Linq;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity.Movement.Generator;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NLog;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// An <see cref="ICreatureEntity"/> is an extension to <see cref="IUnitEntity"/> which contains logic specific to non player controlled combat entities.
    /// </summary>
    public abstract class CreatureEntity : UnitEntity, ICreatureEntity
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Radius within which the NPC will auto-aggro hostile units.
        /// </summary>
        public float AggroRadius { get; protected set; } = 10f;

        // Extra melee buffer added to the sum of caster and target HitRadius to determine "in attack range".
        private const float MeleeAttackBuffer = 1.5f;

        // Movement speed when chasing a combat target.
        private const float ChaseSpeed = 7f;

        // Movement speed when fleeing from combat (slightly faster than chase).
        private const float FleeSpeed = 9f;

        // Movement speed when returning to spawn after evading.
        private const float ReturnSpeed = 7f;

        // Walk speed used during out-of-combat wander.
        private const float WanderSpeed = 2.5f;

        // Seconds the corpse remains visible before being removed from the map.
        private const double CorpseDuration = 5.0;

        // Default respawn delay if not specified in creature data.
        private const double DefaultRespawnDelay = 30.0;

        // Per-creature respawn delay loaded from creature data.
        private double respawnDelay = DefaultRespawnDelay;

        // AI decision tick interval in seconds.
        private readonly UpdateTimer aiUpdateTimer = new(0.5);

        // Melee auto-attack swing timer. Default: 2 seconds, matching most MMO baseline swing speed.
        private readonly UpdateTimer meleeSwingTimer = new(2.0);

        // Minimum cadence between AI spell-cast attempts while in combat.
        private readonly UpdateTimer spellActionTimer = new(1.0);

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

        // Health percentage threshold at which the NPC will attempt to flee from combat.
        // Protected so LoadFamilyBehavior() can tune it per-family.
        protected float fleeHealthThreshold = 0.25f;

        // Maximum distance the NPC will flee from their current position.
        private const float FleeMaxDistance = 15f;

        /// <summary>
        /// The leash behavior for this NPC when exceeding leash range.
        /// </nummary>
        public LeashBehavior LeashBehavior { get; protected set; } = LeashBehavior.Standard;

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
            DetermineLeashBehavior();
            LoadRespawnDelay();
            LoadFamilyBehavior();
        }

        /// <summary>
        /// Loads respawn delay from creature data, defaulting to 30 seconds if not specified.
        /// </summary>
        private void LoadRespawnDelay()
        {
            if (CreatureEntry != null && CreatureEntry.RescanCooldown > 0)
            {
                // RescanCooldown is in seconds.
                respawnDelay = CreatureEntry.RescanCooldown;
                log.Trace($"Creature {Guid} (CreatureId {CreatureEntry.Id}) has respawn delay: {respawnDelay}s.");
            }
            else
            {
                respawnDelay = DefaultRespawnDelay;
            }
        }

        /// <summary>
        /// Determines the leash behavior based on creature data.
        /// Can be overridden by derived classes for custom behavior.
        /// </summary>
        protected virtual void DetermineLeashBehavior()
        {
            if (CreatureEntry == null)
            {
                LeashBehavior = LeashBehavior.Standard;
                return;
            }

            // Check creature flags for special behavior.
            // Flag 0x1 typically indicates a world boss or special NPC.
            // Flag 0x2 could indicate no return behavior.
            // This can be extended based on known WildStar flag values.
            uint flags = CreatureEntry.Flags;

            // Check for boss-like behavior (flag 0x1 = world boss)
            if ((flags & 0x1) != 0)
            {
                LeashBehavior = LeashBehavior.Infinite;
                log.Trace($"Creature {Guid} (CreatureId {CreatureEntry.Id}) has boss flag - using infinite leash.");
                return;
            }

            // Check for no-leash behavior (flag 0x2 = never returns)
            if ((flags & 0x2) != 0)
            {
                LeashBehavior = LeashBehavior.None;
                log.Trace($"Creature {Guid} (CreatureId {CreatureEntry.Id}) has no-leash flag.");
                return;
            }

            LeashBehavior = LeashBehavior.Standard;
        }

        /// <summary>
        /// Call for Help - when NPC takes damage, alert nearby allies to attack the attacker.
        /// </summary>
        private DateTime lastCallForHelpTime = DateTime.MinValue;
        private const double CallForHelpCooldown = 10.0; // seconds
        private const float CallForHelpRadius = 30f; // range to alert allies

        protected override void OnDamaged(IUnitEntity attacker, IDamageDescription damageDescription)
        {
            base.OnDamaged(attacker, damageDescription);

            // Don't call for help if dead, not in world, or attacker is null
            if (!IsAlive || Map == null || attacker == null)
                return;

            // Break crowd control on damage
            BreakCrowdControlOnDamage();

            // Threat proportional to damage dealt — keeps target switching accurate when multiple attackers are present.
            if (damageDescription?.AdjustedDamage > 0u)
                ThreatManager.UpdateThreat(attacker, (int)Math.Min(damageDescription.AdjustedDamage, (uint)int.MaxValue));

            // Cooldown check - don't spam call for help
            if ((DateTime.UtcNow - lastCallForHelpTime).TotalSeconds < CallForHelpCooldown)
                return;

            lastCallForHelpTime = DateTime.UtcNow;

            // Scan for nearby allies within CallForHelpRadius
            foreach (ICreatureEntity ally in Map.Search(Position, CallForHelpRadius, new SearchCheckRange<ICreatureEntity>(Position, CallForHelpRadius)))
            {
                if (ally == this)
                    continue;

                // Skip if ally is dead, already in combat, or is the attacker
                if (!ally.IsAlive || ally.TargetGuid != 0 || ally == attacker)
                    continue;

                // Check if ally shares the same faction or is friendly
                if (!ally.CanAttack(attacker))
                {
                    // Ally found - add attacker to their threat list
                    ally.ThreatManager.UpdateThreat(attacker, 1);
                    log.Trace($"Creature {ally.Guid} answered call for help against {attacker.Guid}");
                }
            }
        }

        /// <summary>
        /// Break all crowd control effects when taking damage.
        /// </summary>
        private void BreakCrowdControlOnDamage()
        {
            uint removedCount = RemoveAllCrowdControlStates(0);
            if (removedCount > 0)
            {
                log.Trace($"Creature {Guid} broke {removedCount} CC state(s) from damage.");
            }
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

            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnDeath());

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
            spellActionTimer.Update(lastTick);
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
                Map?.ScheduleRespawn(SpawnModel, respawnDelay);

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

            // Leash check — if we have wandered too far from spawn, handle based on leash behavior.
            if (LeashBehavior != LeashBehavior.None && Vector3.Distance(Position, LeashPosition) > LeashRange)
            {
                if (LeashBehavior == LeashBehavior.Infinite)
                {
                    // Infinite leash: clear threat but don't return to spawn, just stop chasing.
                    scriptCollection?.Invoke<INonPlayerScript>(s => s.OnEvade());
                    ThreatManager.ClearThreatList();
                    SetTarget((IWorldEntity)null);
                    patrolLaunched = false;
                    log.Trace($"Creature {Guid} exceeded leash range with infinite leash - stopping chase.");
                }
                else
                {
                    // Standard behavior: evade and return to spawn.
                    Evade();
                }
                return;
            }

            // Pick the unit at the top of the threat list as the primary combat target.
            IHostileEntity topThreat = ThreatManager.GetTopHostile();
            if (topThreat == null)
                return;

            // Flee check — if health is below threshold, attempt to flee from combat.
            if (ShouldFlee())
            {
                FleeFromTarget(topThreat);
                return;
            }

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
            {
                // If a ranged spell is available and in range, cast instead of forcing a melee chase.
                if (spellActionTimer.HasElapsed)
                {
                    bool casted = TryCastSpellAction(target);
                    spellActionTimer.Reset();
                    if (casted)
                        return;
                }

                ChaseTarget(target);
            }
            else if (IsRangedCreature())
            {
                // Ranged-only creature: back away to maintain safe attack distance.
                KiteFromTarget(target);
                if (spellActionTimer.HasElapsed)
                {
                    TryCastSpellAction(target);
                    spellActionTimer.Reset();
                }
            }
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
        /// Move toward <paramref name="target"/> using a direct linear path with adaptive speed.
        /// </summary>
        private void ChaseTarget(IUnitEntity target)
        {
            MovementManager.SetRotationFaceUnit(target.Guid);

            // Calculate distance and determine appropriate speed.
            float distance = Vector3.Distance(Position, target.Position);
            float attackRange = HitRadius + target.HitRadius + MeleeAttackBuffer;

            // Determine chase destination - stop at attack range.
            Vector3 direction = Vector3.Normalize(target.Position - Position);
            Vector3 chaseDestination;

            if (distance > attackRange)
            {
                // Stop slightly before attack range to avoid overshooting.
                chaseDestination = target.Position - (direction * attackRange * 0.9f);
            }
            else
            {
                // Already in range, don't move closer.
                chaseDestination = Position;
            }

            // Adaptive speed: slow down when close to target for smoother movement.
            float speed = ChaseSpeed;
            float distanceToDestination = Vector3.Distance(Position, chaseDestination);
            if (distanceToDestination < 5f)
            {
                // Slow down when within 5 units of destination.
                speed = MathF.Max(2f, ChaseSpeed * (distanceToDestination / 5f));
            }

            MovementManager.LaunchGenerator(new DirectMovementGenerator
            {
                Begin = Position,
                Final = chaseDestination,
                Map   = Map
            }, speed, SplineMode.OneShot);
        }

        /// <summary>
        /// Returns true if the NPC should attempt to flee from combat based on health threshold.
        /// </summary>
        private bool ShouldFlee()
        {
            // Don't flee if already in return state.
            if (isReturning)
                return false;

            // Don't flee if dead.
            if (!IsAlive)
                return false;

            // Check if health is below the flee threshold.
            float healthPercent = (float)Health / MaxHealth;
            return healthPercent <= fleeHealthThreshold;
        }

        /// <summary>
        /// Makes the NPC flee away from <paramref name="hostile"/>, moving in the opposite direction.
        /// </summary>
        private void FleeFromTarget(IHostileEntity hostile)
        {
            IUnitEntity target = GetVisible<IUnitEntity>(hostile.HatedUnitId);
            if (target == null)
            {
                // Target is no longer visible, just evade normally.
                Evade();
                return;
            }

            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnFlee());

            // Calculate flee direction: away from the target.
            Vector3 fleeDirection = Position - target.Position;
            if (fleeDirection.Length() < 0.001f)
            {
                // If we're at the same position, pick a random direction.
                float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
                fleeDirection = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            }

            fleeDirection = Vector3.Normalize(fleeDirection);

            // Calculate flee destination - move in the opposite direction from target.
            Vector3 fleePosition = Position + (fleeDirection * FleeMaxDistance);

            // Clamp to leash range - don't flee beyond the leash position.
            float distanceFromLeash = Vector3.Distance(fleePosition, LeashPosition);
            if (distanceFromLeash > LeashRange)
            {
                // If the ideal flee position is beyond leash range, flee towards the leash position instead.
                fleePosition = Position + Vector3.Normalize(LeashPosition - Position) * FleeMaxDistance;
            }

            MovementManager.LaunchGenerator(new DirectMovementGenerator
            {
                Begin = Position,
                Final = fleePosition,
                Map   = Map
            }, FleeSpeed, SplineMode.OneShot);

            log.Trace($"Creature {Guid} is fleeing from {target.Guid} to position {fleePosition}.");
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

            if (spellActionTimer.HasElapsed)
            {
                TryCastSpellAction(target);
                spellActionTimer.Reset();
            }

            scriptCollection?.Invoke<INonPlayerScript>(s => s.OnCombatUpdate(lastTick));
        }

        /// <summary>
        /// Attempts to cast the highest-priority available spell from this creature's action set.
        /// Only one spell is attempted per AI tick. Entries are ordered by <see cref="Creature2ActionEntry.OrderIndex"/>.
        /// </summary>
        private bool TryCastSpellAction(IUnitEntity target)
        {
            if (spellActions == null || spellActions.Count == 0)
                return false;

            // Avoid queuing new casts while a current cast is in-flight.
            if (GetActiveSpell(s => s.IsCasting) != null)
                return false;

            // When the target is actively casting, prefer instant spells to apply combat pressure.
            bool targetIsCasting = target.GetActiveSpell(s => s.IsCasting) != null;
            IEnumerable<Creature2ActionEntry> orderedActions = targetIsCasting
                ? spellActions
                    .OrderBy(a =>
                    {
                        Spell4Entry s = GameTableManager.Instance.Spell4.GetEntry(a.ActionData00);
                        return s?.CastTime == 0u ? 0 : 1; // instant spells first
                    })
                    .ThenBy(a => a.OrderIndex)
                : (IEnumerable<Creature2ActionEntry>)spellActions;

            foreach (Creature2ActionEntry action in orderedActions)
            {
                if (spellCooldowns.ContainsKey(action.Id))
                    continue;

                Spell4Entry spellEntry = GameTableManager.Instance.Spell4.GetEntry(action.ActionData00);
                if (spellEntry == null)
                    continue;

                if (!IsSpellInRange(target, spellEntry))
                    continue;

                // Terrain LOS check for ranged spells — skip if terrain blocks the path.
                bool isRangedSpell = spellEntry.TargetMaxRange > HitRadius + 1f + MeleeAttackBuffer;
                if (isRangedSpell && !HasLineOfSight(target))
                    continue;

                bool hadActiveBefore = GetActiveSpell(s => !s.IsFinished && s.Parameters.SpellInfo.Entry.Id == spellEntry.Id) != null;
                CastSpell(action.ActionData00, new SpellParameters { PrimaryTargetId = target.Guid });
                bool hasActiveAfter = GetActiveSpell(s => !s.IsFinished && s.Parameters.SpellInfo.Entry.Id == spellEntry.Id) != null;

                // Instant casts may execute and finish in the same tick, so they can bypass active-spell detection.
                bool castLikelySucceeded = !hadActiveBefore && (hasActiveAfter || spellEntry.CastTime == 0u);
                if (castLikelySucceeded)
                {
                    double cooldown = action.DelayMS > 0u ? action.DelayMS / 1000.0 : DefaultSpellCooldown;
                    spellCooldowns[action.Id] = cooldown;
                    log.Trace($"Creature {Guid} cast spell {spellEntry.Id} on {target.Guid}; cooldown {cooldown:0.00}s.");
                    return true;
                }
            }

            return false;
        }

        private bool IsSpellInRange(IUnitEntity target, Spell4Entry spellEntry)
        {
            float dx = Position.X - target.Position.X;
            float dz = Position.Z - target.Position.Z;
            float centerDistance = MathF.Sqrt((dx * dx) + (dz * dz));
            float horizontalDistance = MathF.Max(0f, centerDistance - HitRadius - target.HitRadius);
            float verticalDistance = MathF.Abs(Position.Y - target.Position.Y);
            float minRange = Math.Max(0f, spellEntry.TargetMinRange);
            float maxRange = spellEntry.TargetMaxRange <= 0f ? float.MaxValue : spellEntry.TargetMaxRange;
            float maxVerticalRange = spellEntry.TargetVerticalRange <= 0f ? float.MaxValue : spellEntry.TargetVerticalRange;

            return horizontalDistance >= minRange
                && horizontalDistance <= maxRange
                && verticalDistance <= maxVerticalRange;
        }

        /// <summary>
        /// Terrain-based line-of-sight check. Samples the terrain height at the midpoint between
        /// this entity and <paramref name="target"/>. If the terrain rises significantly above both
        /// endpoints the path is considered blocked.
        /// Note: this catches cliffs and ledges but cannot detect static world objects.
        /// </summary>
        private bool HasLineOfSight(IUnitEntity target)
        {
            if (Map == null)
                return true;

            float midX = (Position.X + target.Position.X) * 0.5f;
            float midZ = (Position.Z + target.Position.Z) * 0.5f;

            float? midHeight = Map.GetTerrainHeight(midX, midZ);
            if (!midHeight.HasValue)
                return true; // No terrain data — assume clear

            // LOS is blocked when the midpoint terrain is higher than both endpoints (plus tolerance).
            float highestEndpoint = Math.Max(Position.Y, target.Position.Y);
            return midHeight.Value <= highestEndpoint + 2.5f;
        }

        /// <summary>
        /// Returns true if every spell in this creature's action set is a ranged spell —
        /// i.e., its max range exceeds typical melee engagement range.
        /// Ranged creatures will kite rather than engage in melee.
        /// </summary>
        private bool IsRangedCreature()
        {
            if (spellActions == null || spellActions.Count == 0)
                return false;

            float meleeRange = HitRadius + 1f + MeleeAttackBuffer;
            return spellActions.All(a =>
            {
                Spell4Entry spell = GameTableManager.Instance.Spell4.GetEntry(a.ActionData00);
                return spell != null && spell.TargetMaxRange > meleeRange;
            });
        }

        /// <summary>
        /// Moves this creature away from <paramref name="target"/> to maintain optimal ranged
        /// attack distance. Called each AI tick when a ranged creature is within melee range.
        /// </summary>
        private void KiteFromTarget(IUnitEntity target)
        {
            // Determine the optimal kite distance: 75% of the best available spell's max range.
            float optimalRange = spellActions
                .Select(a => GameTableManager.Instance.Spell4.GetEntry(a.ActionData00))
                .Where(s => s != null && s.TargetMaxRange > 0f)
                .Select(s => s.TargetMaxRange * 0.75f)
                .DefaultIfEmpty(12f)
                .Max();

            float currentDistance = Vector3.Distance(Position, target.Position);
            float gap = optimalRange - currentDistance;
            if (gap <= 0.5f)
                return; // Already at comfortable range — don't micro-reposition.

            Vector3 awayDir  = Vector3.Normalize(Position - target.Position);
            Vector3 kitePos  = Position + awayDir * Math.Min(gap, 5f); // at most 5 units per tick

            // Don't kite outside the leash boundary.
            if (Vector3.Distance(kitePos, LeashPosition) > LeashRange)
                kitePos = Position + awayDir * 1f;

            MovementManager.SetRotationFaceUnit(target.Guid);
            MovementManager.LaunchGenerator(new DirectMovementGenerator
            {
                Begin = Position,
                Final = kitePos,
                Map   = Map
            }, FleeSpeed * 0.7f, SplineMode.OneShot);

            log.Trace($"Creature {Guid} kiting from {target.Guid}, moving {gap:0.0}u back.");
        }

        /// <summary>
        /// Called once during <see cref="Initialise"/> to apply family-specific AI tuning.
        /// Override in derived classes or scripts to customise aggro radius, flee threshold,
        /// or other parameters based on <see cref="Creature2Entry.Creature2FamilyId"/>.
        /// </summary>
        protected virtual void LoadFamilyBehavior()
        {
            if (CreatureEntry == null || CreatureEntry.Creature2FamilyId == 0u)
                return;

            // Derive a broad behavioral class from the family ID.
            // Values without verified game-table data use conservative defaults.
            // Subclasses should override this method with family-specific logic.
            switch (CreatureEntry.Creature2FamilyId)
            {
                // Unknown / no special tuning for unrecognised families.
                default:
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
            if (desc == null)
            {
                EnqueueToVisible(new ServerCombatLog
                {
                    CombatLog = new CombatLogDeflect
                    {
                        BMultiHit = false,
                        CastData  = new CombatLogCastData
                        {
                            CasterId     = Guid,
                            TargetId     = target.Guid,
                            SpellId      = 0u,
                            CombatResult = CombatResult.Avoid
                        }
                    }
                }, true);
                return;
            }

            uint healthBefore = target.Health;
            target.TakeDamage(this, desc);

            uint overkill = desc.AdjustedDamage > healthBefore
                ? desc.AdjustedDamage - healthBefore
                : 0u;

            bool hasMultiHit = desc.MultiHitDamage != 0u;
            bool targetVulnerable = target.IsVulnerable;
            var meleeCastData = new CombatLogCastData
            {
                CasterId     = Guid,
                TargetId     = target.Guid,
                SpellId      = 0u,
                CombatResult = desc.CombatResult
            };

            // Compute base-hit damage (excluding the multi-hit bonus) for the primary log entry.
            uint baseDamage = hasMultiHit && desc.AdjustedDamage >= desc.MultiHitDamage
                ? desc.AdjustedDamage - desc.MultiHitDamage
                : desc.AdjustedDamage;

            EnqueueToVisible(new ServerCombatLog
            {
                CombatLog = new CombatLogDamage
                {
                    MitigatedDamage   = baseDamage,
                    RawDamage         = desc.RawDamage,
                    Shield            = desc.ShieldAbsorbAmount,
                    Absorption        = desc.AbsorbedAmount,
                    Overkill          = overkill,
                    Glance            = desc.GlanceAmount,
                    BTargetVulnerable = targetVulnerable,
                    BKilled           = !target.IsAlive,
                    BPeriodic         = false,
                    DamageType        = desc.DamageType,
                    EffectType        = SpellEffectType.Damage,
                    CastData          = meleeCastData
                }
            }, true);

            if (hasMultiHit)
            {
                EnqueueToVisible(new ServerCombatLog
                {
                    CombatLog = new CombatLogMultiHit
                    {
                        DamageAmount      = desc.MultiHitDamage,
                        RawDamage         = desc.MultiHitDamage,
                        Shield            = 0u,
                        Absorption        = 0u,
                        Overkill          = 0u,
                        GlanceAmount      = 0u,
                        BTargetVulnerable = targetVulnerable,
                        BKilled           = !target.IsAlive,
                        BPeriodic         = false,
                        DamageType        = desc.DamageType,
                        EffectType        = SpellEffectType.Damage,
                        CastData          = meleeCastData
                    }
                }, true);
            }
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
