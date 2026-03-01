namespace NexusForever.Database.Character.Model
{
    public class CharacterChallengeModel
    {
        public ulong Id { get; set; }
        public uint ChallengeId { get; set; }
        public uint CurrentCount { get; set; }
        public uint CurrentTier { get; set; }
        public uint CompletionCount { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsActivated { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsOnCooldown { get; set; }
        public double TimeRemaining { get; set; }
        public double CooldownRemaining { get; set; }
        public uint ActivatedDt { get; set; }

        public CharacterModel Character { get; set; }
    }
}
