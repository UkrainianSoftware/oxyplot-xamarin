using System;
using System.Linq;


namespace OxyPlot
{
    public class PlotModelExtended: PlotModel
    {
        /// <summary>
        /// Gets a series from the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>The nearest series.</returns>
        public Series.Series GetSeriesFromPoint(
            ScreenPoint point,
            Func<Series.Series, bool> seriesPredicate,
            double limit = 100)
        {
            // https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/PlotModel/PlotModel.cs#L686

            double mindist = double.MaxValue;
            Series.Series nearestSeries = null;
            foreach (var series in this.Series.Reverse().Where(s => s.IsVisible).Where(s => seriesPredicate(s)))
            {
                var thr = series.GetNearestPoint(point, true) ?? series.GetNearestPoint(point, false);

                if (thr == null)
                {
                    continue;
                }

                // find distance to this point on the screen
                double dist = point.DistanceTo(thr.Position);
                if (dist < mindist)
                {
                    nearestSeries = series;
                    mindist = dist;
                }
            }

            if (mindist < limit)
            {
                return nearestSeries;
            }

            return null;
        }
    }
}
