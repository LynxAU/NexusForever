using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement.Spline.Template;
using NexusForever.Game.Static.Entity.Movement.Spline;

namespace NexusForever.Game.Entity.Movement.Spline.Template
{
    public class SplineTemplatePath : ISplineTemplatePath
    {
        public SplineType Type { get; private set; }
        public List<ISplineTemplatePoint> Points { get; } = new();

        /// <summary>
        /// Initialise spline template with supplied <see cref="SplineType"/> and points.
        /// </summary>
        public void Initialise(SplineType type, List<Vector3> points)
        {
            Type = type;

            if (points.Count == 0)
                return;

            // to keep things consistent and to match the client, add fake "amplitude" points for linear paths
            if (type == SplineType.Linear)
            {
                Points.Add(new SplineTemplatePoint
                {
                    Position = points[0]
                });
            }

            foreach (Vector3 node in points)
            {
                Points.Add(new SplineTemplatePoint
                {
                    Position = node
                });
            }

            if (type == SplineType.Linear)
            {
                Points.Add(new SplineTemplatePoint
                {
                    Position = points[points.Count - 1]
                });
            }
        }
    }
}
