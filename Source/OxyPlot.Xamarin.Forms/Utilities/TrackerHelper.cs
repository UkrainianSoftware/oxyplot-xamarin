

namespace OxyPlot.Xamarin.Forms.Utilities
{
    /// <summary>
    /// A copy of |OxyPlot/Utilities/TrackerHelper.cs|
    /// placing it here for now wo avoid forking that project as well
    /// TODO: fork both projects before sending a patch to maintainer
    /// ---
    /// https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Utilities/TrackerHelper.cs
    /// </summary>
    public static class TrackerHelper
    {
        /// <summary>
        /// Gets the nearest tracker hit.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="point">The point.</param>
        /// <param name="snap">Snap to points.</param>
        /// <param name="pointsOnly">Check points only (no interpolation).</param>
        /// <param name="firesDistance">The distance from the series at which the tracker fires</param>
        /// <param name="checkDistanceBetweenPoints">The value indicating whether to check distance
        /// when showing tracker between data points.</param>
        /// <remarks>
        /// <paramref name="checkDistanceBetweenPoints" /> is ignored if <paramref name="pointsOnly"/> is equal to <c>False</c>.
        /// </remarks>
        /// <returns>A tracker hit result.</returns>
        public static TrackerHitResult GetNearestHit(
            Series.Series series,
            ScreenPoint point,
            bool snap,
            bool pointsOnly,
            double firesDistance,
            bool checkDistanceBetweenPoints)
        {
            if (series == null)
            {
                return null;
            }

            TrackerHitResult result = series.GetNearestPoint(point, interpolate: true);
            return result;



            //// Check data points only
            //if (snap || pointsOnly)
            //{
            //    TrackerHitResult result = series.GetNearestPoint(point, interpolate: false);
            //    if (ShouldTrackerOpen(result, point, firesDistance))
            //    {
            //        return result;
            //    }
            //}
            //
            //// Check between data points (if possible)
            //if (!pointsOnly)
            //{
            //    TrackerHitResult result = series.GetNearestPoint(point, interpolate: true);
            //    if (!checkDistanceBetweenPoints || ShouldTrackerOpen(result, point, firesDistance))
            //    {
            //        return result;
            //    }
            //}
            //
            //return null;
        }

        private static bool ShouldTrackerOpen(TrackerHitResult result, ScreenPoint point, double firesDistance) => true;
            // result?.Position.DistanceTo(point) < firesDistance;
    }
}
