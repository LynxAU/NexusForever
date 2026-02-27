using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestCompleteHandler : IMessageHandler<IWorldSession, ClientQuestComplete>
    {
        public void HandleMessage(IWorldSession session, ClientQuestComplete questComplete)
        {
            session.Player.QuestManager.QuestComplete(questComplete.QuestId, questComplete.RewardSelection, questComplete.IsCommunicatorMsg);

            // Trigger Unknown39 objectives - related to completing quests
            // Data is 0 on all entries, so triggers on any quest completion
            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown39, 0, 1);

            // Trigger CompleteChallenge objectives - completing challenges anywhere in Nexus
            // Data is 0, triggered on quest completion (challenges are often quest-based)
            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1);

            // Trigger Unknown27 objectives - rare objective type with single entry
            // Not in localization, but try to trigger on quest completion as fallback
            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown27, 0, 1);

            // Trigger Unknown31 objectives - empty text, likely script-triggered
            // Try to trigger on quest completion as many scripted events complete via quests
            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown31, 0, 1);
        }
    }
}
