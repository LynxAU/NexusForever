using System.Numerics;
using NexusForever.Game.Abstract.Housing;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Housing
{
    public class ResidenceEntrance : IResidenceEntrance
    {
        public WorldEntry Entry { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public ResidenceEntrance(WorldLocation2Entry entry)
        {
            Entry = GameTableManager.Instance.World.GetEntry(entry.WorldId);
            if (Entry == null)
                throw new HousingException($"WorldLocation2 entry {entry.Id} references invalid WorldId {entry.WorldId}.");
            Position = new Vector3(entry.Position0, entry.Position1, entry.Position2);
            Rotation = new Quaternion(entry.Facing0, entry.Facing1, entry.Facing2, entry.Facing3);
        }
    }
}
