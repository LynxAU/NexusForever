using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusForever.Database.Character.Model
{
    [Table("character_primal_matrix")]
    public class CharacterPrimalMatrixModel
    {
        [Key]
        [Column("id")]
        public ulong Id { get; set; }

        [Key]
        [Column("essence_id")]
        public uint EssenceId { get; set; }

        [Column("amount")]
        public uint Amount { get; set; }

        public CharacterModel Character { get; set; }
    }
}
