using System;

namespace NexusForever.Database.Character.Model
{
    public class ResidenceNeighbor
    {
        public ulong Id { get; set; }
        public ulong ResidenceId { get; set; }
        public ulong NeighborResidenceId { get; set; }
        public bool Pending { get; set; }
        public DateTime CreatedAt { get; set; }

        public ResidenceModel Residence { get; set; }
    }
}
