namespace NexusForever.Game.Static.Housing
{
    /// <summary>
    /// Defines who can access a shared resource on a residence (resource nodes, garden plots).
    /// Stored as a 3-bit value in the database and sent over the network.
    /// </summary>
    public enum ResidenceSharingLevel : byte
    {
        /// <summary>Only the residence owner may access.</summary>
        OwnerOnly = 0,

        /// <summary>The owner and any roommates (community members) may access.</summary>
        Roommates = 1,

        /// <summary>The owner, roommates, and neighbors may access.</summary>
        Neighbors = 2,

        /// <summary>Anyone may access.</summary>
        Public = 3
    }
}
