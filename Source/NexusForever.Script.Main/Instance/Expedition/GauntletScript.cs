using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for The Gauntlet expedition (WorldId 2183).
    ///
    /// Completion condition: all four boss objectives must be cleared.
    /// Each objective accepts either a veteran creature ID or its normal equivalent:
    ///   Obj 1864: 69254 (veteran) or 48491 (normal)
    ///   Obj 1865: 69255 (veteran) or 48529 (normal)
    ///   Obj 1873: 69308 (veteran) or 48579 (normal)
    ///   Obj 1874: 69305 (veteran) or 48554 (normal, final)
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Gauntlet.sql
    /// </summary>
    [ScriptFilterOwnerId(2183)]
    public class GauntletScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Objective groups from PublicEvent objectives 1864/1865/1873/1874.
        // Each row is: veteran ID, normal ID.
        private static readonly uint[][] BossObjectiveGroups =
        {
            new[] { 69254u, 48491u }, // Obj 1864
            new[] { 69255u, 48529u }, // Obj 1865
            new[] { 69308u, 48579u }, // Obj 1873
            new[] { 69305u, 48554u }, // Obj 1874
        };

        private IContentMapInstance owner;
        private readonly HashSet<int> defeatedObjectives = new();

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            this.owner = owner;
            defeatedObjectives.Clear();
        }

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            int objectiveIndex = GetObjectiveIndex(creatureId);
            if (objectiveIndex < 0 || !defeatedObjectives.Add(objectiveIndex))
                return;

            if (defeatedObjectives.Count < BossObjectiveGroups.Length)
                return;

            // All four objectives completed - instance complete.
            if (owner.Match != null)
                owner.Match.MatchFinish();
            else
                owner.OnMatchFinish();
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            defeatedObjectives.Clear();
        }

        private static int GetObjectiveIndex(uint creatureId)
        {
            for (int i = 0; i < BossObjectiveGroups.Length; i++)
            {
                uint[] group = BossObjectiveGroups[i];
                for (int j = 0; j < group.Length; j++)
                {
                    if (group[j] == creatureId)
                        return i;
                }
            }

            return -1;
        }
    }
}
