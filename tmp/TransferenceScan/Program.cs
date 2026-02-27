using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

string path = @"c:\Games\Dev\WIldstar\NexusForever-codex\tmp\tables\tbl\Spell4Effects.tbl";
using var stream = File.OpenRead(path);
var table = new GameTable<Spell4EffectsEntry>(stream);

var rows = table.Entries.Where(e => e.EffectType == SpellEffectType.Transference).ToList();
Console.WriteLine($"count={rows.Count}");

var byBits = rows
    .GroupBy(r => new { r.DataBits00, r.DataBits01, r.DataBits02, r.DataBits03, r.DataBits04, r.DamageType, r.TargetFlags })
    .OrderByDescending(g => g.Count())
    .Take(25)
    .ToList();

Console.WriteLine("Top patterns:");
foreach (var g in byBits)
{
    var k = g.Key;
    Console.WriteLine($"n={g.Count(),4} dmg={k.DamageType,-10} tgt=0x{k.TargetFlags:X} bits00={k.DataBits00} bits01={k.DataBits01} bits02={k.DataBits02} bits03={k.DataBits03} bits04={k.DataBits04}");
}

Console.WriteLine("Sample with params:");
foreach (var r in rows.Take(30))
{
    string pTypes = string.Join(",", r.ParameterType.Select(p => p.ToString()));
    string pVals = string.Join(",", r.ParameterValue.Select(v => v.ToString("0.###")));
    Console.WriteLine($"id={r.Id} spell={r.SpellId} dmg={r.DamageType} tgt=0x{r.TargetFlags:X} b00={r.DataBits00} b01={r.DataBits01} b02={r.DataBits02} b03={r.DataBits03} b04={r.DataBits04} pT=[{pTypes}] pV=[{pVals}]");
}
