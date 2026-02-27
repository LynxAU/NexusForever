using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity.Movement.Spline.Type;
using NexusForever.Game.Static.Entity.Movement.Spline;

namespace NexusForever.Game.Entity.Movement.Spline.Type
{
    public class SplineTypeFactory : ISplineTypeFactory
    {
        #region Dependency Injection

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<SplineTypeFactory> log;

        public SplineTypeFactory(
            IServiceProvider serviceProvider,
            ILogger<SplineTypeFactory> log)
        {
            this.serviceProvider = serviceProvider;
            this.log             = log;
        }

        #endregion

        public ISplineType Create(SplineType type)
        {
            ISplineType splineType = type switch
            {
                SplineType.Linear     => serviceProvider.GetRequiredService<SplineTypeLinear>(),
                SplineType.CatmullRom => serviceProvider.GetRequiredService<SplineTypeCatmullRom>(),
                _                     => null
            };

            if (splineType != null)
                return splineType;

            log.LogWarning("Unknown spline type {SplineType}, falling back to Linear.", type);
            return serviceProvider.GetRequiredService<SplineTypeLinear>();
        }
    }
}
