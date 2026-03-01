using System.ComponentModel.DataAnnotations.Schema;

namespace NexusForever.Database.Character.Model
{
    public class CharacterFriendInviteModel
    {
        /// <summary>Unique invite ID (used as the wire InviteId in packets).</summary>
        public ulong Id { get; set; }

        /// <summary>Character who sent the invite.</summary>
        public ulong SenderId { get; set; }

        /// <summary>Character who received the invite.</summary>
        public ulong ReceiverId { get; set; }

        /// <summary>Optional note attached to the invite request.</summary>
        public string Note { get; set; } = "";

        /// <summary>UTC creation time (stored as Unix epoch seconds).</summary>
        public long CreatedAt { get; set; }

        /// <summary>
        /// Whether the receiver has seen (acknowledged) the invite in the UI.
        /// Maps to the <c>Seen</c> 3-bit field in <see cref="ServerFriendInviteList.InviteData"/>.
        /// </summary>
        public byte Seen { get; set; }

        [ForeignKey("SenderId")]
        public CharacterModel Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public CharacterModel Receiver { get; set; }
    }
}
