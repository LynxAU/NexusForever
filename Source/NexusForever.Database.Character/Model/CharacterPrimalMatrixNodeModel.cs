using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusForever.Database.Character.Model
{
    [Table("character_primal_matrix_node")]
    public class CharacterPrimalMatrixNodeModel
    {
        [Key]
        [Column("id")]
        public ulong Id { get; set; }

        [Key]
        [Column("nodeId")]
        public uint NodeId { get; set; }

        [Column("allocations")]
        public uint Allocations { get; set; }

        public CharacterModel Character { get; set; }
    }
}
