using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.World;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NLog;

namespace NexusForever.ContentTool
{
    public class Reporter
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private readonly IGameTableManager _tbl;
        private readonly WorldContext _db;

        public Reporter(IGameTableManager gameTableManager, WorldContext dbContext)
        {
            _tbl = gameTableManager;
            _db = dbContext;
        }

        public async Task GenerateReport()
        {
            log.Info("=== Generating Content Coverage Report ===");

            await ReportLootCoverage();
            await ReportSpawnCoverage();
            await ReportQuestCoverage();
            
            log.Info("=== End of Report ===");
        }

        private async Task ReportLootCoverage()
        {
            log.Info("--- Loot Coverage ---");
            
            var allCreatures = _tbl.Creature2.Entries.ToList();
            var bossTiers = new HashSet<uint> { 3, 4, 5, 8 }; // Typical boss/elite tiers, need verification

            var dbLootCreatures = await _db.CreatureLootEntry
                .Select(e => e.CreatureId)
                .Distinct()
                .ToListAsync();
            
            var dbLootSet = new HashSet<uint>(dbLootCreatures);

            int total = allCreatures.Count;
            int withLoot = allCreatures.Count(c => dbLootSet.Contains(c.Id));
            
            log.Info($"Overall: {withLoot}/{total} creatures have loot ({Percent(withLoot, total)}%)");

            // Categorize by Tier (if 100% accurate) or just Sample Bosses
            var bosses = allCreatures.Where(c => bossTiers.Contains(c.Creature2TierId)).ToList();
            int bossesWithLoot = bosses.Count(c => dbLootSet.Contains(c.Id));
            log.Info($"Bosses/Elites: {bossesWithLoot}/{bosses.Count} have loot ({Percent(bossesWithLoot, bosses.Count)}%)");
        }

        private async Task ReportSpawnCoverage()
        {
            log.Info("--- Spawn Coverage ---");

            var allWorlds = _tbl.World.Entries.ToList();
            var spawnedWorlds = await _db.Entity
                .Select(e => e.World)
                .Distinct()
                .ToListAsync();

            var spawnedSet = new HashSet<ushort>(spawnedWorlds.Select(w => (ushort)w));

            int total = allWorlds.Count;
            int withSpawns = allWorlds.Count(w => spawnedSet.Contains((ushort)w.Id));

            log.Info($"Worlds: {withSpawns}/{total} worlds have spawns defined ({Percent(withSpawns, total)}%)");
            
            if (total - withSpawns > 0)
            {
                var missing = allWorlds.Where(w => !spawnedSet.Contains((ushort)w.Id)).Take(5);
                log.Info($"Missing Worlds Sample: {string.Join(", ", missing.Select(m => m.Id))}");
            }
        }

        private async Task ReportQuestCoverage()
        {
            log.Info("--- Quest Coverage ---");

            var allQuests = _tbl.Quest2.Entries.ToList();
            // Quest implementation is complex (scripted vs DB). 
            // For now, let's just check if they are mentioned in any DB table if applicable, 
            // or just report total count vs TODO.
            
            log.Info($"Total Quests in Client: {allQuests.Count}");
            // Missing: actual implementation check.
        }

        public async Task GenerateSeedData(string outputDir)
        {
            log.Info($"Generating seed data to {outputDir}...");
            Directory.CreateDirectory(outputDir);

            // Placeholder for seed generation logic
            // Example: Generate missing Loot Group linkage from LootPinataInfo.tbl
            
            log.Info("Seeding logic not yet fully implemented.");
        }

        private string Percent(int count, int total)
        {
            if (total == 0) return "0.0";
            return ((double)count / total * 100.0).ToString("F1");
        }
    }
}
