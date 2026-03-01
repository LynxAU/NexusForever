namespace NexusForever.Game.Static.Housing
{
    [Flags]
    public enum ResidenceFlags
    {
        None = 0x00,
        HideGroundClutter = 0x01,
        HideNeighborSkyplots = 0x02,
        UpkeepLocked = 0x04
    }
}
