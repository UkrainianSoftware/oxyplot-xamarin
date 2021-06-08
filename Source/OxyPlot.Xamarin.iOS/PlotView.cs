// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides a view that can show a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Xamarin.iOS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Foundation;
    using UIKit;

    using OxyPlot;
    using OxyPlot.Series;


    /// <summary>
    /// Provides a view that can show a <see cref="PlotModel" />. 
    /// </summary>
    [Register("PlotView")]
    public class PlotView : UIView, IPlotView
    {
        /// <summary>
        /// The current plot model.
        /// </summary>
        private PlotModel model;

        /// <summary>
        /// The default plot controller.
        /// </summary>
        private IPlotController defaultController;

        /// <summary>
        /// The pan zoom gesture recognizer
        /// </summary>
        private readonly PanZoomGestureRecognizer panZoomGesture = new PanZoomGestureRecognizer();

             

        /// <summary>
        /// The tap gesture recognizer
        /// </summary>
        private readonly UITapGestureRecognizer tapGesture = new UITapGestureRecognizer();
               
        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPlot.Xamarin.iOS.PlotView"/> class.
        /// </summary>
        public PlotView()
        {
            this.Initialize ();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPlot.Xamarin.iOS.PlotView"/> class.
        /// </summary>
        /// <param name="frame">The initial frame.</param>
        public PlotView(CoreGraphics.CGRect frame) : base(frame)
        {
            this.Initialize ();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPlot.Xamarin.iOS.PlotView"/> class.
        /// </summary>
        /// <param name="coder">Coder.</param>
        [Export ("initWithCoder:")]
        public PlotView(NSCoder coder) : base (coder)
        {
            this.Initialize ();
        }

        /// <summary>
        /// Uses the new layout.
        /// </summary>
        /// <returns><c>true</c>, if new layout was used, <c>false</c> otherwise.</returns>
        [Export ("requiresConstraintBasedLayout")]
        private bool UseNewLayout ()
        {
            return true;
        }

        /// <summary>
        /// Initialize the view.
        /// </summary>
        private void Initialize()
        {
            this.UserInteractionEnabled = true;
            this.MultipleTouchEnabled = true;
            this.BackgroundColor = UIColor.White;
            this.KeepAspectRatioWhenPinching = true;

			this.panZoomGesture.AddTarget(this.HandlePanZoomGesture);
			this.tapGesture.AddTarget(this.HandleTapGesture);
			//Prevent panZoom and tap gestures from being recognized simultaneously
			this.tapGesture.RequireGestureRecognizerToFail(this.panZoomGesture);


            // solving conflicts with UIScrollView touches
            // **  without this fix no scrolling happens
            InitHotfixForScrollviewConflicts();

            // Do not intercept touches on overlapping views
            this.panZoomGesture.ShouldReceiveTouch += (recognizer, touch) => touch.View == this;
			this.tapGesture.ShouldReceiveTouch += (recognizer, touch) => touch.View == this;
        }

        private void InitHotfixForScrollviewConflicts()
        {
            // solving conflicts with UIScrollView touches
            // **  without this fix no scrolling happens

            this.tapGesture.CancelsTouchesInView = false;
            this.panZoomGesture.CancelsTouchesInView = false;

            this.panZoomGesture.ShouldRecognizeSimultaneously += (thisGesture, anotherGesture) =>
            {
                bool result = !object.ReferenceEquals(this.tapGesture, anotherGesture);
                return result;
            };

            this.tapGesture.ShouldRecognizeSimultaneously += (thisGesture, anotherGesture) =>
            {
                bool result = !object.ReferenceEquals(this.panZoomGesture, anotherGesture);
                return result;
            };
        }

        public bool IsZoomingAndPanningGestureAllowed { get; set; } = true;
        public bool IsTrackerLineShouldMatchDataPointsExactly { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="PlotModel"/> to show in the view. 
        /// </summary>
        /// <value>The <see cref="PlotModel"/>.</value>
        public PlotModel Model
        {
            get
            {
                return this.model;
            }

            set
            {
                if (this.model != value)
                {
                    if (this.model != null)
                    {
                        ((IPlotModel)this.model).AttachPlotView(null);
                        this.model = null;
                    }

                    if (value != null)
                    {
                        ((IPlotModel)value).AttachPlotView(this);
                        this.model = value;
                    }

                    this.InvalidatePlot();
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IPlotController"/> that handles input events.
        /// </summary>
        /// <value>The <see cref="IPlotController"/>.</value>
        public IPlotController Controller { get; set; }

        /// <summary>
        /// Gets the actual model in the view.
        /// </summary>
        /// <value>
        /// The actual model.
        /// </value>
        Model IView.ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the actual <see cref="PlotModel"/> to show.
        /// </summary>
        /// <value>The actual model.</value>
        public PlotModel ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets the actual controller.
        /// </summary>
        /// <value>
        /// The actual <see cref="IController" />.
        /// </value>
        IController IView.ActualController
        {
            get
            {
                return this.ActualController;
            }
        }

        /// <summary>
        /// Gets the coordinates of the client area of the view.
        /// </summary>
        public OxyRect ClientArea
        {
            get
            {
                // TODO
                return new OxyRect(0, 0, 100, 100);
            }
        }

        /// <summary>
        /// Gets the actual <see cref="IPlotController"/>.
        /// </summary>
        /// <value>The actual plot controller.</value>
        public IPlotController ActualController
        {
            get
            {
                return this.Controller ?? (this.defaultController ?? (this.defaultController = new PlotController()));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="OxyPlot.Xamarin.iOS.PlotView"/> keeps the aspect ratio when pinching.
        /// </summary>
        /// <value><c>true</c> if keep aspect ratio when pinching; otherwise, <c>false</c>.</value>
        public bool KeepAspectRatioWhenPinching
        {
            get { return this.panZoomGesture.KeepAspectRatioWhenPinching; }
            set { this.panZoomGesture.KeepAspectRatioWhenPinching = value; }
        }

        /// <summary>
        /// How far apart touch points must be on a certain axis to enable scaling that axis.
        /// (only applies if KeepAspectRatioWhenPinching == false)
        /// </summary>
        public double ZoomThreshold
        {
            get { return this.panZoomGesture.ZoomThreshold; }
            set { this.panZoomGesture.ZoomThreshold = value; }
        }

        /// <summary>
        /// If <c>true</c>, and KeepAspectRatioWhenPinching is <c>false</c>, a zoom-out gesture
        /// can turn into a zoom-in gesture if the fingers cross. Setting to <c>false</c> will
        /// instead simply stop the zoom at that point.
        /// </summary>
        public bool AllowPinchPastZero
        {
            get { return this.panZoomGesture.AllowPinchPastZero; }
            set { this.panZoomGesture.AllowPinchPastZero = value; }
        }

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        public void HideTracker()
        {
            _isTrackerVerticalLineVisible = false;
            InvalidatePlot(updateData: false);
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        public void HideZoomRectangle()
        {
        }

        /// <summary>
        /// Invalidates the plot (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">If set to <c>true</c> update data.</param>
        public void InvalidatePlot(bool updateData = true)
        {
            var actualModel = this.model;
            if (actualModel != null)
            {
                // TODO: update the model on a background thread
                ((IPlotModel)actualModel).Update(updateData);
            }

            this.SetNeedsDisplay();
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        public void SetCursorType(CursorType cursorType)
        {
            // No cursor on iOS
        }

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="trackerHitResult">The tracker data.</param>
        public void ShowTracker(TrackerHitResult trackerHitResult)
        {
            // TODO: how to show a tracker on iOS
            // the tracker must be moved away from the finger...

#if DEBUG
            System.Console.WriteLine(
                $"[oxyplot] [ios] ShowTracker() | x={trackerHitResult.Position.X} ; y={trackerHitResult.Position.Y} | Item={trackerHitResult.Item}");
#endif

            _lastTrackerHitResult = trackerHitResult;
            _isTrackerVerticalLineVisible = true;

            InvalidatePlot(updateData: false);
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        public void ShowZoomRectangle(OxyRect rectangle)
        {
            // Not needed - better with pinch events on iOS?
        }

        /// <summary>
        /// Stores text on the clipboard.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetClipboardText(string text)
        {
            UIPasteboard.General.SetValue(new NSString(text), "public.utf8-plain-text");
        }

        /// <summary>
        /// Draws the content of the view.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        public override void Draw(CoreGraphics.CGRect rect)
        {
            var actualModel = (IPlotModel)this.model;
            if (actualModel != null)
            {
                var context = UIGraphics.GetCurrentContext ();
                using (var renderer = new CoreGraphicsRenderContext(context))
                {
                    if (actualModel.Background.IsVisible())
                    {
                        context.SetFillColor (actualModel.Background.ToCGColor ());
                        context.FillRect (rect);
                    }

                    actualModel.Render(renderer, rect.Width, rect.Height);

                    DrawVerticalLineOfTrackerIfNeeded(
                        rect,
                        this.model,
                        renderer,
                        context);
                }
            }
        }

        /// <summary>
        /// Method invoked when a motion (a shake) has started.
        /// </summary>
        /// <param name="motion">The motion subtype.</param>
        /// <param name="evt">The event arguments.</param>
        public override void MotionBegan(UIEventSubtype motion, UIEvent evt)
        {
            base.MotionBegan(motion, evt);
            if (motion == UIEventSubtype.MotionShake)
            {
                this.ActualController.HandleGesture(this, new OxyShakeGesture(), new OxyKeyEventArgs());
            }
        }

		/// <summary>
		/// Used to add/remove the gesture recognizer so that it
		/// doesn't prevent the PlotView from being garbage-collected.
		/// </summary>
		/// <param name="newsuper">New superview</param>
		public override void WillMoveToSuperview (UIView newsuper)
		{
			if (newsuper == null)
			{
				this.RemoveGestureRecognizer (this.panZoomGesture);
				this.RemoveGestureRecognizer (this.tapGesture);
			}
			else if (this.Superview == null)
			{
				this.AddGestureRecognizer (this.panZoomGesture);
				this.AddGestureRecognizer (this.tapGesture);
			}

			base.WillMoveToSuperview (newsuper);
		}

        private void HandlePanZoomGesture()
        {
            if (!this.IsZoomingAndPanningGestureAllowed)
            {
                return;
            }

            switch (this.panZoomGesture.State)
            {
                case UIGestureRecognizerState.Began:
                    this.ActualController.HandleTouchStarted(this, this.panZoomGesture.TouchEventArgs);
                    break;
                case UIGestureRecognizerState.Changed:
                    this.ActualController.HandleTouchDelta(this, this.panZoomGesture.TouchEventArgs);
                    break;
                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                    this.ActualController.HandleTouchCompleted(this, this.panZoomGesture.TouchEventArgs);
                    break;
            }
        }

		private void HandleTapGesture()
		{
			var location = this.tapGesture.LocationInView(this);
            this.ActualController.HandleTouchStarted(this, location.ToTouchEventArgs());
            this.ActualController.HandleTouchCompleted(this, location.ToTouchEventArgs());
		}


        private void DrawVerticalLineOfTrackerIfNeeded(
            CoreGraphics.CGRect rect,
            PlotModel actualModel,
            CoreGraphicsRenderContext renderer,
            CoreGraphics.CGContext context)
        {
            // TODO: [@dodikk] might not need the logic below
            // with PlotCommands.PointsOnlyTrackTouch
            // ---
            // https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/PlotController/PlotCommands.cs#L45
            // https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/PlotController/Manipulators/TouchTrackerManipulator.cs
            // -

            if (!_isTrackerVerticalLineVisible)
            {
                return;
            }

            // TODO: [@dodikk] maybe expose as bindings of PlotView
            // * in case our other charts need different colors
            // * or before sending a PR to lib maintainers
            // -
            var verticalLinePen = new OxyPen(
                    color: OxyColors.Black,
                    thickness: 2,
                    lineStyle: LineStyle.Solid);


            var allSeries = actualModel.Series;
            if (allSeries == null || !allSeries.Any())
            {
                // Note: [@dodikk] typically this is not supposed to happen
                // but when it does - just draw the line "as is"
                // ---
                // might be a good idea to NOT draw that vertical line at all
                // -

                renderer.DrawLine(
                    points: new List<ScreenPoint>()
                    {
                        new ScreenPoint(
                            _lastTrackerHitResult.Position.X,
                            actualModel.PlotAndAxisArea.Bottom),
                        new ScreenPoint(
                            _lastTrackerHitResult.Position.X,
                            actualModel.PlotAndAxisArea.Top),
                    },
                    stroke: verticalLinePen.Color,
                    thickness: verticalLinePen.Thickness,
                    dashArray: null,
                    lineJoin: verticalLinePen.LineJoin,
                    aliased: false);
            }
            else if (IsTrackerLineShouldMatchDataPointsExactly)
            {
                var someLineSeries =
                    allSeries.Where(s => s != null)
                             .Where(s => s is LineSeries)
                             .Select(s => s as LineSeries)
                             .FirstOrDefault();

                int indexOfNearestDataPoint =
                    (int)Math.Round(_lastTrackerHitResult.Index);

                if (someLineSeries == null)
                {
                    // Note: [@dodikk] less precise calculation.
                    // But might somehow work with other series like columns, etc
                    // -
                    double xCoordinateOfHitTest =
                        _lastTrackerHitResult.Position.X;

                    // TODO: [xm-939] handle null or negative index properly
                    //       if that even happens "in real life"
                    // ---
                    // maybe need a more precise "zero delta"
                    // like 0.1 or 0.001
                    // -
                    double screenLengthPerHorizontalIndexPoint =
                        (_lastTrackerHitResult.Index <= 1)
                        ? (double)1
                        : xCoordinateOfHitTest / _lastTrackerHitResult.Index;

                    double xCoordinateNormalized =
                        indexOfNearestDataPoint * screenLengthPerHorizontalIndexPoint;


                    renderer.DrawLine(
                        points: new List<ScreenPoint>()
                        {
                            new ScreenPoint(
                                xCoordinateNormalized, //_lastTrackerHitResult.Position.X,
                                actualModel.PlotAndAxisArea.Bottom),
                            new ScreenPoint(
                                xCoordinateNormalized, //_lastTrackerHitResult.Position.X,
                                actualModel.PlotAndAxisArea.Top),
                        },
                        stroke: verticalLinePen.Color,
                        thickness: verticalLinePen.Thickness,
                        dashArray: null,
                        lineJoin: verticalLinePen.LineJoin,
                        aliased: false);
                }
                else
                {
                    // Note: [@dodikk] more precise calculation.
                    // tailored specifically for line series
                    // ---
                    // aiming for behaviour
                    // like in the Stocks.app
                    // for few data points
                    // -


                    var castedLineSeries = someLineSeries as LineSeries;
                    int totalNumberOfDataPoints = castedLineSeries.Points.Count();

                    // Note: [@dodikk] cannot use PlotView binding
                    // since that freezes droid app
                    // no idea how to avoid hardcode yet
                    // -
                    int smallDatasetPointsCount = 32; // dataset of one month +-1 day

                    bool isSmallDataset = (totalNumberOfDataPoints <= smallDatasetPointsCount);

                    if (isSmallDataset)
                    {
                        DataPoint nearestDataPoint = castedLineSeries.Points[indexOfNearestDataPoint];

                        ScreenPoint screenCoordinatesOfNearestDataPoint =
                            castedLineSeries.Transform(nearestDataPoint);

                        double trackerLineX = screenCoordinatesOfNearestDataPoint.X;


                        renderer.DrawLine(
                            points: new List<ScreenPoint>()
                            {
                                new ScreenPoint(
                                    trackerLineX,
                                    actualModel.PlotAndAxisArea.Bottom),
                                new ScreenPoint(
                                    trackerLineX,
                                    actualModel.PlotAndAxisArea.Top),
                            },
                            stroke: verticalLinePen.Color,
                            thickness: verticalLinePen.Thickness,
                            dashArray: null,
                            lineJoin: verticalLinePen.LineJoin,
                            aliased: false);
                    }
                    else
                    {
                        renderer.DrawLine(
                            points: new List<ScreenPoint>()
                            {
                                new ScreenPoint(
                                    _lastTrackerHitResult.Position.X,
                                    actualModel.PlotAndAxisArea.Bottom),
                                new ScreenPoint(
                                    _lastTrackerHitResult.Position.X,
                                    actualModel.PlotAndAxisArea.Top),
                            },
                            stroke: verticalLinePen.Color,
                            thickness: verticalLinePen.Thickness,
                            dashArray: null,
                            lineJoin: verticalLinePen.LineJoin,
                            aliased: false);
                    }
                }
            }
            else
            {
                // Note: [@dodikk] coordinates "as is" are good for large datasets
                // when all data points do not fit into canvas pixels
                // and some interpolation happens
                // ---
                // Highlighting individual points makes no sense in this case
                // so the logic above is not needed
                // -

                renderer.DrawLine(
                    points: new List<ScreenPoint>()
                    {
                        new ScreenPoint(
                            _lastTrackerHitResult.Position.X,
                            actualModel.PlotAndAxisArea.Bottom),
                        new ScreenPoint(
                            _lastTrackerHitResult.Position.X,
                            actualModel.PlotAndAxisArea.Top),
                    },
                    stroke: verticalLinePen.Color,
                    thickness: verticalLinePen.Thickness,
                    dashArray: null,
                    lineJoin: verticalLinePen.LineJoin,
                    aliased: false);
            }
        }


        private bool _isTrackerVerticalLineVisible = false;
        private TrackerHitResult _lastTrackerHitResult = null;
    }
}