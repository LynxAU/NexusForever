namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Infinite Labs (WorldId 2980, Ultimate Protogames dungeon) - Boss Encounter Scripts
    //
    // NOTE: All boss creature IDs used in this dungeon are SHARED with the
    // Ultimate Protogames raid (WorldId 3041). Boss scripts for these creatures
    // are defined in Raid/UltimateProtogamesBossScripts.cs:
    //
    //   61417 - Hut-Hut (Gorganoth Boss)     -> UPHutHutScript    (BossEncounterScript with full rotation)
    //   61463 - Bev-O-Rage (Vending Machine) -> UPBevORageScript  (BossEncounterScript with full rotation)
    //   62575 - Crate Destruction Miniboss   -> UPCrateDestructionScript  (EncounterBossScript stub)
    //   63319 - Mixed Wave Miniboss          -> UPMixedWaveScript         (EncounterBossScript stub)
    //
    // Registering duplicate [ScriptFilterCreatureId] entries for these IDs here
    // would cause BOTH scripts to load on the same creature entity (the script
    // system stacks all matching scripts), resulting in double TriggerBossDeath calls.
    // The map-script HashSet guard prevents double-counting but duplicate scripts
    // are avoided entirely by keeping definitions in the single authoritative file above.
    //
    // Do NOT add [ScriptFilterCreatureId] entries here for those four IDs.
}
