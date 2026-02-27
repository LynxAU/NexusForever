using System.Collections.Immutable;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Loot;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Loot
{
    public sealed class DatabaseLootSourceProvider : Singleton<DatabaseLootSourceProvider>, ILootSourceProvider
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IDatabaseManager databaseManager;
        private readonly IItemManager itemManager;

        public DatabaseLootSourceProvider(
            IDatabaseManager databaseManager,
            IItemManager itemManager)
        {
            this.databaseManager = databaseManager;
            this.itemManager     = itemManager;
        }

        private ImmutableDictionary<uint, ImmutableList<CreatureLootEntryModel>> creatureLootEntries
            = ImmutableDictionary<uint, ImmutableList<CreatureLootEntryModel>>.Empty;

        public void Initialise()
        {
            var entries = databaseManager.GetDatabase<WorldDatabase>()
                .GetCreatureLootEntries()
                .Where(e => e.Enabled)
                .Where(e => e.ItemId != 0u)
                .Where(e => e.MaxCount > 0u)
                .Where(e => e.DropRate > 0f);

            var builder = new Dictionary<uint, List<CreatureLootEntryModel>>();

            foreach (CreatureLootEntryModel entry in entries)
            {
                if (itemManager.GetItemInfo(entry.ItemId) == null)
                {
                    log.Warn($"Skipping creature loot row with unknown itemId {entry.ItemId} for creatureId {entry.CreatureId}.");
                    continue;
                }

                if (!builder.TryGetValue(entry.CreatureId, out List<CreatureLootEntryModel> lootRows))
                {
                    lootRows = [];
                    builder.Add(entry.CreatureId, lootRows);
                }

                lootRows.Add(entry);
            }

            creatureLootEntries = builder.ToImmutableDictionary(
                t => t.Key,
                t => t.Value.ToImmutableList());

            log.Info($"Loaded {creatureLootEntries.Sum(t => t.Value.Count)} creature loot row(s) across {creatureLootEntries.Count} creature(s).");
        }

        public IEnumerable<LootDrop> RollCreatureLoot(uint creatureId, LootContext context = LootContext.Any)
        {
            if (!creatureLootEntries.TryGetValue(creatureId, out ImmutableList<CreatureLootEntryModel> lootRows))
                return Enumerable.Empty<LootDrop>();

            var drops = new List<LootDrop>();

            foreach (CreatureLootEntryModel lootRow in lootRows)
            {
                LootContext rowContext = (LootContext)lootRow.Context;
                if (rowContext != LootContext.Any && rowContext != context)
                    continue;

                double chance = NormaliseDropRate(lootRow.DropRate);
                if (chance <= 0d || Random.Shared.NextDouble() > chance)
                    continue;

                uint minCount = Math.Max(1u, lootRow.MinCount);
                uint maxCount = Math.Max(minCount, lootRow.MaxCount);
                uint count = minCount == maxCount
                    ? minCount
                    : (uint)Random.Shared.Next((int)minCount, (int)maxCount + 1);

                if (count == 0u)
                    continue;

                drops.Add(new LootDrop
                {
                    ItemId           = lootRow.ItemId,
                    Count            = count,
                    SourceConfidence = (LootSourceConfidence)lootRow.SourceConfidence,
                    EvidenceReference = lootRow.EvidenceRef
                });
            }

            return drops;
        }

        private static double NormaliseDropRate(float dropRate)
        {
            // Accept both 0..1 and 0..100 source formats to simplify imports.
            float normalised = dropRate > 1f
                ? dropRate / 100f
                : dropRate;

            return Math.Clamp(normalised, 0f, 1f);
        }
    }
}
