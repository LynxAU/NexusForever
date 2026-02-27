using System.Collections.Immutable;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Loot;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Loot
{
    public sealed class DatabaseLootSourceProvider : Singleton<DatabaseLootSourceProvider>, ILootSourceProvider
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IDatabaseManager databaseManager;
        private readonly IItemManager itemManager;
        private readonly IGameTableManager gameTableManager;

        public DatabaseLootSourceProvider(
            IDatabaseManager databaseManager,
            IItemManager itemManager,
            IGameTableManager gameTableManager)
        {
            this.databaseManager = databaseManager;
            this.itemManager     = itemManager;
            this.gameTableManager = gameTableManager;
        }

        private ImmutableDictionary<uint, ImmutableList<CreatureLootEntryModel>> creatureLootEntries
            = ImmutableDictionary<uint, ImmutableList<CreatureLootEntryModel>>.Empty;

        public void Initialise()
        {
            WorldDatabase worldDatabase = databaseManager.GetDatabase<WorldDatabase>();

            EnsureBootstrappedCreatureLoot(worldDatabase);

            var entries = worldDatabase
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

            LogLootCoverage();
        }

        private void LogLootCoverage()
        {
            // Get all creatures from the database
            WorldDatabase worldDatabase = databaseManager.GetDatabase<WorldDatabase>();
            var allCreatures = worldDatabase.GetAllCreatureIds().ToList();
            
            if (allCreatures.Count == 0)
            {
                log.Debug("No creatures found in database for loot coverage analysis.");
                return;
            }

            int creaturesWithLoot = creatureLootEntries.Count;
            int creaturesWithoutLoot = allCreatures.Count - creaturesWithLoot;
            double coveragePercent = (double)creaturesWithLoot / allCreatures.Count * 100.0;

            log.Info($"=== Loot Coverage Report ===");
            log.Info($"Total creatures in DB: {allCreatures.Count}");
            log.Info($"Creatures with loot defined: {creaturesWithLoot}");
            log.Info($"Creatures without loot: {creaturesWithoutLoot}");
            log.Info($"Coverage: {coveragePercent:F1}%");

            if (coveragePercent < 50.0)
            {
                log.Warn($"Loot coverage is low ({coveragePercent:F1}%). Consider adding more loot mappings.");
            }

            // Show sample of creatures without loot (first 10)
            var creaturesMissingLoot = allCreatures
                .Where(c => !creatureLootEntries.ContainsKey(c))
                .Take(10)
                .ToList();

            if (creaturesMissingLoot.Count > 0)
            {
                log.Debug($"Sample creatures without loot (first 10): {string.Join(", ", creaturesMissingLoot)}");
            }
        }

        private void EnsureBootstrappedCreatureLoot(WorldDatabase worldDatabase)
        {
            // Check specifically for ClientReverse rows — CommunityDatabase rows seeded via migration should
            // not prevent LootPinataInfo.tbl bootstrapping from running on a fresh database.
            if (worldDatabase.GetCreatureLootEntries().Any(e => e.SourceConfidence == (byte)LootSourceConfidence.ClientReverse))
                return;

            var lootPinataRows = gameTableManager.LootPinataInfo.Entries
                .Where(r => r.Item2Id != 0u)
                .Where(r => r.Creature2IdChest != 0u)
                .GroupBy(r => r.Creature2IdChest)
                .ToList();

            var seedRows = new List<CreatureLootEntryModel>();
            uint seedPinataRows = 0u;
            uint seedLootSpellRows = 0u;

            foreach (var creatureRows in lootPinataRows)
            {
                float positiveMassTotal = creatureRows
                    .Where(r => r.Mass > 0f)
                    .Sum(r => r.Mass);

                uint rowCount = (uint)creatureRows.Count();

                foreach (LootPinataInfoEntry row in creatureRows)
                {
                    if (itemManager.GetItemInfo(row.Item2Id) == null)
                        continue;

                    float dropRate = positiveMassTotal > 0f
                        ? row.Mass / positiveMassTotal
                        : 1f / rowCount;

                    seedRows.Add(new CreatureLootEntryModel
                    {
                        CreatureId       = row.Creature2IdChest,
                        LootGroupId      = row.Id,
                        ItemId           = row.Item2Id,
                        Context          = (byte)LootContext.Any,
                        SourceConfidence = (byte)LootSourceConfidence.ClientReverse,
                        MinCount         = 1u,
                        MaxCount         = 1u,
                        DropRate         = Math.Clamp(dropRate, 0f, 1f),
                        EvidenceRef      = $"LootPinataInfo.tbl:{row.Id}",
                        Enabled          = true
                    });

                    seedPinataRows++;
                }
            }

            // LootSpell does not provide explicit item semantics in all cases.
            // Seed only rows where we can deterministically resolve a single Item2 id, and keep disabled by default.
            foreach (var creatureRows in gameTableManager.LootSpell.Entries
                .Where(r => r.Creature2Id != 0u)
                .GroupBy(r => r.Creature2Id))
            {
                uint candidateCount = 0u;
                var resolvedRows = new List<(LootSpellEntry Entry, uint ItemId)>();

                foreach (LootSpellEntry row in creatureRows)
                {
                    uint[] candidates = [row.Data, row.DataValue];
                    uint[] validItemCandidates = candidates
                        .Where(id => id != 0u)
                        .Distinct()
                        .Where(id => itemManager.GetItemInfo(id) != null)
                        .ToArray();

                    if (validItemCandidates.Length != 1)
                        continue;

                    resolvedRows.Add((row, validItemCandidates[0]));
                    candidateCount++;
                }

                if (candidateCount == 0u)
                    continue;

                float defaultRate = 1f / candidateCount;
                foreach ((LootSpellEntry entry, uint itemId) in resolvedRows)
                {
                    seedRows.Add(new CreatureLootEntryModel
                    {
                        CreatureId       = entry.Creature2Id,
                        LootGroupId      = entry.Id,
                        ItemId           = itemId,
                        Context          = (byte)LootContext.Any,
                        SourceConfidence = (byte)LootSourceConfidence.Inferred,
                        MinCount         = 1u,
                        MaxCount         = 1u,
                        DropRate         = defaultRate,
                        EvidenceRef      = $"LootSpell.tbl:{entry.Id} type={entry.LootSpellTypeEnum} data={entry.Data} dataValue={entry.DataValue}",
                        Enabled          = false
                    });

                    seedLootSpellRows++;
                }
            }

            if (seedRows.Count == 0)
            {
                log.Warn("Creature loot bootstrap found no usable LootPinataInfo rows.");
                return;
            }

            worldDatabase.UpsertCreatureLootEntries(seedRows);
            log.Info($"Bootstrapped {seedRows.Count} creature loot row(s): {seedPinataRows} from LootPinataInfo.tbl, {seedLootSpellRows} inferred from LootSpell.tbl.");
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
