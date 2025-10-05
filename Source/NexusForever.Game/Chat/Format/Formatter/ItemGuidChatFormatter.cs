using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Chat.Shared.Format;
using NexusForever.Network.Internal.Message.Chat.Shared.Format.Model;
using NexusForever.Network.World.Chat;
using NexusForever.Network.World.Chat.Model;

namespace NexusForever.Game.Chat.Format.Formatter
{
    public class ItemGuidChatFormatter : IInternalChatFormatter<ChatFormatItemGuid>, ILocalChatFormatter<ChatFormatItemGuid>
    {
        public IChatChannelTextFormatModel ToInternal(IPlayer player, ChatFormatItemGuid format)
        {
            IItem item = player.Inventory.GetItem(format.ItemGuid);
            return new ChatChannelTextItemGuidFormat
            {
                ItemGuid = item.Guid
            };
        }

        public IChatFormatModel ToLocal(IPlayer player, ChatFormatItemGuid format)
        {
            IItem item = player.Inventory.GetItem(format.ItemGuid);
            return new ChatFormatItemGuid
            {
                ItemGuid = item.Guid
            };
        }
    }
}
