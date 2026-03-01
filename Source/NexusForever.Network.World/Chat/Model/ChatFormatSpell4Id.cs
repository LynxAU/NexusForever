using NexusForever.Game.Static.Chat;

namespace NexusForever.Network.World.Chat.Model
{
    public class ChatFormatSpell4Id : IChatFormatModel
    {
        public ChatFormatType Type => ChatFormatType.Spell4Id;
        public uint Spell4Id { get; set; }

        public void Read(GamePacketReader reader)
        {
            Spell4Id = reader.ReadUInt(18u);
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Spell4Id, 18u);
        }
    }
}
