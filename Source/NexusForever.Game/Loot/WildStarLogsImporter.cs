using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Loot;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Loot
{
    /// <summary>
    /// Imports creature loot data from WildStarLogs reports.
    /// WildStarLogs provides combat logs with ability data, damage tables, and loot tracking.
    /// </summary>
    /// <remarks>
    /// API documentation: https://www.wildstarlogs.com/
    /// 
    /// Report URL format: https://www.wildstarlogs.com/reports/{reportId}
    /// </remarks>
    public class WildStarLogsImporter : Singleton<WildStarLogsImporter>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly HttpClient httpClient;
        private readonly IDatabaseManager databaseManager;
        private readonly IGameTableManager gameTableManager;

        // WildStarLogs API base URL
        private const string WildStarLogsApiBase = "https://www.wildstarlogs.com/api/v2";

        public WildStarLogsImporter(IDatabaseManager databaseManager, IGameTableManager gameTableManager)
        {
            this.databaseManager = databaseManager;
            this.gameTableManager = gameTableManager;
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "NexusForever/1.0");
        }

        /// <summary>
        /// Import creature loot data from a WildStarLogs report.
        /// </summary>
        /// <param name="reportId">The WildStarLogs report ID (e.g., "jgKRFVZMN4rCH2t6")</param>
        public async Task ImportLootDataAsync(string reportId)
        {
            log.Info($"Starting WildStarLogs import for report: {reportId}");

            try
            {
                // Fetch report data
                var reportData = await FetchReportDataAsync(reportId);
                if (reportData == null)
                {
                    log.Error($"Failed to fetch report data for {reportId}");
                    return;
                }

                // Get all fights from the report
                var fights = await FetchFightsAsync(reportId);
                if (fights == null || fights.fights == null)
                {
                    log.Error($"Failed to fetch fights for report {reportId}");
                    return;
                }

                // Process each fight
                int lootEntriesAdded = 0;
                foreach (var fight in fights.fights)
                {
                    if (fight.enemies == null)
                        continue;

                    foreach (var enemy in fight.enemies)
                    {
                        // Skip non-bosses if desired (optional filtering)
                        // if (!fight.boss.HasValue || fight.boss == 0)
                        //     continue;

                        // Fetch loot data for this enemy
                        var lootData = await FetchEnemyLootAsync(reportId, enemy.id);
                        if (lootData?.items != null)
                        {
                            foreach (var item in lootData.items)
                            {
                                var entry = CreateLootEntry((uint)enemy.id, item);
                                if (entry != null)
                                {
                                    await SaveLootEntryAsync(entry);
                                    lootEntriesAdded++;
                                }
                            }
                        }
                    }
                }

                log.Info($"Successfully imported {lootEntriesAdded} loot entries from report {reportId}");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error importing loot data from WildStarLogs report {reportId}");
            }
        }

        private async Task<ReportData> FetchReportDataAsync(string reportId)
        {
            try
            {
                // Try to get JSON data from the report
                string url = $"{WildStarLogsApiBase}/report/{reportId}";
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Try alternative URL format (web page)
                    log.Debug($"API not available, trying web scraping approach for {reportId}");
                    return await FetchReportFromWebAsync(reportId);
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ReportData>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                log.Debug(ex, $"API fetch failed for {reportId}, trying web approach");
                return await FetchReportFromWebAsync(reportId);
            }
        }

        private async Task<ReportData> FetchReportFromWebAsync(string reportId)
        {
            // Fallback: scrape the web page for basic data
            // This is a simplified approach - full implementation would parse HTML
            try
            {
                string url = $"https://www.wildstarlogs.com/reports/{reportId}";
                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Try to find embedded JSON data in the page
                    int jsonStart = content.IndexOf("window.ajaxify");
                    if (jsonStart >= 0)
                    {
                        int dataStart = content.IndexOf("data", jsonStart);
                        if (dataStart >= 0)
                        {
                            // Extract relevant data - simplified
                            log.Info($"Web page fetched for {reportId}, manual data entry may be required");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex, $"Web fetch failed for {reportId}");
            }

            return null;
        }

        private async Task<FightsData> FetchFightsAsync(string reportId)
        {
            try
            {
                string url = $"{WildStarLogsApiBase}/report/{reportId}/fights";
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FightsData>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private async Task<EnemyLootData> FetchEnemyLootAsync(string reportId, int enemyId)
        {
            try
            {
                string url = $"{WildStarLogsApiBase}/report/{reportId}/enemy/{enemyId}/loot";
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EnemyLootData>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private CreatureLootEntryModel CreateLootEntry(uint creatureId, LootItemData itemData)
        {
            // Validate item exists in game tables
            if (itemData.itemId == 0)
                return null;

            Item2Entry itemEntry = gameTableManager.Item.GetEntry(itemData.itemId);
            if (itemEntry == null)
            {
                log.Debug($"Skipping unknown item ID: {itemData.itemId}");
                return null;
            }

            // Calculate drop rate based on kill count and drop count
            float dropRate = 1.0f;
            if (itemData.killCount > 0 && itemData.numDropped > 0)
            {
                dropRate = (float)itemData.numDropped / itemData.killCount;
                dropRate = Math.Clamp(dropRate, 0.001f, 1.0f);
            }

            return new CreatureLootEntryModel
            {
                CreatureId       = creatureId,
                LootGroupId      = itemData.lootRule > 0 ? (uint)itemData.lootRule : 1, // Default to group 1 if not specified
                ItemId           = itemData.itemId,
                Context          = (byte)LootContext.Any,
                SourceConfidence = (byte)LootSourceConfidence.LogObserved,
                MinCount         = (uint)Math.Max(1, itemData.minCount),
                MaxCount         = (uint)Math.Max(1, itemData.maxCount),
                DropRate         = dropRate,
                EvidenceRef      = $"WildStarLogs:item={itemData.itemId},kills={itemData.killCount},drops={itemData.numDropped}",
                Enabled          = true
            };
        }

        private async Task SaveLootEntryAsync(CreatureLootEntryModel entry)
        {
            var worldDb = databaseManager.GetDatabase<WorldDatabase>();
            await Task.Run(() => worldDb.UpsertCreatureLootEntries(new[] { entry }));
        }

        #region JSON Data Models

        private class ReportData
        {
            public string id { get; set; }
            public string title { get; set; }
            public DateTime startTime { get; set; }
            public DateTime endTime { get; set; }
        }

        private class FightsData
        {
            public List<FightData> fights { get; set; }
        }

        private class FightData
        {
            public int id { get; set; }
            public int? boss { get; set; }
            public string name { get; set; }
            public int? friendlyPlayers { get; set; }
            public List<EnemyData> enemies { get; set; }
        }

        private class EnemyData
        {
            public int id { get; set; }
            public string name { get; set; }
            public int? guid { get; set; } // This would be the creature ID
            public int? serverInfo { get; set; }
        }

        private class EnemyLootData
        {
            public List<LootItemData> items { get; set; }
        }

        private class LootItemData
        {
            public uint itemId { get; set; }
            public string name { get; set; }
            public int killCount { get; set; }
            public int numDropped { get; set; }
            public int minCount { get; set; }
            public int maxCount { get; set; }
            public int lootRule { get; set; }
        }

        #endregion
    }
}
