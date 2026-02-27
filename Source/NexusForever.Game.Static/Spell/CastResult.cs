namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Result of a spell cast operation.
    /// </summary>
    public enum CastResult
    {
        Success               = 0,
        SpellInterrupted      = 1,
        SpellCancelled        = 2,
        NotReady              = 3,
        OutOfRange            = 4,
        InvalidTarget         = 5,
        InsufficientResources = 6,
        CooldownNotReady      = 7,
        LineOfSightBlocked    = 8,
        Fizzle                = 9,
        Disrupted             = 10,
        Prevented             = 11
    }
}
