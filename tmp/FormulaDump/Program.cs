using NexusForever.GameTable;
using NexusForever.GameTable.Model;

if (args.Length == 0)
{
    Console.WriteLine("Usage: FormulaDump <path-to-GameFormula.tbl>");
    return;
}

using var stream = File.OpenRead(args[0]);
var table = new GameTable<GameFormulaEntry>(stream);

uint[] ids = [1230, 1231, 1232, 1234, 1235, 1236, 1240, 1269, 1270];
foreach (uint id in ids)
{
    var e = table.GetEntry(id);
    if (e == null)
    {
        Console.WriteLine($"{id}: <missing>");
        continue;
    }

    Console.WriteLine($"{id}: Dataint0={e.Dataint0} Dataint01={e.Dataint01} Dataint02={e.Dataint02} Dataint03={e.Dataint03} Dataint04={e.Dataint04} Datafloat0={e.Datafloat0} Datafloat01={e.Datafloat01} Datafloat02={e.Datafloat02} Datafloat03={e.Datafloat03} Datafloat04={e.Datafloat04}");
}
