using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Abstract.Matching.Queue
{
    public interface IMatchingRoleEnforcerResultMember
    {
        IIdentity Identity { get; init; }
        Role Role { get; set; }
    }
}
