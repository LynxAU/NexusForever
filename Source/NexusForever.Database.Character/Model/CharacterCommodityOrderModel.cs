using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusForever.Database.Character.Model
{
    /// <summary>
    /// Represents a commodity exchange order (buy or sell order for stackable items)
    /// </summary>
    [Table("character_commodity_order")]
    public class CharacterCommodityOrderModel
    {
        [Column("orderId")]
        [Key]
        public ulong OrderId { get; set; }

        [Column("id")]
        public ulong Id { get; set; }

        [Column("characterId")]
        public ulong CharacterId { get; set; }

        [Column("itemId")]
        public uint ItemId { get; set; }

        [Column("quantity")]
        public uint Quantity { get; set; }

        [Column("filledQuantity")]
        public uint FilledQuantity { get; set; }

        [Column("unitPrice")]
        public ulong UnitPrice { get; set; }

        [Column("isBuyOrder")]
        public bool IsBuyOrder { get; set; }

        [Column("expirationTime")]
        public DateTime ExpirationTime { get; set; }

        [Column("createTime")]
        public DateTime CreateTime { get; set; }
    }
}
