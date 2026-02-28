using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.World.Entity;
using NexusForever.Script.Template;

namespace NexusForever.Script.Main.Instance
{
    // Lightweight ISpellParameters implementation for boss script use.
    // Only PrimaryTargetId (and optionally Position) need to be set for boss casts.
    internal sealed class BossSpellParameters : ISpellParameters
    {
        public ICharacterSpell CharacterSpell { get; set; }
        public ISpellInfo SpellInfo { get; set; }
        public ISpellInfo ParentSpellInfo { get; set; }
        public ISpellInfo RootSpellInfo { get; set; }
        public bool UserInitiatedSpellCast { get; set; }
        public uint PrimaryTargetId { get; set; }
        public Position Position { get; set; }
        public ushort TaxiNode { get; set; }
    }

    /// <summary>
    /// Base script for boss encounter NPCs inside a <see cref="IContentMapInstance"/>.
    /// On death, notifies the containing map script via <see cref="IContentMapInstance.TriggerBossDeath"/>.
    /// Subclasses should add <see cref="NexusForever.Script.Template.Filter.ScriptFilterCreatureIdAttribute"/>
    /// to bind to the correct creature id.
    /// </summary>
    public class EncounterBossScript : INonPlayerScript, IOwnedScript<INonPlayerEntity>
    {
        private INonPlayerEntity owner;

        /// <inheritdoc/>
        public virtual void OnLoad(INonPlayerEntity owner)
        {
            this.owner = owner;
        }

        /// <inheritdoc/>
        public void OnDeath()
        {
            (owner.Map as IContentMapInstance)?.TriggerBossDeath(owner.CreatureId);
        }
    }

    /// <summary>
    /// Extended base for boss encounter scripts.
    /// Provides health-based phase transitions, a scheduled spell-cast system,
    /// and an enrage timer — all driven by <see cref="INonPlayerScript.OnCombatUpdate"/>.
    ///
    /// Usage:
    /// <code>
    /// [ScriptFilterCreatureId(12345u)]
    /// public class MyBossScript : BossEncounterScript
    /// {
    ///     protected override void OnBossLoad()
    ///     {
    ///         ScheduleSpell(spellId: 99999, initialDelay: 4.0, interval: 12.0);
    ///         AddPhase(healthPct: 50f, OnPhase2);
    ///         SetEnrage(seconds: 480.0, enrageSpellId: 11111);
    ///     }
    ///     private void OnPhase2() { ScheduleSpell(88888, 2.0, 8.0); }
    /// }
    /// </code>
    /// </summary>
    public abstract class BossEncounterScript : EncounterBossScript
    {
        private sealed class Phase
        {
            public float HealthThreshold; // 0.0–1.0
            public Action OnEnter;
        }

        private sealed class ScheduledSpell
        {
            public readonly uint Spell4Id;
            public readonly double Interval;
            public double Timer;

            public ScheduledSpell(uint spell4Id, double initialDelay, double interval)
            {
                Spell4Id  = spell4Id;
                Interval  = interval;
                Timer     = initialDelay;
            }

            public void Reset() => Timer = Interval;
        }

        private readonly List<Phase>         phases = new();
        private readonly List<ScheduledSpell> spells = new();

        private double enrageDuration;   // seconds set at load
        private double enrageRemaining;  // countdown
        private uint   enrageSpellId;

        private int currentPhaseIdx = -1;

        /// <summary>The owning boss NPC entity.</summary>
        protected INonPlayerEntity Owner { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public override void OnLoad(INonPlayerEntity owner)
        {
            base.OnLoad(owner);
            Owner = owner;
            OnBossLoad();
        }

        /// <summary>
        /// Override to define phases, schedule spells, and set enrage.
        /// Called once when the boss NPC is loaded into the world.
        /// </summary>
        protected virtual void OnBossLoad() { }

        /// <inheritdoc/>
        public void OnCombatUpdate(double lastTick)
        {
            CheckPhases();
            TickSpells(lastTick);
            TickEnrage(lastTick);
        }

        /// <inheritdoc/>
        public void OnEvade()
        {
            // Reset all timers so the next pull starts clean.
            foreach (ScheduledSpell s in spells)
                s.Reset();

            enrageRemaining = enrageDuration;
            currentPhaseIdx = -1;
        }

        // ── Builder API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Register a phase that triggers when boss health drops at or below
        /// <paramref name="healthPct"/> percent (0–100).
        /// Multiple phases are evaluated highest-to-lowest.
        /// </summary>
        protected void AddPhase(float healthPct, Action onEnter)
        {
            phases.Add(new Phase { HealthThreshold = healthPct / 100f, OnEnter = onEnter });
            phases.Sort((a, b) => b.HealthThreshold.CompareTo(a.HealthThreshold));
        }

        /// <summary>
        /// Schedule a repeating spell cast.
        /// The first cast fires after <paramref name="initialDelay"/> seconds;
        /// subsequent casts fire every <paramref name="interval"/> seconds.
        /// </summary>
        protected void ScheduleSpell(uint spell4Id, double initialDelay, double interval)
        {
            spells.Add(new ScheduledSpell(spell4Id, initialDelay, interval));
        }

        /// <summary>
        /// Set a hard enrage: after <paramref name="seconds"/> of combat the boss casts
        /// <paramref name="enrageSpellId"/> (typically an AoE wipe ability).
        /// </summary>
        protected void SetEnrage(double seconds, uint enrageSpellId)
        {
            enrageDuration   = seconds;
            enrageRemaining  = seconds;
            this.enrageSpellId = enrageSpellId;
        }

        // ── Internal tick logic ───────────────────────────────────────────────────

        private float HealthPercent =>
            Owner != null && Owner.MaxHealth > 0
                ? (float)Owner.Health / Owner.MaxHealth
                : 1f;

        private void CheckPhases()
        {
            float hp = HealthPercent;
            for (int i = currentPhaseIdx + 1; i < phases.Count; i++)
            {
                if (hp > phases[i].HealthThreshold)
                    break; // phases are descending; no point checking further

                currentPhaseIdx = i;
                phases[i].OnEnter?.Invoke();
            }
        }

        private void TickSpells(double lastTick)
        {
            if (Owner == null) return;

            uint targetId = Owner.ThreatManager?.GetTopHostile()?.HatedUnitId ?? 0u;
            if (targetId == 0u) return;

            foreach (ScheduledSpell spell in spells)
            {
                spell.Timer -= lastTick;
                if (spell.Timer > 0)
                    continue;

                spell.Timer = spell.Interval;
                Owner.CastSpell(spell.Spell4Id, new BossSpellParameters { PrimaryTargetId = targetId });
            }
        }

        private void TickEnrage(double lastTick)
        {
            if (enrageSpellId == 0 || enrageRemaining <= 0)
                return;

            enrageRemaining -= lastTick;
            if (enrageRemaining > 0)
                return;

            enrageRemaining = 0;
            // Cast enrage on self (typically a PBAE wipe or damage buff).
            Owner?.CastSpell(enrageSpellId, new BossSpellParameters { PrimaryTargetId = Owner.Guid });
        }
    }
}
