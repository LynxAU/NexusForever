namespace NexusForever.Game.Static.Crafting
{
    /// <summary>
    /// Result of a crafting operation.
    /// </summary>
    public enum CraftingResult
    {
        Success                 = 0,
        InvalidSchematic         = 1,
        SchematicNotLearned     = 2,
        TradeskillNotLearned    = 3,
        InsufficientMaterials   = 4,
        InventoryFull           = 5,
        InvalidOutput           = 6,
        /// <summary>Complex-craft minigame failure: materials consumed but craft produced a fail item or nothing.</summary>
        CraftFailed             = 7
    }
}
