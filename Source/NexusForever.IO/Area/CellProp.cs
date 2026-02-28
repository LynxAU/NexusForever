namespace NexusForever.IO.Area
{
    public class CellProp : IReadable
    {
        public byte[] RawData { get; private set; }

        public void Read(BinaryReader reader)
        {
            int remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            if (remaining > 0)
                RawData = reader.ReadBytes(remaining);
        }
    }
}
