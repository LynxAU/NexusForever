using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

string tableDir = args.Length > 0
    ? args[0]
    : @"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl";

string outputPath = args.Length > 1
    ? args[1]
    : @"C:\Games\Dev\WIldstar\NexusForever\tmp\expedition-data-report.md";

if (!Directory.Exists(tableDir))
{
    Console.Error.WriteLine($"Table directory does not exist: {tableDir}");
    return 1;
}

// PublicEvent IDs for the 8 expeditions (from the MapScript [ScriptFilterOwnerId] annotations)
var expeditionPublicEventIds = new Dictionary<uint, (uint WorldId, string Name)>
{
    { 95u,  (1232u, "Infestation") },
    { 108u, (1319u, "Outpost M-13") },
    { 213u, (1627u, "RageLogic") },
    { 390u, (2149u, "Space Madness") },
    { 446u, (2183u, "The Gauntlet") },
    { 447u, (2188u, "Deep Space Exploration") },
    { 680u, (3180u, "Fragment Zero") },
    { 781u, (3404u, "Evil From The Ether") },
};

GameTable<PublicEventEntry>          peTable    = LoadTable<PublicEventEntry>(tableDir, "PublicEvent.tbl");
GameTable<PublicEventObjectiveEntry> peoTable   = LoadTable<PublicEventObjectiveEntry>(tableDir, "PublicEventObjective.tbl");
GameTable<Creature2Entry>            c2Table    = LoadTable<Creature2Entry>(tableDir, "Creature2.tbl");
GameTable<Creature2DifficultyEntry>  c2DiffTbl  = LoadTable<Creature2DifficultyEntry>(tableDir, "Creature2Difficulty.tbl");
GameTable<Creature2ArcheTypeEntry>   c2ArcheTbl = LoadTable<Creature2ArcheTypeEntry>(tableDir, "Creature2ArcheType.tbl");
GameTable<TargetGroupEntry>          tgTable    = LoadTable<TargetGroupEntry>(tableDir, "TargetGroup.tbl");

Dictionary<uint, Creature2Entry>         creatureById = c2Table.Entries.ToDictionary(e => e.Id);
Dictionary<uint, Creature2DifficultyEntry> diffById    = c2DiffTbl.Entries.ToDictionary(e => e.Id);
Dictionary<uint, Creature2ArcheTypeEntry> archeById   = c2ArcheTbl.Entries.ToDictionary(e => e.Id);
Dictionary<uint, TargetGroupEntry>        tgById      = tgTable.Entries.ToDictionary(e => e.Id);

// Creature2Difficulty: Id field maps to an archetype "eliteness" level
// Creature2ArcheType: Id field maps to Boss/Elite/Normal etc.

// Group objectives by PublicEventId
Dictionary<uint, List<PublicEventObjectiveEntry>> objByEvent = new();
foreach (PublicEventObjectiveEntry obj in peoTable.Entries)
{
    if (!objByEvent.ContainsKey(obj.PublicEventId))
        objByEvent[obj.PublicEventId] = new List<PublicEventObjectiveEntry>();
    objByEvent[obj.PublicEventId].Add(obj);
}

var sb = new System.Text.StringBuilder();
sb.AppendLine("# Expedition Public Event Data Report");
sb.AppendLine();
sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
sb.AppendLine($"Table source: `{tableDir}`");
sb.AppendLine();

// Print Creature2Difficulty mapping
sb.AppendLine("## Creature2Difficulty Table (eliteness levels)");
sb.AppendLine("| Id | LocalizedTextIdTitle | RankValue | GroupValue |");
sb.AppendLine("|---:|---:|---:|---:|");
foreach (Creature2DifficultyEntry d in c2DiffTbl.Entries.OrderBy(x => x.Id))
    sb.AppendLine($"| {d.Id} | {d.LocalizedTextIdTitle} | {d.RankValue} | {d.GroupValue} |");
sb.AppendLine();

// Print Creature2ArcheType mapping
sb.AppendLine("## Creature2ArcheType Table (class/role types)");
sb.AppendLine("| Id | Icon |");
sb.AppendLine("|---:|---|");
foreach (Creature2ArcheTypeEntry a in c2ArcheTbl.Entries.OrderBy(x => x.Id))
    sb.AppendLine($"| {a.Id} | {a.Icon} |");
sb.AppendLine();

foreach (KeyValuePair<uint, (uint WorldId, string Name)> kvp in expeditionPublicEventIds)
{
    uint peId = kvp.Key;
    uint worldId = kvp.Value.WorldId;
    string name = kvp.Value.Name;

    PublicEventEntry? pe = peTable.Entries.FirstOrDefault(e => e.Id == peId);

    sb.AppendLine($"## {name}");
    sb.AppendLine($"- WorldId: {worldId}");
    sb.AppendLine($"- PublicEventId: {peId}");
    if (pe != null)
    {
        sb.AppendLine($"- PublicEvent.WorldId (tbl): {pe.WorldId}");
        sb.AppendLine($"- PublicEvent.WorldZoneId: {pe.WorldZoneId}");
        sb.AppendLine($"- PublicEvent.Type: {pe.PublicEventTypeEnum}");
        sb.AppendLine($"- PublicEvent.MinPlayerLevel: {pe.MinPlayerLevel}");
        sb.AppendLine($"- PublicEvent.FailureTimeMs: {pe.FailureTimeMs}");
        sb.AppendLine($"- PublicEvent.LocalizedTextIdName: {pe.LocalizedTextIdName}");
    }
    else
    {
        sb.AppendLine($"- WARNING: No PublicEventEntry found for id {peId}");
    }
    sb.AppendLine();

    if (objByEvent.TryGetValue(peId, out List<PublicEventObjectiveEntry>? objectives))
    {
        sb.AppendLine($"### Objectives ({objectives.Count} total)");
        sb.AppendLine();
        sb.AppendLine("| ObjId | Type | TypeNum | ObjectId | Count | FailMs | Team | MedalPts | DisplayOrder | ParentId | LocalizedTextId | LocalizedTextIdShort |");
        sb.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (PublicEventObjectiveEntry obj in objectives.OrderBy(o => o.DisplayOrder).ThenBy(o => o.Id))
        {
            sb.AppendLine($"| {obj.Id} | {obj.PublicEventObjectiveTypeEnum} | {(int)obj.PublicEventObjectiveTypeEnum} | {obj.ObjectId} | {obj.Count} | {obj.FailureTimeMs} | {obj.PublicEventTeamId} | {obj.MedalPointValue} | {obj.DisplayOrder} | {obj.PublicEventObjectiveIdParent} | {obj.LocalizedTextId} | {obj.LocalizedTextIdShort} |");
        }
        sb.AppendLine();

        // Resolve creature/target group references
        var killObjectives = objectives.Where(o => o.ObjectId != 0).OrderBy(o => o.Id).ToList();
        if (killObjectives.Any())
        {
            sb.AppendLine("### Resolved ObjectIds");
            sb.AppendLine();

            foreach (PublicEventObjectiveEntry obj in killObjectives)
            {
                string objTypeName = obj.PublicEventObjectiveTypeEnum.ToString();

                if (obj.PublicEventObjectiveTypeEnum is
                    PublicEventObjectiveType.KillTargetGroup or
                    PublicEventObjectiveType.KillClusterTargetGroup or
                    PublicEventObjectiveType.ActivateTargetGroup or
                    PublicEventObjectiveType.ActivateTargetGroupChecklist)
                {
                    sb.AppendLine($"#### Objective {obj.Id} | {objTypeName} | TargetGroupId={obj.ObjectId} | Count={obj.Count}");
                    if (tgById.TryGetValue(obj.ObjectId, out TargetGroupEntry? tg))
                    {
                        sb.AppendLine($"  - TargetGroup LocalizedTextId={tg.LocalizedTextIdDisplayString}, Type={tg.Type}");
                        sb.AppendLine($"  - DataEntries (Creature2 IDs): {string.Join(", ", tg.DataEntries.Where(x => x != 0))}");
                        foreach (uint cid in tg.DataEntries.Where(x => x != 0))
                        {
                            string desc = FormatCreature(cid, creatureById, diffById, archeById);
                            sb.AppendLine($"    - Creature2Id {cid}: {desc}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"  - TargetGroup {obj.ObjectId} NOT FOUND in TargetGroup.tbl");
                    }
                    sb.AppendLine();
                }
                else if (obj.PublicEventObjectiveTypeEnum is
                    PublicEventObjectiveType.KillEventUnit or
                    PublicEventObjectiveType.KillEventObjectiveUnit or
                    PublicEventObjectiveType.KillClusterEventUnit or
                    PublicEventObjectiveType.KillClusterEventObjectiveUnit or
                    PublicEventObjectiveType.DefendObjectiveUnit)
                {
                    sb.AppendLine($"#### Objective {obj.Id} | {objTypeName} | Creature2Id={obj.ObjectId} | Count={obj.Count}");
                    string desc = FormatCreature(obj.ObjectId, creatureById, diffById, archeById);
                    sb.AppendLine($"  - {desc}");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine($"#### Objective {obj.Id} | {objTypeName} | ObjectId={obj.ObjectId} | Count={obj.Count}");
                    sb.AppendLine();
                }
            }
        }
    }
    else
    {
        sb.AppendLine("(No objectives found for this public event id)");
    }

    sb.AppendLine("---");
    sb.AppendLine();
}

// Raw CSV export
sb.AppendLine("## Raw Objective CSV");
sb.AppendLine("```csv");
sb.AppendLine("ExpeditionName,PublicEventId,ObjId,Type,TypeNum,ObjectId,Count,FailMs,Team,MedalPts,DisplayOrder,ParentId,LocalizedTextId,LocalizedTextIdShort");
foreach (KeyValuePair<uint, (uint WorldId, string Name)> kvp in expeditionPublicEventIds)
{
    uint peId = kvp.Key;
    string eName = kvp.Value.Name;
    if (!objByEvent.TryGetValue(peId, out List<PublicEventObjectiveEntry>? objs)) continue;
    foreach (PublicEventObjectiveEntry o in objs.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id))
    {
        sb.AppendLine($"{eName},{peId},{o.Id},{o.PublicEventObjectiveTypeEnum},{(int)o.PublicEventObjectiveTypeEnum},{o.ObjectId},{o.Count},{o.FailureTimeMs},{o.PublicEventTeamId},{o.MedalPointValue},{o.DisplayOrder},{o.PublicEventObjectiveIdParent},{o.LocalizedTextId},{o.LocalizedTextIdShort}");
    }
}
sb.AppendLine("```");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, sb.ToString());

Console.WriteLine($"Wrote report: {outputPath}");
return 0;

static string FormatCreature(uint cid, Dictionary<uint, Creature2Entry> creatureById,
    Dictionary<uint, Creature2DifficultyEntry> diffById,
    Dictionary<uint, Creature2ArcheTypeEntry> archeById)
{
    if (!creatureById.TryGetValue(cid, out Creature2Entry? c2))
        return $"NOT FOUND in Creature2.tbl";

    string diffName = diffById.TryGetValue(c2.Creature2DifficultyId, out Creature2DifficultyEntry? diff)
        ? $"DiffId={c2.Creature2DifficultyId}(RankValue={diff.RankValue})"
        : $"DiffId={c2.Creature2DifficultyId}";

    string archeName = archeById.TryGetValue(c2.Creature2ArcheTypeId, out Creature2ArcheTypeEntry? arche)
        ? $"ArcheType={c2.Creature2ArcheTypeId}(Icon={arche.Icon})"
        : $"ArcheTypeId={c2.Creature2ArcheTypeId}";

    return $"LocalizedTextIdName={c2.LocalizedTextIdName}, MinLvl={c2.MinLevel}, MaxLvl={c2.MaxLevel}, {diffName}, {archeName}, FactionId={c2.FactionId}";
}

static GameTable<T> LoadTable<T>(string tableDir, string fileName) where T : class, new()
{
    string path = Path.Combine(tableDir, fileName);
    if (!File.Exists(path))
        throw new FileNotFoundException($"Missing table file: {path}");

    using FileStream stream = File.OpenRead(path);
    return new GameTable<T>(stream);
}
