using System.Numerics;

namespace NexusForever.IO.Area
{
    public class Curt : IReadable
    {
        public class Entry
        {
            public uint CreatureId  { get; }
            public uint Unknown4    { get; }  // constant per creature type
            public uint Unknown8    { get; }  // constant per creature type
            public uint UnknownC    { get; }  // area/zone property
            public uint Offset      { get; }  // byte offset from chunk start to spawn positions
            public uint Unknown14   { get; }  // constant per creature type

            /// <summary>
            /// XYZ spawn positions read from the trailing data block.
            /// Populated after the full entry table is read.
            /// </summary>
            public List<Vector3> Positions { get; } = new();

            public Entry(BinaryReader reader)
            {
                CreatureId = reader.ReadUInt32();
                Unknown4   = reader.ReadUInt32();
                Unknown8   = reader.ReadUInt32();
                UnknownC   = reader.ReadUInt32();
                Offset     = reader.ReadUInt32();
                Unknown14  = reader.ReadUInt32();
            }
        }

        public List<Entry> Entries { get; } = new();

        public void Read(BinaryReader reader)
        {
            uint count = reader.ReadUInt32();
            for (uint i = 0; i < count; i++)
                Entries.Add(new Entry(reader));

            // Each entry's Offset points (from stream position 0) to a block of
            // spawn instance positions: packed (X:float32, Y:float32, Z:float32) triplets.
            // Block size = next entry's Offset − this entry's Offset
            //            = (stream.Length − this entry's Offset) for the last entry.
            long streamLen = reader.BaseStream.Length;
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry entry = Entries[i];
                long blockEnd = (i + 1 < Entries.Count)
                    ? Entries[i + 1].Offset
                    : streamLen;

                long blockSize = blockEnd - entry.Offset;
                if (blockSize <= 0 || blockSize % 12 != 0)
                    continue;   // skip malformed blocks

                reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                int spawns = (int)(blockSize / 12);
                for (int s = 0; s < spawns; s++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    entry.Positions.Add(new Vector3(x, y, z));
                }
            }
        }
    }
}
