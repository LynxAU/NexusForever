using System.Text;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

string tableDir = args.Length > 0
    ? args[0]
    : @"C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl";

string outputPath = args.Length > 1
    ? args[1]
    : @"C:\Games\Dev\WIldstar\NexusForever\tmp\spell-data-driven-report.md";

if (!Directory.Exists(tableDir))
{
    Console.Error.WriteLine($"Table directory does not exist: {tableDir}");
    return 1;
}

GameTable<Spell4EffectsEntry> spell4Effects = LoadTable<Spell4EffectsEntry>(tableDir, "Spell4Effects.tbl");
GameTable<Spell4Entry> spell4 = LoadTable<Spell4Entry>(tableDir, "Spell4.tbl");
GameTable<Spell4BaseEntry> spell4Base = LoadTable<Spell4BaseEntry>(tableDir, "Spell4Base.tbl");
GameTable<Spell4CCConditionsEntry> spell4CcConditions = LoadTable<Spell4CCConditionsEntry>(tableDir, "Spell4CCConditions.tbl");
GameTable<CCStatesEntry> ccStates = LoadTable<CCStatesEntry>(tableDir, "CCStates.tbl");
GameTable<Spell4StackGroupEntry> spell4StackGroups = LoadTable<Spell4StackGroupEntry>(tableDir, "Spell4StackGroup.tbl");
GameTable<SpellEffectTypeEntry> spellEffectTypes = LoadTable<SpellEffectTypeEntry>(tableDir, "SpellEffectType.tbl");

Dictionary<uint, Spell4Entry> spellById = spell4.Entries.ToDictionary(e => e.Id, e => e);
Dictionary<uint, Spell4BaseEntry> baseById = spell4Base.Entries.ToDictionary(e => e.Id, e => e);
Dictionary<uint, Spell4StackGroupEntry> stackById = spell4StackGroups.Entries.ToDictionary(e => e.Id, e => e);
Dictionary<uint, CCStatesEntry> ccStateById = ccStates.Entries.ToDictionary(e => e.Id, e => e);
Dictionary<uint, SpellEffectTypeEntry> spellEffectTypeById = spellEffectTypes.Entries.ToDictionary(e => e.Id, e => e);

List<Spell4EffectsEntry> effects = spell4Effects.Entries.ToList();
int totalEffects = effects.Count;
int uniqueEffectTypes = effects.Select(e => e.EffectType).Distinct().Count();

bool IsPeriodic(Spell4EffectsEntry e) => e.TickTime > 0u && e.DurationTime >= e.TickTime;

var periodicDamage = effects.Where(e => e.EffectType == SpellEffectType.Damage && IsPeriodic(e)).ToList();
var periodicHeal = effects.Where(e => e.EffectType == SpellEffectType.Heal && IsPeriodic(e)).ToList();
var periodicHealShields = effects.Where(e => e.EffectType == SpellEffectType.HealShields && IsPeriodic(e)).ToList();

var ccSetRows = effects.Where(e => e.EffectType == SpellEffectType.CCStateSet).ToList();
var ccBreakRows = effects.Where(e => e.EffectType == SpellEffectType.CCStateBreak).ToList();
var dispelRows = effects.Where(e => e.EffectType == SpellEffectType.SpellDispel).ToList();
var procRows = effects.Where(e => e.EffectType == SpellEffectType.Proc).ToList();

var ccSetByState = ccSetRows
    .GroupBy(r => r.DataBits00)
    .OrderByDescending(g => g.Count())
    .ToList();

var ccBreakByState = ccBreakRows
    .GroupBy(r => r.DataBits00)
    .OrderByDescending(g => g.Count())
    .ToList();

int ccBreakSingleStateRows = ccBreakRows.Count(r => Enum.IsDefined(typeof(CCState), (int)r.DataBits00));
int ccBreakMaskRows = ccBreakRows.Count(r => !Enum.IsDefined(typeof(CCState), (int)r.DataBits00) && DecodeCCMask(r.DataBits00).Count > 0);
int ccBreakUnknownRows = ccBreakRows.Count - ccBreakSingleStateRows - ccBreakMaskRows;

var dispelByBits = dispelRows
    .GroupBy(r => new { r.DataBits00, r.DataBits01, r.DataBits02, r.DataBits03, r.DataBits04, r.Flags, r.TargetFlags })
    .OrderByDescending(g => g.Count())
    .Take(20)
    .ToList();

var procByBits = procRows
    .GroupBy(r => new { r.DataBits00, r.DataBits01, r.DataBits02, r.DataBits03, r.DataBits04, r.Flags, r.TargetFlags, r.TickTime, r.DurationTime })
    .OrderByDescending(g => g.Count())
    .Take(25)
    .ToList();

var periodicFlagDistribution = effects
    .Where(IsPeriodic)
    .GroupBy(e => new { e.EffectType, e.Flags, e.TargetFlags })
    .OrderByDescending(g => g.Count())
    .Take(20)
    .ToList();

var periodicByTickDuration = effects
    .Where(IsPeriodic)
    .GroupBy(e => new { e.EffectType, e.TickTime, e.DurationTime })
    .OrderByDescending(g => g.Count())
    .Take(20)
    .ToList();

var spellIdsWithEffects = effects.Select(e => e.SpellId).Distinct().ToHashSet();
var spellsTouched = spellById.Values.Where(s => spellIdsWithEffects.Contains(s.Id)).ToList();

var spellsWithStackGroup = spellsTouched
    .Where(s => s.Spell4StackGroupId != 0u)
    .ToList();

var periodicSpells = effects.Where(IsPeriodic).Select(e => e.SpellId).Distinct().ToHashSet();
var periodicSpellsWithStackGroup = spellById.Values
    .Where(s => periodicSpells.Contains(s.Id) && s.Spell4StackGroupId != 0u)
    .ToList();

int buffClassId = (int)SpellClass.BuffDispellable;
int buffNonDispellableClassId = (int)SpellClass.BuffNonDispellable;
int debuffClassId = (int)SpellClass.DebuffDispellable;
int debuffNonDispellableClassId = (int)SpellClass.DebuffNonDispellable;

var periodicBySpellClass = spellById.Values
    .Where(s => periodicSpells.Contains(s.Id))
    .Select(s => ResolveSpellClass(s, baseById))
    .GroupBy(s => s)
    .OrderByDescending(g => g.Count())
    .ToList();

HashSet<uint> referencedCcConditionIds = spell4.Entries
    .SelectMany(s => new[] { s.Spell4CCConditionsIdCaster, s.Spell4CCConditionsIdTarget })
    .Where(id => id != 0u)
    .ToHashSet();

var referencedCcConditions = spell4CcConditions.Entries
    .Where(c => referencedCcConditionIds.Contains(c.Id))
    .ToList();

var ccStateMaskUsage = referencedCcConditions
    .Where(c => c.CcStateMask != 0u)
    .GroupBy(c => new { c.CcStateMask, c.CcStateFlagsRequired })
    .OrderByDescending(g => g.Count())
    .Take(20)
    .ToList();

var sb = new StringBuilder();
sb.AppendLine("# Spell Data-Driven Report");
sb.AppendLine();
sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
sb.AppendLine($"Table source: `{tableDir}`");
sb.AppendLine();
sb.AppendLine("## Coverage Snapshot");
sb.AppendLine($"- Spell4Effects rows: **{totalEffects}**");
sb.AppendLine($"- Unique effect types present: **{uniqueEffectTypes}**");
sb.AppendLine($"- Periodic Damage rows (`Damage` with tick/duration): **{periodicDamage.Count}**");
sb.AppendLine($"- Periodic Heal rows (`Heal` with tick/duration): **{periodicHeal.Count}**");
sb.AppendLine($"- Periodic Shield-Heal rows (`HealShields` with tick/duration): **{periodicHealShields.Count}**");
sb.AppendLine($"- CC apply rows (`CCStateSet`): **{ccSetRows.Count}**");
sb.AppendLine($"- CC break rows (`CCStateBreak`): **{ccBreakRows.Count}**");
sb.AppendLine($"- Dispel rows (`SpellDispel`): **{dispelRows.Count}**");
sb.AppendLine($"- Proc rows (`Proc`): **{procRows.Count}**");
sb.AppendLine($"- Referenced CC condition rows (`Spell4CCConditions` used by Spell4): **{referencedCcConditions.Count}**");
sb.AppendLine();

sb.AppendLine("## Effect Metadata (from SpellEffectType.tbl)");
sb.AppendLine("| EffectType | Id | Flags | DataTypes (00..09) |");
sb.AppendLine("|---|---:|---:|---|");
foreach (SpellEffectType type in new[]
         {
             SpellEffectType.Damage,
             SpellEffectType.Heal,
             SpellEffectType.HealShields,
             SpellEffectType.CCStateSet,
             SpellEffectType.CCStateBreak,
             SpellEffectType.SpellDispel,
             SpellEffectType.UnitPropertyModifier
         })
{
    uint id = (uint)type;
    if (!spellEffectTypeById.TryGetValue(id, out SpellEffectTypeEntry entry))
        continue;

    string dataTypes = string.Join(",", new[]
    {
        entry.DataType00, entry.DataType01, entry.DataType02, entry.DataType03, entry.DataType04,
        entry.DataType05, entry.DataType06, entry.DataType07, entry.DataType08, entry.DataType09
    }.Select(DecodeDataType));

    sb.AppendLine($"| {type} | {id} | {entry.Flags} | {dataTypes} |");
}
sb.AppendLine();

sb.AppendLine("## Periodic Tick Patterns (Top 20)");
sb.AppendLine("| EffectType | Tick(ms) | Duration(ms) | Rows |");
sb.AppendLine("|---|---:|---:|---:|");
foreach (var g in periodicByTickDuration)
    sb.AppendLine($"| {g.Key.EffectType} | {g.Key.TickTime} | {g.Key.DurationTime} | {g.Count()} |");
sb.AppendLine();

sb.AppendLine("## Periodic Flags/Targets (Top 20)");
sb.AppendLine("| EffectType | Flags | TargetFlags | Rows |");
sb.AppendLine("|---|---:|---:|---:|");
foreach (var g in periodicFlagDistribution)
    sb.AppendLine($"| {g.Key.EffectType} | {g.Key.Flags} | {g.Key.TargetFlags} | {g.Count()} |");
sb.AppendLine();

sb.AppendLine("## CCStateSet Distribution");
sb.AppendLine("| DataBits00 | StateName | Rows | DefaultDRId |");
sb.AppendLine("|---:|---|---:|---:|");
foreach (var g in ccSetByState.Take(24))
{
    string stateName = Enum.IsDefined(typeof(CCState), (int)g.Key) ? ((CCState)g.Key).ToString() : "Unknown";
    uint drId = ccStateById.TryGetValue(g.Key, out CCStatesEntry stateEntry) ? stateEntry.CcStateDiminishingReturnsId : 0u;
    sb.AppendLine($"| {g.Key} | {stateName} | {g.Count()} | {drId} |");
}
sb.AppendLine();

sb.AppendLine("## CCStateBreak Distribution");
sb.AppendLine("| DataBits00 | Interpretation | Rows |");
sb.AppendLine("|---:|---|---:|");
foreach (var g in ccBreakByState.Take(24))
{
    string interpretation;
    if (Enum.IsDefined(typeof(CCState), (int)g.Key))
        interpretation = ((CCState)g.Key).ToString();
    else
    {
        List<string> maskStates = DecodeCCMask(g.Key);
        interpretation = maskStates.Count > 0
            ? $"Mask[{string.Join(",", maskStates)}]"
            : "Unknown";
    }

    sb.AppendLine($"| {g.Key} | {interpretation} | {g.Count()} |");
}
sb.AppendLine();

sb.AppendLine("## CCStateBreak Shape");
sb.AppendLine($"- Single-state payload rows: **{ccBreakSingleStateRows}**");
sb.AppendLine($"- Mask payload rows: **{ccBreakMaskRows}**");
sb.AppendLine($"- Unknown payload rows: **{ccBreakUnknownRows}**");
sb.AppendLine();

sb.AppendLine("## Stack Group Signals");
sb.AppendLine($"- Spells with a stack group id: **{spellsWithStackGroup.Count}**");
sb.AppendLine($"- Periodic spells with stack group id: **{periodicSpellsWithStackGroup.Count}**");
sb.AppendLine();
sb.AppendLine("| StackTypeEnum | StackCap | SpellCount |");
sb.AppendLine("|---:|---:|---:|");
foreach (var g in spellsWithStackGroup
             .Where(s => stackById.ContainsKey(s.Spell4StackGroupId))
             .GroupBy(s => new
             {
                 stackById[s.Spell4StackGroupId].StackTypeEnum,
                 stackById[s.Spell4StackGroupId].StackCap
             })
             .OrderByDescending(g => g.Count())
             .Take(20))
{
    sb.AppendLine($"| {g.Key.StackTypeEnum} | {g.Key.StackCap} | {g.Count()} |");
}
sb.AppendLine();

sb.AppendLine("## SpellDispel Payload Patterns (Top 20)");
sb.AppendLine("| DataBits00 | DataBits01 | DataBits02 | DataBits03 | DataBits04 | Flags | TargetFlags | Rows |");
sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");
foreach (var g in dispelByBits)
    sb.AppendLine($"| {g.Key.DataBits00} | {g.Key.DataBits01} | {g.Key.DataBits02} | {g.Key.DataBits03} | {g.Key.DataBits04} | {g.Key.Flags} | {g.Key.TargetFlags} | {g.Count()} |");
sb.AppendLine();

sb.AppendLine("## Proc Payload Patterns (Top 25)");
sb.AppendLine("| DataBits00 | DataBits01 | DataBits02 | DataBits03 | DataBits04 | Flags | TargetFlags | Tick(ms) | Duration(ms) | Rows |");
sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
foreach (var g in procByBits)
    sb.AppendLine($"| {g.Key.DataBits00} | {g.Key.DataBits01} | {g.Key.DataBits02} | {g.Key.DataBits03} | {g.Key.DataBits04} | {g.Key.Flags} | {g.Key.TargetFlags} | {g.Key.TickTime} | {g.Key.DurationTime} | {g.Count()} |");
sb.AppendLine();

sb.AppendLine("## Periodic SpellClass Mix");
sb.AppendLine("| SpellClass (raw) | ClassBucket | SpellCount |");
sb.AppendLine("|---:|---|---:|");
foreach (var g in periodicBySpellClass)
{
    string bucket = "Other";
    if (g.Key == buffClassId)
        bucket = "BuffDispellable";
    else if (g.Key == buffNonDispellableClassId)
        bucket = "BuffNonDispellable";
    else if (g.Key == debuffClassId)
        bucket = "DebuffDispellable";
    else if (g.Key == debuffNonDispellableClassId)
        bucket = "DebuffNonDispellable";
    sb.AppendLine($"| {g.Key} | {bucket} | {g.Count()} |");
}
sb.AppendLine();

sb.AppendLine("## CC Cast-Condition Masks (Top 20)");
sb.AppendLine("| CcStateMask(hex) | DecodedMaskStates | Required(hex) | DecodedRequiredStates | Rows |");
sb.AppendLine("|---|---|---|---|---:|");
foreach (var g in ccStateMaskUsage)
{
    string maskStates = string.Join(",", DecodeCCMask(g.Key.CcStateMask));
    string requiredStates = string.Join(",", DecodeCCMask(g.Key.CcStateFlagsRequired));
    sb.AppendLine($"| 0x{g.Key.CcStateMask:X8} | {maskStates} | 0x{g.Key.CcStateFlagsRequired:X8} | {requiredStates} | {g.Count()} |");
}
sb.AppendLine();

sb.AppendLine("## Implementation Guidance (Data-Driven)");
sb.AppendLine("- Prioritize periodic support for `Damage`, `Heal`, and `HealShields` rows with high-frequency tick/duration patterns above.");
sb.AppendLine("- Implement CC duration using `CCStateSet` rows (`DataBits00` state + `DurationTime`) and tie DR buckets via `CCStates.CcStateDiminishingReturnsId`.");
sb.AppendLine("- Expand dispel from CC-only to buff/debuff instance removal by honoring `SpellClass` and stack-group metadata.");
sb.AppendLine("- Apply stack semantics using `Spell4StackGroup` (`StackTypeEnum`, `StackCap`) for periodic auras.");
sb.AppendLine("- Use `Spell4CCConditions` masks/required flags for cast gating and persistence checks.");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, sb.ToString());

Console.WriteLine($"Wrote report: {outputPath}");
Console.WriteLine($"Effects={totalEffects}, UniqueEffectTypes={uniqueEffectTypes}, CCSet={ccSetRows.Count}, PeriodicDamage={periodicDamage.Count}");
return 0;

static GameTable<T> LoadTable<T>(string tableDir, string fileName) where T : class, new()
{
    string path = Path.Combine(tableDir, fileName);
    if (!File.Exists(path))
        throw new FileNotFoundException($"Missing table file: {path}");

    using FileStream stream = File.OpenRead(path);
    return new GameTable<T>(stream);
}

static int ResolveSpellClass(Spell4Entry spell, IReadOnlyDictionary<uint, Spell4BaseEntry> baseById)
{
    if (!baseById.TryGetValue(spell.Spell4BaseIdBaseSpell, out Spell4BaseEntry baseEntry))
        return -1;

    return unchecked((int)baseEntry.SpellClass);
}

static List<string> DecodeCCMask(uint mask)
{
    var states = new List<string>();
    for (int bit = 0; bit < 32; bit++)
    {
        if ((mask & (1u << bit)) == 0u)
            continue;

        if (Enum.IsDefined(typeof(CCState), bit))
            states.Add(((CCState)bit).ToString());
    }

    return states;
}

static string DecodeDataType(uint value)
{
    if (Enum.IsDefined(typeof(DataType), (ushort)value))
        return $"{(DataType)(ushort)value}";

    return value.ToString();
}
