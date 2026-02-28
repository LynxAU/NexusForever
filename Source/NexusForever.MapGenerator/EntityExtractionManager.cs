using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nexus.Archive;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO;
using NexusForever.IO.Area;
using LocalGameTableManager = NexusForever.MapGenerator.GameTable.GameTableManager;
using NexusForever.Shared;
using NLog;

namespace NexusForever.MapGenerator
{
    /// <summary>
    /// Extracts NPC spawn data from client .area files (CURT chunks) and writes it as CSV.
    /// Output file entity_spawns.csv has one row per spawn instance:
    ///   WorldId, GridX, GridY, CreatureId, X, Y, Z, InCreature2
    /// </summary>
    public sealed class EntityExtractionManager : Singleton<EntityExtractionManager>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private string outputDir;
        private GameTable<Creature2Entry> creature2;

        public void Initialise(string outputDir)
        {
            this.outputDir = outputDir;
            creature2 = LoadGameTable<Creature2Entry>("Creature2.tbl");

            log.Info("Extracting entity spawn data from .area files...");
            DumpAll();
        }

        private void DumpAll()
        {
            string spawnPath = Path.Combine(outputDir, "entity_spawns.csv");
            string propPath  = Path.Combine(outputDir, "prop_dump.csv");

            using var spawnWriter = new StreamWriter(spawnPath, false, Encoding.UTF8);
            using var propWriter  = new StreamWriter(propPath,  false, Encoding.UTF8);

            spawnWriter.WriteLine("WorldId,WorldAsset,GridX,GridY,CreatureId,X,Y,Z,InCreature2");
            propWriter.WriteLine("WorldId,WorldAsset,GridX,GridY,ChunkType,CellX,CellY,SizeBytes,HexPrefix");

            var regex = new Regex(@"[\w]+\.([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})\.area");

            int totalSpawns = 0;

            foreach (WorldEntry worldEntry in LocalGameTableManager.Instance.World.Entries
                .Where(e => e.AssetPath != string.Empty)
                .GroupBy(e => e.AssetPath)
                .Select(g => g.First()))
            {
                string path = Path.Combine(worldEntry.AssetPath, "*.*.area");
                foreach (IArchiveFileEntry grid in ArchiveManager.Instance.MainArchive.IndexFile.GetFiles(path))
                {
                    if (grid.FileName.Contains("_low", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Match match = regex.Match(grid.FileName);
                    if (!match.Success)
                        continue;

                    byte gridX = byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
                    byte gridY = byte.Parse(match.Groups[2].Value, NumberStyles.HexNumber);

                    try
                    {
                        using Stream stream = ArchiveManager.Instance.MainArchive.OpenFileStream(grid);
                        var areaFile = new AreaFile(stream);

                        foreach (IReadable areaChunk in areaFile.Chunks)
                        {
                            switch (areaChunk)
                            {
                                case Curt curt:
                                    foreach (Curt.Entry entry in curt.Entries)
                                    {
                                        if (entry.Positions.Count == 0)
                                            continue;

                                        bool known = creature2.GetEntry(entry.CreatureId) != null;
                                        foreach (var pos in entry.Positions)
                                        {
                                            spawnWriter.WriteLine(
                                                $"{worldEntry.Id},{worldEntry.AssetPath},{gridX},{gridY}," +
                                                $"{entry.CreatureId}," +
                                                $"{pos.X:F4},{pos.Y:F4},{pos.Z:F4}," +
                                                $"{(known ? "YES" : "no")}");
                                            totalSpawns++;
                                        }
                                    }
                                    break;

                                case Prop prop when prop.RawData != null:
                                    propWriter.WriteLine(
                                        $"{worldEntry.Id},{worldEntry.AssetPath},{gridX},{gridY}," +
                                        $"Prop,,," +
                                        $"{prop.RawData.Length}," +
                                        $"{Convert.ToHexString(prop.RawData.Length <= 256 ? prop.RawData : prop.RawData[..256])}");
                                    break;

                                case Chnk chnk:
                                    foreach (ChnkCell cell in chnk.Cells.Where(c => c != null))
                                    {
                                        foreach (IReadable cellChunk in cell.Chunks)
                                        {
                                            if (cellChunk is CellProp cellProp && cellProp.RawData != null)
                                            {
                                                propWriter.WriteLine(
                                                    $"{worldEntry.Id},{worldEntry.AssetPath},{gridX},{gridY}," +
                                                    $"CellProp,{cell.X},{cell.Y}," +
                                                    $"{cellProp.RawData.Length}," +
                                                    $"{Convert.ToHexString(cellProp.RawData.Length <= 64 ? cellProp.RawData : cellProp.RawData[..64])}");
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log.Warn($"Error reading {grid.FileName}: {e.Message}");
                    }
                }
            }

            log.Info($"Spawn extraction complete: {totalSpawns:N0} spawn instances written to {spawnPath}");
            log.Info($"PROP dump written to {propPath}");
        }

        private GameTable<T> LoadGameTable<T>(string name) where T : class, new()
        {
            string filePath = Path.Combine("DB", name);
            if (ArchiveManager.Instance.MainArchive.IndexFile.FindEntry(filePath) is not IArchiveFileEntry file)
                throw new FileNotFoundException(name);

            using Stream archiveStream = ArchiveManager.Instance.MainArchive.OpenFileStream(file);
            using var ms = new MemoryStream();
            archiveStream.CopyTo(ms);
            ms.Position = 0;
            return new GameTable<T>(ms);
        }
    }
}
