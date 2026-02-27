using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity.Movement.Spline.Mode;
using NexusForever.Game.Static.Entity.Movement.Spline;

namespace NexusForever.Game.Entity.Movement.Spline.Mode
{
    public class SplineModeFactory : ISplineModeFactory
    {
        #region Dependency Injection

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<SplineModeFactory> log;

        public SplineModeFactory(
            IServiceProvider serviceProvider,
            ILogger<SplineModeFactory> log)
        {
            this.serviceProvider = serviceProvider;
            this.log             = log;
        }

        #endregion

        public ISplineMode Create(SplineMode mode)
        {
            ISplineMode splineMode = mode switch
            {
                SplineMode.OneShot             => serviceProvider.GetRequiredService<SplineModeOneShot>(),
                SplineMode.BackAndForth        => serviceProvider.GetRequiredService<SplineModeBackAndForth>(),
                SplineMode.Cyclic              => serviceProvider.GetRequiredService<SplineModeCyclic>(),
                SplineMode.OneShotReverse      => serviceProvider.GetRequiredService<SplineModeOneShotReverse>(),
                SplineMode.BackAndForthReverse => serviceProvider.GetRequiredService<SplineModeBackAndForthReverse>(),
                SplineMode.CyclicReverse       => serviceProvider.GetRequiredService<SplineModeCyclicReverse>(),
                _                              => null
            };

            if (splineMode != null)
                return splineMode;

            log.LogWarning("Unknown spline mode {SplineMode}, falling back to OneShot.", mode);
            return serviceProvider.GetRequiredService<SplineModeOneShot>();
        }
    }
}
