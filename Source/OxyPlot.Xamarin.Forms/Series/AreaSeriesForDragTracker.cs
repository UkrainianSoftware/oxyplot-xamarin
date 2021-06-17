﻿using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot.Series;


namespace OxyPlot
{
    // Note: ideally this class should belong to OxyPlot.csproj
    // placing it here for now wo avoid forking that project
    // -
    public class AreaSeriesForDragTracker: AreaSeries
    {
        // -----
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Series/XYAxisSeries.cs#L310
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Series/AreaSeries.cs#L153
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Series/XYAxisSeries.cs#L297
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Series/XYAxisSeries.cs#L310
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Series/XYAxisSeries.cs#L214
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Series/XYAxisSeries.cs#L227
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Rendering/ScreenPointHelper.cs#L74
        // * https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Rendering/ScreenPointHelper.cs#L106
        // -


        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">interpolate if set to <c>true</c> .</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            var xy = this.InverseTransform(point);
            var targetX = xy.X;
            int startIdx = this.IsXMonotonic
                ? this.FindWindowStartIndex(this.ActualPoints, p => p.X, targetX, this.WindowStartIndex)
                : 0;
            int startIdx2 = this.IsXMonotonic
                ? this.FindWindowStartIndex(this.ActualPoints2, p => p.X, targetX, this.WindowStartIndex2)
                : 0;

            TrackerHitResult result1, result2;
            if (interpolate && this.CanTrackerInterpolatePoints)
            {
                result1 = this.GetNearestInterpolatedPointInternalX(this.ActualPoints, startIdx, point);
                result2 = this.GetNearestInterpolatedPointInternalX(this.ActualPoints2, startIdx2, point);
            }
            else
            {
                result1 = this.GetNearestPointInternalX(this.ActualPoints, startIdx, point);
                result2 = this.GetNearestPointInternalX(this.ActualPoints2, startIdx2, point);
            }

            TrackerHitResult result;
            if (result1 != null && result2 != null)
            {
                double dist1 = Math.Abs(result1.Position.X - point.X); // result1.Position.DistanceTo(point);
                double dist2 = Math.Abs(result2.Position.X - point.X); // result2.Position.DistanceTo(point);

                result = (dist1 < dist2)
                    ? result1
                    : result2;
            }
            else
            {
                result = result1 ?? result2;
            }

            if (result != null)
            {
                result.Text = StringHelper.Format(
                    this.ActualCulture,
                    this.TrackerFormatString,
                    result.Item,
                    this.Title,
                    this.XAxis.Title ?? XYAxisSeries.DefaultXAxisTitle,
                    this.XAxis.GetValue(result.DataPoint.X),
                    this.YAxis.Title ?? XYAxisSeries.DefaultYAxisTitle,
                    this.YAxis.GetValue(result.DataPoint.Y));
            }

            return result;
        }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="points">The points (data coordinates).</param>
        /// <param name="startIdx">The index to start from.</param>
        /// <param name="point">The point (screen coordinates).</param>
        /// <returns>A <see cref="TrackerHitResult" /> if a point was found, <c>null</c> otherwise.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected virtual TrackerHitResult GetNearestPointInternalX(
            IEnumerable<DataPoint> points,
            int startIdx,
            ScreenPoint point)
        {
            var spn = default(ScreenPoint);
            var dpn = default(DataPoint);
            double index = -1;

            double minimumDistance = double.MaxValue;
            int i = 0;
            foreach (var p in points.Skip(startIdx))
            {
                if (!this.IsValidPoint(p))
                {
                    i++;
                    continue;
                }

                var sp = this.Transform(p.X, p.Y);
                double d2 =
                    (sp.X - point.X)
                  * (sp.X - point.X);

                if (d2 < minimumDistance)
                {
                    dpn = p;
                    spn = sp;
                    minimumDistance = d2;
                    index = i;
                }

                i++;
            }

            if (minimumDistance < double.MaxValue)
            {
                var item = this.GetItem((int)Math.Round(index));
                return new TrackerHitResult
                {
                    Series = this,
                    DataPoint = dpn,
                    Position = spn,
                    Item = item,
                    Index = index
                };
            }

            return null;
        }


        /// <summary>
        /// Gets the point on the curve that is nearest the specified point.
        /// </summary>
        /// <param name="points">The point list.</param>
        /// <param name="startIdx">The index to start from.</param>
        /// <param name="point">The point.</param>
        /// <returns>A tracker hit result if a point was found.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected TrackerHitResult GetNearestInterpolatedPointInternalX(
            List<DataPoint> points,
            int startIdx,
            ScreenPoint point)
        {
            if (this.XAxis == null || this.YAxis == null || points == null)
            {
                return null;
            }

            var spn = default(ScreenPoint);
            var dpn = default(DataPoint);
            double index = -1;

            double minimumDistance = double.MaxValue;

            for (int i = startIdx; i + 1 < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                if (!this.IsValidPoint(p1) || !this.IsValidPoint(p2))
                {
                    continue;
                }

                var sp1 = this.Transform(p1);
                var sp2 = this.Transform(p2);

                // Find the nearest point on the line segment.
                // ---
                // !!! TODO: maybe updatete this method to ignore Y
                // -
                var spl = ScreenPointHelper.FindPointOnLine(point, sp1, sp2);

                if (ScreenPoint.IsUndefined(spl))
                {
                    // P1 && P2 coincident
                    continue;
                }

                double l2 =
                    (point.X - spl.X)
                  * (point.X - spl.X);

                if (l2 < minimumDistance)
                {
                    double segmentLength = Math.Abs(sp2.X - sp1.X);
                    double u = (segmentLength > 0)
                        ? Math.Abs(spl.X - sp1.X) / segmentLength
                        : 0;

                    dpn = this.InverseTransform(spl);
                    spn = spl;
                    minimumDistance = l2;
                    index = i + u;
                }
            }

            if (minimumDistance < double.MaxValue)
            {
                var item = this.GetItem((int)Math.Round(index));
                return new TrackerHitResult
                {
                    Series = this,
                    DataPoint = dpn,
                    Position = spn,
                    Item = item,
                    Index = index
                };
            }

            return null;
        }



        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="points">The points (data coordinates).</param>
        /// <param name="point">The point (screen coordinates).</param>
        /// <returns>A <see cref="TrackerHitResult" /> if a point was found, <c>null</c> otherwise.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected virtual TrackerHitResult GetNearestPointInternalX(
            IEnumerable<DataPoint> points,
            ScreenPoint point)
        {
            return this.GetNearestPointInternalX(points, 0, point);
        }


        /// <summary>
        /// Gets the point on the curve that is nearest the specified point.
        /// </summary>
        /// <param name="points">The point list.</param>
        /// <param name="point">The point.</param>
        /// <returns>A tracker hit result if a point was found.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected TrackerHitResult GetNearestInterpolatedPointInternalX(
            List<DataPoint> points,
            ScreenPoint point)
        {
            return this.GetNearestInterpolatedPointInternalX(points, 0, point);
        }




        /// <summary>
        /// Finds the point on line.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="p1">The first point on the line.</param>
        /// <param name="p2">The second point on the line.</param>
        /// <returns>The nearest point on the line.</returns>
        /// <remarks>See <a href="http://paulbourke.net/geometry/pointlineplane/">Bourke</a>.</remarks>
        public static ScreenPoint FindPointOnLine(
            ScreenPoint p,
            ScreenPoint p1,
            ScreenPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double u = FindPositionOnLine(p, p1, p2);

            if (double.IsNaN(u))
            {
                u = 0;
            }

            if (u < 0)
            {
                u = 0;
            }

            if (u > 1)
            {
                u = 1;
            }

            return new ScreenPoint(p1.X + (u * dx), p1.Y + (u * dy));
        }

        /// <summary>
        /// Finds the nearest point on line.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="p1">The start point on the line.</param>
        /// <param name="p2">The end point on the line.</param>
        /// <returns>The relative position of the nearest point.</returns>
        /// <remarks>See <a href="http://paulbourke.net/geometry/pointlineplane/">Bourke</a>.</remarks>
        public static double FindPositionOnLine(
            ScreenPoint p,
            ScreenPoint p1,
            ScreenPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double u1 = ((p.X - p1.X) * dx) + ((p.Y - p1.Y) * dy);
            double u2 = (dx * dx) + (dy * dy);

            if (u2 < 1e-6)
            {
                return double.NaN;
            }

            return u1 / u2;
        }
    }
}
