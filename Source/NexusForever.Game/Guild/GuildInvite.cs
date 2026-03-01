using System;
using NexusForever.Game.Abstract.Guild;

namespace NexusForever.Game.Guild
{
    public class GuildInvite : IGuildInvite
    {
        public ulong GuildId { get; set; }
        public ulong InviterId { get; set; }
        public ulong InviteeId { get; set; }
        public DateTime Created { get; set; }
    }
}
