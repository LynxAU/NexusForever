using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Challenges;
using NexusForever.Network.World.Message.Model.Challenges;
using NLog;

namespace NexusForever.Game.Entity
{
    public class ChallengeManager : IChallengeManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;

        // Active (or recently completed) challenges keyed by challenge id
        private readonly Dictionary<uint, IChallenge> challenges = new();

        public ChallengeManager(IPlayer owner)
        {
            player = owner;
        }

        public void Dispose()
        {
            challenges.Clear();
        }

        public void Update(double lastTick)
        {
            // Timer-based challenge expiry / cooldown tracking goes here in a future pass.
        }

        public void SendInitialPackets()
        {
            var packet = new ServerChallengeUpdate
            {
                ActiveChallenges = challenges.Values
                    .Select(c => c.Build())
                    .ToList()
            };

            player.Session.EnqueueMessageEncrypted(packet);
        }

        public void ChallengeActivate(uint challengeId)
        {
            // Lazily create the challenge on first activation
            if (!challenges.TryGetValue(challengeId, out IChallenge challenge))
            {
                IChallengeInfo info = Challenge.GlobalChallengeManager.Instance.GetChallengeInfo(challengeId);
                if (info == null)
                {
                    log.Warn($"Player {player.CharacterId} tried to activate unknown challenge {challengeId}.");
                    SendResult(challengeId, ChallengeResult.GenericFail);
                    return;
                }

                challenge = new Challenge.Challenge(info);
                challenges[challengeId] = challenge;
            }

            challenge.Activate();
            SendResult(challengeId, ChallengeResult.Activate);
            SendUpdate(challenge);
        }

        public void ChallengeAbandon(uint challengeId)
        {
            if (!challenges.TryGetValue(challengeId, out IChallenge challenge))
                return;

            challenge.Abandon();
            SendResult(challengeId, ChallengeResult.AbandonRemove);
            SendUpdate(challenge);
        }

        public void OnEntityKilled(uint creatureId)
        {
            bool anyUpdated = false;

            // Advance all active Combat-type challenges that match this creature
            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                if (!challenge.OnEntityKilled(creatureId))
                    continue;

                anyUpdated = true;

                if (challenge.IsCompleted)
                    SendResult(challenge.Id, ChallengeResult.Completed);
                else
                    SendUpdate(challenge);
            }

            // Also check if any unlocked Combat challenges for this creature exist and should be advertised
            if (!anyUpdated)
                return; // avoid extra work — only send updates if something progressed
        }

        private void SendResult(uint challengeId, ChallengeResult result)
        {
            player.Session.EnqueueMessageEncrypted(new ServerChallengeResult
            {
                ChallengeId = (ushort)challengeId,
                Result      = result
            });
        }

        private void SendUpdate(IChallenge challenge)
        {
            player.Session.EnqueueMessageEncrypted(new ServerChallengeUpdate
            {
                ActiveChallenges = new List<ServerChallengeUpdate.Challenge> { challenge.Build() }
            });
        }
    }
}
