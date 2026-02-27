using System;

namespace NexusForever.Database.Character.Model
{
    /// <summary>
    /// Stores instance lockout and saved instance data for characters
    /// </summary>
    public class CharacterInstanceModel
    {
        public ulong Id { get; set; }
        public ulong CharacterId { get; set; }
        public ushort WorldId { get; set; }
        public ulong InstanceId { get; set; }
        public DateTime LockoutExpiry { get; set; }
        public byte Difficulty { get; set; }
        public byte PrimeLevel { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float Rotation { get; set; }

        public CharacterModel Character { get; set; }
    }
}
