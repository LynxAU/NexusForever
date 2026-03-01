using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Challenge;
using NexusForever.Shared;

namespace NexusForever.Game.Challenge
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameChallenge(this IServiceCollection sc)
        {
            sc.AddSingletonLegacy<IGlobalChallengeManager, GlobalChallengeManager>();
        }
    }
}
