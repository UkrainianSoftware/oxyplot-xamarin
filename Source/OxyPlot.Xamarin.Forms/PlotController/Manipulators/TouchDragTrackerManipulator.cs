using System.Linq;
using Utilities = OxyPlot.Xamarin.Forms.Utilities;


namespace OxyPlot
{
    // Note: ideally this class should belong to OxyPlot.csproj
    // placing it here for now wo avoid forking that project
    // -
    public class TouchDragTrackerManipulator : TouchManipulator
    {
        /// <summary>
        /// The current series.
        /// </summary>
        private Series.Series currentSeries;

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchTrackerManipulator" /> class.
        /// </summary>
        /// <param name="plotView">The plot view.</param>
        public TouchDragTrackerManipulator(IPlotView plotView)
            : base(plotView)
        {
            this.Snap = true;
            this.PointsOnly = false;
            this.LockToInitialSeries = true; // false ==> MouseDown is not delievered on ios

            // Distance between point and line.
            // Made it absurdly large enough for the sake of prototype
            // If too narrow ==> no tracker visible
            // ---
            // since |this.currentSeries == null| ==> |OxyTouchEventArgs == null|
            // -
            this.FiresDistance = 200.0;


            this.CheckDistanceBetweenPoints = false;

            // Note: the tracker manipulator should not handle pan or zoom
            this.SetHandledForPanOrZoom = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show tracker on points only (not interpolating).
        /// </summary>
        public bool PointsOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to snap to the nearest point.
        /// </summary>
        public bool Snap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to lock the tracker to the initial series.
        /// </summary>
        /// <value><c>true</c> if the tracker should be locked; otherwise, <c>false</c>.</value>
        public bool LockToInitialSeries { get; set; }

        /// <summary>
        /// Gets or sets the distance from the series at which the tracker fires.
        /// </summary>
        public double FiresDistance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to check distance when showing tracker between data points.
        /// </summary>
        /// <remarks>This parameter is ignored if <see cref="PointsOnly"/> is equal to <c>False</c>.</remarks>
        public bool CheckDistanceBetweenPoints { get; set; }

        /// <summary>
        /// Occurs when a manipulation is complete.
        /// </summary>
        /// <param name="e">The <see cref="OxyPlot.OxyTouchEventArgs" /> instance containing the event data.</param>
        public override void Completed(OxyTouchEventArgs e)
        {
            base.Completed(e);

            this.currentSeries = null;
            this.PlotView.HideTracker();
            if (this.PlotView.ActualModel != null)
            {
                this.PlotView.ActualModel.RaiseTrackerChanged(null);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Occurs when a touch delta event is handled.
        /// </summary>
        /// <param name="e">The <see cref="OxyPlot.OxyTouchEventArgs" /> instance containing the event data.</param>
        public override void Delta(OxyTouchEventArgs e)
        {
            // Note: [@dodikk] touchesMoved does not generate this event for some reason
            // ---
            // https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/Graphics/ControllerBase.cs#L254
            // -

            base.Delta(e);

            // Note: [@dodikk] expecting |this.currentSeries| to be set in Started()
            // might be a good idea to
            // |if (null == this.currentSeries) return;|
            // -
            // this.currentSeries = this.PlotView.ActualModel?.GetSeriesFromPoint(e.Position, this.FiresDistance);

            this.currentSeries = GetSeriesForPoint(e.Position);
            UpdateTracker(e.Position);

            e.Handled = true;
        }

        private Series.Series GetSeriesForPoint(ScreenPoint position)
        {
            var result = GetAreaOnlySeriesForPoint(position);
            // var result = GetSeriesForPointFairplay(position);
            // var result = GetSeriesForPointFirst(position);

            return result;
        }

        private Series.Series GetSeriesForPointFairplay(ScreenPoint position)
        {
            var result = this.PlotView.ActualModel?.GetSeriesFromPoint(position, limit: this.FiresDistance);
            return result;
        }

        private Series.Series GetAreaOnlySeriesForPoint(ScreenPoint position)
        {
            var plotModel = this.PlotView.ActualModel;
            if (plotModel == null)
            {
                return null;
            }
            else if (plotModel is PlotModelExtended)
            {
                var castedPlotModel = plotModel as PlotModelExtended;
                var result = castedPlotModel.GetSeriesFromPoint(
                    position,
                    seriesPredicate: s => s is Series.AreaSeries,
                    limit: this.FiresDistance);

                return result;
            }
            else
            {
                var result = GetSeriesForPointFairplay(position);
                return result;
            }
        }

        private Series.Series GetSeriesForPointFirst(ScreenPoint position)
        {
            var allSeries = this.PlotView.ActualModel?.Series;
            if (allSeries == null || !allSeries.Any())
            {
                return null;
            }

            var result = allSeries[0];
            return result;
        }


        /// <summary>
        /// Occurs when an input device begins a manipulation on the plot.
        /// </summary>
        /// <param name="e">The <see cref="OxyPlot.OxyTouchEventArgs" /> instance containing the event data.</param>
        public override void Started(OxyTouchEventArgs e)
        {
            base.Started(e);

            // this.currentSeries = this.PlotView.ActualModel?.GetSeriesFromPoint(e.Position, this.FiresDistance);
            this.currentSeries = GetSeriesForPoint(e.Position);
            UpdateTracker(e.Position);

            e.Handled = true;
        }

        /// <summary>
        /// Updates the tracker to the specified position.
        /// </summary>
        /// <param name="position">The position.</param>
        private void UpdateTracker(ScreenPoint position)
        {
            if (this.currentSeries == null || !this.LockToInitialSeries)
            {
                // get the nearest
                // this.currentSeries = this.PlotView.ActualModel?.GetSeriesFromPoint(position, this.FiresDistance);
                this.currentSeries = GetSeriesForPoint(position);
            }

            if (this.currentSeries == null)
            {
                if (!this.LockToInitialSeries)
                {
                    this.PlotView.HideTracker();
                }

                return;
            }

            var actualModel = this.PlotView.ActualModel;
            if (actualModel == null)
            {
                return;
            }

            if (!actualModel.PlotArea.Contains(position.X, position.Y))
            {
                return;
            }

            var result = Utilities.TrackerHelper.GetNearestHit(
                this.currentSeries,
                position,
                this.Snap,
                this.PointsOnly,
                this.FiresDistance,
                this.CheckDistanceBetweenPoints);
            if (result != null)
            {
                result.PlotModel = this.PlotView.ActualModel;
                this.PlotView.ShowTracker(result);
                this.PlotView.ActualModel.RaiseTrackerChanged(result);
            }
        }
    }
}
