using CommandLine;

namespace NexusForever.ContentTool
{
    public class Parameters
    {
        [Option('i', "patchPath", Required = false,
            HelpText = "The location of the WildStar client patch folder.")]
        public string? PatchPath { get; set; }

        [Option('d', "dbPath", Required = false,
            HelpText = "Path to the world database SQL files or connection string.")]
        public string? DbPath { get; set; }

        [Option('r', "report",
            HelpText = "Generate a coverage report (missing loot, spawns, etc).")]
        public bool Report { get; set; }

        [Option('s', "seed",
            HelpText = "Generate seed SQL/JSON for missing content.")]
        public bool Seed { get; set; }

        [Option('e', "extract",
            HelpText = "Extract game tables and other assets.")]
        public bool Extract { get; set; }

        [Option('o', "output",
            HelpText = "Output directory for generated files.")]
        public string OutputDir { get; set; } = "Output";

        [Option("tblPath", HelpText = "Path to already extracted .tbl files.")]
        public string? TblPath { get; set; }
    }
}
