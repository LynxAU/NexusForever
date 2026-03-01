using NexusForever.Game.Static.Chat;
using NexusForever.Network.Internal.Message.Chat.Shared.Format;

namespace NexusForever.Network.Internal.Message.Chat.Shared.Format.Model
{
    public class ChatChannelTextSpell4IdFormat : IChatChannelTextFormatModel
    {
        public ChatFormatType Type => ChatFormatType.Spell4Id;
        public uint Spell4Id { get; set; }
    }
}
