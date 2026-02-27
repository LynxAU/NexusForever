using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusForever.Database.Character.Model
{
    [Table("character_auction")]
    public class CharacterAuctionModel
    {
        [Key]
        [Column("id")]
        public ulong Id { get; set; }

        [Column("auctionId")]
        public ulong AuctionId { get; set; }

        [Column("itemGuid")]
        public ulong ItemGuid { get; set; }

        [Column("itemId")]
        public uint ItemId { get; set; }

        [Column("quantity")]
        public uint Quantity { get; set; }

        [Column("minimumBid")]
        public ulong MinimumBid { get; set; }

        [Column("buyoutPrice")]
        public ulong BuyoutPrice { get; set; }

        [Column("currentBid")]
        public ulong CurrentBid { get; set; }

        [Column("ownerCharacterId")]
        public ulong OwnerCharacterId { get; set; }

        [Column("topBidderCharacterId")]
        public ulong TopBidderCharacterId { get; set; }

        [Column("expirationTime")]
        public DateTime ExpirationTime { get; set; }

        [Column("createTime")]
        public DateTime CreateTime { get; set; }

        [Column("worldRequirement_Item2Id")]
        public uint WorldRequirementItem2Id { get; set; }

        [Column("glyphData")]
        public uint GlyphData { get; set; }

        [Column("thresholdData")]
        public ulong ThresholdData { get; set; }

        [Column("circuitData")]
        public ulong CircuitData { get; set; }

        [Column("unknown2")]
        public uint Unknown2 { get; set; }
    }
}
