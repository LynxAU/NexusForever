using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;
using NLog;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Loot
{
    /// <summary>
    /// Manages active need/greed/pass loot roll sessions.
    /// One session is created per creature kill when the killer is in a group.
    /// </summary>
    public sealed class LootRollManager : Singleton<LootRollManager>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static long nextLootUnitId;
        private static uint GetNextLootUnitId() => (uint)System.Threading.Interlocked.Increment(ref nextLootUnitId);

        /// <summary>
        /// How long (seconds) players have to submit a roll before items auto-resolve.
        /// </summary>
        private const double RollTimeoutSeconds = 60.0;

        /// <summary>
        /// Seconds sent to the client for the roll countdown UI.
        /// </summary>
        private const uint RollTimeTicks = 60u;

        private sealed class PendingLootItem
        {
            public uint LootUnitId { get; init; }
            public uint ItemId     { get; init; }
            public uint Count      { get; init; }

            /// <summary>CharacterId → roll (null = not yet rolled)</summary>
            public Dictionary<ulong, LootRollAction?> Rolls { get; } = new();

            public bool IsResolved { get; set; }
        }

        private sealed class LootRollSession
        {
            public uint         OwnerUnitId    { get; init; }
            public List<ulong>  ParticipantIds { get; init; }
            public List<PendingLootItem> Items { get; init; }
            public bool         IsResolved     { get; set; }
        }

        private readonly Dictionary<uint, LootRollSession> sessions = new();
        private readonly object sessionLock = new object();

        private static readonly Random rng = new Random();

        /// <summary>
        /// Create a new roll session for loot dropped by the creature at <paramref name="ownerUnitId"/>.
        /// Each group member on the same map is treated as a participant.
        /// </summary>
        public void CreateSession(uint ownerUnitId, List<ulong> participantIds, IEnumerable<LootDrop> drops)
        {
            var items = new List<PendingLootItem>();
            foreach (LootDrop drop in drops)
            {
                var item = new PendingLootItem
                {
                    LootUnitId = GetNextLootUnitId(),
                    ItemId     = drop.ItemId,
                    Count      = drop.Count
                };
                foreach (ulong pid in participantIds)
                    item.Rolls[pid] = null;
                items.Add(item);
            }

            if (items.Count == 0)
                return;

            var session = new LootRollSession
            {
                OwnerUnitId    = ownerUnitId,
                ParticipantIds = participantIds,
                Items          = items
            };

            lock (sessionLock)
                sessions[ownerUnitId] = session;

            // Notify all participants of available loot requiring rolls.
            var notify = new ServerLootNotify
            {
                OwnerUnitId  = ownerUnitId,
                ParentUnitId = ownerUnitId,
                Explosion    = false,
                LootItems    = items.Select(i => new LootItem
                {
                    LootUnitId    = i.LootUnitId,
                    Type          = LootItemType.StaticItem,
                    ItemId        = i.ItemId,
                    Amount        = i.Count,
                    CanLoot       = false,
                    RequiresRoll  = true,
                    RollTime     = RollTimeTicks
                }).ToList()
            };
            BroadcastToParticipants(participantIds, notify);

            // Schedule automatic resolution when the roll window expires.
            Task.Delay(TimeSpan.FromSeconds(RollTimeoutSeconds)).ContinueWith(_ =>
            {
                lock (sessionLock)
                {
                    if (!sessions.TryGetValue(ownerUnitId, out LootRollSession s) || s.IsResolved)
                        return;
                    ResolveAllPending(s);
                    s.IsResolved = true;
                    sessions.Remove(ownerUnitId);
                }
            });
        }

        /// <summary>
        /// Record a player's roll action for a specific loot item.
        /// Broadcasts the roll to all participants and resolves the item if all have rolled.
        /// </summary>
        public void HandleRollAction(IPlayer player, uint ownerUnitId, uint lootUnitId, LootRollAction action)
        {
            lock (sessionLock)
            {
                if (!sessions.TryGetValue(ownerUnitId, out LootRollSession session) || session.IsResolved)
                    return;

                PendingLootItem item = session.Items.FirstOrDefault(i => i.LootUnitId == lootUnitId && !i.IsResolved);
                if (item == null)
                    return;

                if (!item.Rolls.ContainsKey(player.CharacterId))
                    return; // Not a participant in this session.

                if (item.Rolls[player.CharacterId] != null)
                    return; // Player already rolled for this item.

                item.Rolls[player.CharacterId] = action;

                // Broadcast roll to all participants.
                BroadcastToParticipants(session.ParticipantIds, new ServerLootRoll
                {
                    LootUnitId = lootUnitId,
                    Roller     = MakeNetworkIdentity(player),
                    ItemId     = item.ItemId,
                    Action     = action
                });

                // Resolve immediately if everyone has submitted a roll.
                if (item.Rolls.Values.All(r => r != null))
                    ResolveItem(session, item);

                // Clean up the session when all items are done.
                if (session.Items.All(i => i.IsResolved))
                {
                    session.IsResolved = true;
                    sessions.Remove(session.OwnerUnitId);
                }
            }
        }

        private void ResolveAllPending(LootRollSession session)
        {
            foreach (PendingLootItem item in session.Items.Where(i => !i.IsResolved))
                ResolveItem(session, item);
        }

        private void ResolveItem(LootRollSession session, PendingLootItem item)
        {
            item.IsResolved = true;

            // Assign random roll values:
            //   Need  → 101-200 (client subtracts 100 for display)
            //   Greed → 1-100
            //   Pass or absent → 0 (no win chance)
            var rollValues = new Dictionary<ulong, uint>();
            foreach (var (charId, action) in item.Rolls)
            {
                rollValues[charId] = action switch
                {
                    LootRollAction.Need  => (uint)(rng.Next(1, 101) + 100),
                    LootRollAction.Greed => (uint)rng.Next(1, 101),
                    _                   => 0u
                };
            }

            // Find winner: highest roll value wins.
            ulong winnerId  = 0;
            uint  winnerVal = 0;
            foreach (var (charId, val) in rollValues)
            {
                if (val > winnerVal)
                {
                    winnerVal = val;
                    winnerId  = charId;
                }
            }

            // Build winner announcement.
            var winnerMsg = new ServerLootWinner
            {
                LootUnitId  = item.LootUnitId,
                ItemId      = item.ItemId,
                WinningRoll = winnerId != 0
                    ? new ServerLootWinner.LootRoll { Identity = GetNetworkIdentityForChar(winnerId), Value = winnerVal }
                    : new ServerLootWinner.LootRoll(), // Zero identity = all passed.
                OtherRolls  = rollValues
                    .Where(kvp => kvp.Key != winnerId)
                    .Select(kvp => new ServerLootWinner.LootRoll
                    {
                        Identity = GetNetworkIdentityForChar(kvp.Key),
                        Value    = kvp.Value
                    })
                    .ToList()
            };
            BroadcastToParticipants(session.ParticipantIds, winnerMsg);

            // Grant item to winner.
            if (winnerId != 0)
            {
                IPlayer winner = PlayerManager.Instance.GetPlayer(winnerId);
                if (winner != null)
                    winner.Inventory.ItemCreate(InventoryLocation.Inventory, item.ItemId, item.Count, ItemUpdateReason.Loot);
                else
                    log.Warn($"Loot roll winner {winnerId} is offline — item {item.ItemId} was lost.");
            }
        }

        private static NetworkIdentity MakeNetworkIdentity(IPlayer player)
        {
            return new NetworkIdentity
            {
                Id      = player.CharacterId,
                RealmId = player.Identity.RealmId
            };
        }

        private static NetworkIdentity GetNetworkIdentityForChar(ulong characterId)
        {
            IPlayer player = PlayerManager.Instance.GetPlayer(characterId);
            return player != null
                ? MakeNetworkIdentity(player)
                : new NetworkIdentity { Id = characterId, RealmId = 0 };
        }

        private static void BroadcastToParticipants(IEnumerable<ulong> participantIds, IWritable message)
        {
            foreach (ulong charId in participantIds)
                PlayerManager.Instance.GetPlayer(charId)?.Session.EnqueueMessageEncrypted(message);
        }
    }
}
