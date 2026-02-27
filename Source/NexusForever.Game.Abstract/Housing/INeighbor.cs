namespace NexusForever.Game.Abstract.Housing
{
    public interface INeighbor
    {
        ulong ResidenceId { get; }
        bool IsPending { get; }
    }
}
