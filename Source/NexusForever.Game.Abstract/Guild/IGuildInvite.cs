using System;

namespace NexusForever.Game.Abstract.Guild
{
    public interface IGuildInvite
    {
        ulong GuildId { get; set; }
        ulong InviterId { get; set; }
        ulong InviteeId { get; set; }
        DateTime Created { get; set; }
    }
}
