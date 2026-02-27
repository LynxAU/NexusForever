using NexusForever.Game.Static.Setting;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    /// <summary>
    /// Handles setting instance difficulty and prime level settings
    /// </summary>
    public class ClientSetInstanceSettingsHandler : IMessageHandler<IWorldSession, ClientSetInstanceSettings>
    {
        public void HandleMessage(IWorldSession session, ClientSetInstanceSettings instanceSettings)
        {
            // TODO: Store these settings for the player and apply them when entering the instance
            // This requires implementing saved instance settings in the database

            session.EnqueueMessageEncrypted(new ServerInstanceSettings
            {
                Difficulty = instanceSettings.Difficulty,
                PrimeLevel = instanceSettings.PrimeLevel,
                Flags = ServerInstanceSettings.WorldSetting.TransmatTeleport,
                ClientEntitySendUpdateInterval = 125
            });
        }
    }
}
