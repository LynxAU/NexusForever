using NexusForever.Game.Static.RBAC;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Configuration.Model
{
    [ConfigurationBind]
    public class RealmConfig
    {
        public MapConfig Map { get; set; }
        public ushort RealmId { get; set; }
        public string MessageOfTheDay { get; set; }
        public uint LengthOfInGameDay { get; set; }
        public ulong CostumeSaveFlatFeeCredits { get; set; } = 1000ul;
        public uint HousingCrateBaseCapacity { get; set; } = 100u;
        public uint HousingCrateCapacityPerUpgrade { get; set; } = 10u;
        public double HousingSaveIntervalSeconds { get; set; } = 60d;
        public double HousingTemporaryPlotExpiryHours { get; set; } = 72d;
        public ulong AmpRespecCostPerAmpCredits { get; set; } = 0ul;
        public bool CrossFactionChat { get; set; } = true;
        public uint MaxPlayers { get; set; } = 50u;
        public Role? DefaultRole { get; set; } = Role.Player;
    }
}
