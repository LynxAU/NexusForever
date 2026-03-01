using System.Collections.Immutable;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Reputation
{
    public sealed class FactionManager : Singleton<FactionManager>, IFactionManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private ImmutableDictionary<Faction, IFactionNode> nodes;

        public void Initialise()
        {
            DateTime start = DateTime.Now;
            log.Info("Initialising factions...");

            // Validate Faction2Relationship for duplicate (FactionId0, FactionId1) pairs.
            // FactionNode deduplicates these via GroupBy, but logging the concrete IDs helps
            // identify whether the duplicates are a data error or intentional table redundancy.
            var duplicates = GameTableManager.Instance.Faction2Relationship.Entries
                .Where(e => e != null)
                .GroupBy(e => (e.FactionId0, e.FactionId1))
                .Where(g => g.Count() > 1);
            foreach (var group in duplicates)
                log.Warn($"Faction2Relationship has {group.Count()} duplicate entries for (FactionId0={group.Key.FactionId0}, FactionId1={group.Key.FactionId1}) — first entry (Id={group.First().Id}) will be used.");

            var builder = ImmutableDictionary.CreateBuilder<Faction, IFactionNode>();
            foreach (Faction2Entry entry in GameTableManager.Instance.Faction2.Entries)
                builder.Add((Faction)entry.Id, new FactionNode(entry));

            // link nodes and build the trees
            foreach (IFactionNode node in builder.Values)
            {
                // parent
                builder.TryGetValue((Faction)node.Entry.Faction2IdParent, out IFactionNode parent);

                // children
                var children = new List<IFactionNode>();
                foreach (Faction2Entry entry in GameTableManager.Instance.Faction2.Entries
                    .Where(e => (Faction)e.Faction2IdParent == node.FactionId))
                {
                    builder.TryGetValue((Faction)entry.Id, out IFactionNode child);
                    children.Add(child);
                }

                // relationships
                var relationships = new List<IFactionNode>();
                foreach (IRelationshipNode relationship in node.GetRelationships())
                {
                    builder.TryGetValue((Faction)relationship.Entry.FactionId1, out IFactionNode child);
                    relationships.Add(child);
                }

                node.Link(parent, children, relationships);
            }

            nodes = builder.ToImmutable();

            TimeSpan span = DateTime.Now - start;
            log.Info($"Initialised {nodes.Count} faction(s) in {span.TotalMilliseconds}ms.");
        }

        /// <summary>
        /// Return the <see cref="IFactionNode"/> with the supplied faction id.
        /// </summary>
        public IFactionNode GetFaction(Faction factionId)
        {
            return nodes.TryGetValue(factionId, out IFactionNode node) ? node : null;
        }
    }
}
