﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a view that can show a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Xamarin.Android
{
    using System;
    using System.Linq;

    using global::Android.Content;
    using global::Android.Graphics;
    using global::Android.Util;
    using global::Android.Views;

    using OxyPlot;
    using OxyPlot.Series;


    /// <summary>
    /// Represents a view that can show a <see cref="PlotModel" />.
    /// </summary>
    public class PlotView : View, IPlotView
    {
        /// <summary>
        /// The factor that scales from OxyPlot´s device independent pixels (96 dpi) to 
        /// Android´s current density-independent pixels (dpi).
        /// </summary>
        /// <remarks>See <a href="http://developer.android.com/guide/practices/screens_support.html">Supporting multiple screens.</a>.</remarks>
        public double Scale;

        /// <summary>
        /// The rendering lock object.
        /// </summary>
        private readonly object renderingLock = new object();

        /// <summary>
        /// The invalidation lock object.
        /// </summary>
        private readonly object invalidateLock = new object();

        /// <summary>
        /// The touch points of the previous touch event.
        /// </summary>
        private ScreenPoint[] previousTouchPoints;

        /// <summary>
        /// The current model.
        /// </summary>
        private PlotModel model;

        /// <summary>
        /// The default controller
        /// </summary>
        private IPlotController defaultController;

        /// <summary>
        /// The current render context.
        /// </summary>
        private CanvasRenderContext rc;

        /// <summary>
        /// The model invalidated flag.
        /// </summary>
        private bool isModelInvalidated;

        /// <summary>
        /// The update data flag.
        /// </summary>
        private bool updateDataFlag = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks>Use this constructor when creating the view from code.</remarks>
        public PlotView(Context context) :
            base(context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="attrs">The attribute set.</param>
        /// <remarks>This constructor is called when inflating the view from XML.</remarks>
        public PlotView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="attrs">The attribute set.</param>
        /// <param name="defStyle">The definition style.</param>
        /// <remarks>This constructor performs inflation from XML and applies a class-specific base style.</remarks>
        public PlotView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
        }

        public bool IsZoomingAndPanningGestureAllowed { get; set; } = true;
        public bool IsTrackerLineShouldMatchDataPointsExactly { get; set; } = false;


        /// <summary>
        /// Gets or sets the plot model.
        /// </summary>
        /// <value>The model.</value>
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
        /// Gets or sets the plot controller.
        /// </summary>
        /// <value>The controller.</value>
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
        /// Gets the actual <see cref="PlotModel" /> of the control.
        /// </summary>
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
                return new OxyRect(0, 0, this.Width, this.Height);
            }
        }

        /// <summary>
        /// Gets the actual <see cref="IPlotController" /> of the control.
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
        /// Hides the tracker.
        /// </summary>
        public void HideTracker()
        {
            this._isTrackerVerticalLineVisible = false;
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
        /// <param name="updateData">if set to <c>true</c>, all data bindings will be updated.</param>
        public void InvalidatePlot(bool updateData = true)
        {
            lock (this.invalidateLock)
            {
                this.isModelInvalidated = true;
                this.updateDataFlag = this.updateDataFlag || updateData;
            }

            this.Invalidate();
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        public void SetCursorType(CursorType cursorType)
        {
        }

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="trackerHitResult">The tracker data.</param>
        public void ShowTracker(TrackerHitResult trackerHitResult)
        {
#if DEBUG
            System.Console.WriteLine(
                $"[oxyplot] [droid] ShowTracker() | x={trackerHitResult.Position.X} ; y={trackerHitResult.Position.Y} | Item={trackerHitResult.Item}");
#endif

            this._lastTrackerHitResult = trackerHitResult;
            this._isTrackerVerticalLineVisible = true;
            InvalidatePlot(updateData: false);
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        public void ShowZoomRectangle(OxyRect rectangle)
        {
        }

        /// <summary>
        /// Stores text on the clipboard.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetClipboardText(string text)
        {
        }

        /// <summary>
        /// Handles key down events.
        /// </summary>
        /// <param name="keyCode">The key code.</param>
        /// <param name="e">The event arguments.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        //public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        //{
        //    var handled = base.OnKeyDown(keyCode, e);
        //    if (!handled)
        //    {
        //        handled = this.ActualController.HandleKeyDown(this, e.ToKeyEventArgs());
        //    }
        //
        //    return handled;
        //}

        /// <summary>
        /// Handles touch screen motion events.
        /// </summary>
        /// <param name="e">The motion event arguments.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        public override bool OnTouchEvent(MotionEvent e)
        {
            //bool handled = true;

            bool handled = base.OnTouchEvent(e);
            //if (!handled)
            //{
                switch (e.ActionMasked)
                {
                    case MotionEventActions.Down:
                        handled = this.OnTouchDownEvent(e);
                        break;
                    case MotionEventActions.Move:
                        handled = this.OnTouchMoveEvent(e);
                        break;
                    case MotionEventActions.Up:
                        handled = this.OnTouchUpEvent(e);
                        break;
                }
            //}

            return handled;
        }

        /// <summary>
        /// Draws the content of the control.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            var actualModel = this.ActualModel;
            if (actualModel == null)
            {
                return;
            }

            if (actualModel.Background.IsVisible())
            {
                canvas.DrawColor(actualModel.Background.ToColor());
            }
            else
            {
                // do nothing
            }

            lock (this.invalidateLock)
            {
                if (this.isModelInvalidated)
                {
                    ((IPlotModel)actualModel).Update(this.updateDataFlag);
                    this.updateDataFlag = false;
                    this.isModelInvalidated = false;
                }
            }

            lock (this.renderingLock)
            {
                if (this.rc == null)
                {
                    var displayMetrics = this.Context.Resources.DisplayMetrics;

                    // The factors for scaling to Android's DPI and SPI units.
                    // The density independent pixel is equivalent to one physical pixel 
                    // on a 160 dpi screen (baseline density)
                    this.Scale = displayMetrics.Density;
                    this.rc = new CanvasRenderContext(Scale, displayMetrics.ScaledDensity);
                }

                this.rc.SetTarget(canvas);
                
                ((IPlotModel)actualModel).Render(this.rc, Width / Scale, Height / Scale);

                DrawVerticalLineOfTrackerIfNeeded(onCanvas: canvas);
            }
        }

        private void DrawVerticalLineOfTrackerIfNeeded(Canvas onCanvas)
        {
            // TODO: [@dodikk] might not need the logic below
            // with PlotCommands.PointsOnlyTrackTouch
            // ---
            // https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/PlotController/PlotCommands.cs#L45
            // https://github.com/oxyplot/oxyplot/blob/075d1b3808946e0661c0544af248dfdc3a898ebc/Source/OxyPlot/PlotController/Manipulators/TouchTrackerManipulator.cs
            // -


            Canvas canvas = onCanvas;

            if (!_isTrackerVerticalLineVisible)
            {
                return;
            }

            var actualModel = this.ActualModel;
            if (actualModel == null)
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

            this.rc.DrawLine(
                x0: _lastTrackerHitResult.Position.X,
                y0: actualModel.PlotAndAxisArea.Bottom,
                x1: _lastTrackerHitResult.Position.X,
                y1: actualModel.PlotAndAxisArea.Top,
                pen: verticalLinePen);


            // -----
            // Note: the logic below makes tracker drag unexpected
            //       so we've removed it.
            // Might make sense for tap based tracker, etc.
            // Feel free uncommenting it in that case.
            // -----

            //var allSeries = actualModel.Series;
            //if (allSeries == null || !allSeries.Any())
            //{
            //    // Note: [@dodikk] typically this is not supposed to happen
            //    // but when it does - just draw the line "as is"
            //    // ---
            //    // might be a good idea to NOT draw that vertical line at all
            //    // -
            //    this.rc.DrawLine(
            //        x0: _lastTrackerHitResult.Position.X,
            //        y0: actualModel.PlotAndAxisArea.Bottom,
            //        x1: _lastTrackerHitResult.Position.X,
            //        y1: actualModel.PlotAndAxisArea.Top,
            //        pen: verticalLinePen);
            //}
            //else if (IsTrackerLineShouldMatchDataPointsExactly)
            //{
            //    var someLineSeries =
            //        allSeries.Where(s => s != null)
            //                 .Where(s => s is LineSeries)
            //                 .Select(s => s as LineSeries)
            //                 .FirstOrDefault();
            //
            //    int indexOfNearestDataPoint =
            //        (int)Math.Round(_lastTrackerHitResult.Index);
            //
            //    if (someLineSeries == null)
            //    {
            //        // Note: [@dodikk] less precise calculation.
            //        // But might somehow work with other series like columns, etc
            //        // -
            //        double xCoordinateOfHitTest =
            //            _lastTrackerHitResult.Position.X;
            //
            //        // TODO: [xm-939] handle null or negative index properly
            //        //       if that even happens "in real life"
            //        // ---
            //        // maybe need a more precise "zero delta"
            //        // like 0.1 or 0.001
            //        // -
            //        double screenLengthPerHorizontalIndexPoint =
            //            (_lastTrackerHitResult.Index <= 1)
            //            ? (double)1
            //            : xCoordinateOfHitTest / _lastTrackerHitResult.Index;
            //
            //        double xCoordinateNormalized =
            //            indexOfNearestDataPoint * screenLengthPerHorizontalIndexPoint;
            //
            //        this.rc.DrawLine(
            //            x0: xCoordinateNormalized, //_lastTrackerHitResult.Position.X,
            //            y0: actualModel.PlotAndAxisArea.Bottom,
            //            x1: xCoordinateNormalized, //_lastTrackerHitResult.Position.X,
            //            y1: actualModel.PlotAndAxisArea.Top,
            //            pen: verticalLinePen);
            //    }
            //    else
            //    {
            //        // Note: [@dodikk] more precise calculation.
            //        // tailored specifically for line series
            //        // ---
            //        // aiming for behaviour
            //        // like in the Stocks.app
            //        // for few data points
            //        // -
            //
            //        
            //        var castedLineSeries = someLineSeries as LineSeries;
            //        int totalNumberOfDataPoints = castedLineSeries.Points.Count();
            //
            //        // Note: [@dodikk] cannot use PlotView binding
            //        // since that freezes droid app
            //        // no idea how to avoid hardcode yet
            //        // -
            //        int smallDatasetPointsCount = 32; // dataset of one month +-1 day
            //
            //        bool isSmallDataset = (totalNumberOfDataPoints <= smallDatasetPointsCount);
            //
            //        if (isSmallDataset)
            //        {
            //            DataPoint nearestDataPoint = castedLineSeries.Points[indexOfNearestDataPoint];
            //
            //            ScreenPoint screenCoordinatesOfNearestDataPoint =
            //                castedLineSeries.Transform(nearestDataPoint);
            //
            //            double trackerLineX = screenCoordinatesOfNearestDataPoint.X;
            //
            //            this.rc.DrawLine(
            //                x0: trackerLineX,
            //                y0: actualModel.PlotAndAxisArea.Bottom,
            //                x1: trackerLineX,
            //                y1: actualModel.PlotAndAxisArea.Top,
            //                pen: verticalLinePen);
            //        }
            //        else
            //        {
            //            this.rc.DrawLine(
            //                x0: _lastTrackerHitResult.Position.X,
            //                y0: actualModel.PlotAndAxisArea.Bottom,
            //                x1: _lastTrackerHitResult.Position.X,
            //                y1: actualModel.PlotAndAxisArea.Top,
            //                pen: verticalLinePen);
            //        }
            //    }
            //}
            //else
            //{
            //    // Note: [@dodikk] coordinates "as is" are good for large datasets
            //    // when all data points do not fit into canvas pixels
            //    // and some interpolation happens
            //    // ---
            //    // Highlighting individual points makes no sense in this case
            //    // so the logic above is not needed
            //    // -
            //
            //    this.rc.DrawLine(
            //        x0: _lastTrackerHitResult.Position.X,
            //        y0: actualModel.PlotAndAxisArea.Bottom,
            //        x1: _lastTrackerHitResult.Position.X,
            //        y1: actualModel.PlotAndAxisArea.Top,
            //        pen: verticalLinePen);
            //}
        }

        /// <summary>
        /// Handles touch down events.
        /// </summary>
        /// <param name="e">The motion event arguments.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        private bool OnTouchDownEvent(MotionEvent e)
        {
            var args = e.ToTouchEventArgs(Scale);
            /*var handled*/ _ = this.ActualController.HandleTouchStarted(this, args);
            this.previousTouchPoints = e.GetTouchPoints(Scale);


            // Note: [@dodikk] [proto] for some reason Manipulator.Delta() is not called
            // trying to tinker with this result
            // Force |return false| ==> does not help
            // Force |return true | ==> seems better than |return handled|
            // (at least one OnTouchMoveEvent() arrives
            //  but next events do not)
            // -
            return true;
        }

        /// <summary>
        /// Handles touch move events.
        /// </summary>
        /// <param name="e">The motion event arguments.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        private bool OnTouchMoveEvent(MotionEvent e)
        {
            ScreenPoint[] currentTouchPoints = e.GetTouchPoints(Scale);
            var args = new OxyTouchEventArgs(currentTouchPoints, this.previousTouchPoints);
            /*var handled*/ _ = this.ActualController.HandleTouchDelta(this, args);
            this.previousTouchPoints = currentTouchPoints;


            // Note: [@dodikk] [proto] for some reason Manipulator.Delta() is not called
            // trying to tinker with this result
            // Force |return false| ==> does not help
            // Force |return true | ==> ???
            // -
            return true;


            // ???
            // Thats the declaration of my activity. and to detect swipes i have implemented that:
            // @Override
            // public boolean onFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
            // https://stackoverflow.com/questions/13083871/method-ontouchevent-not-being-called
            // ---
            // [!] https://guides.codepath.com/android/Gestures-and-Touch-Events
            // https://xmonkeys360.com/2021/01/04/xamarin-android-gesture-detector/
            // [?] https://github.com/softlion/XamarinFormsGesture/blob/master/XamarinFormsGesture/Android/PlatformGestureEffect.cs
            // https://github.com/xamarin/xamarin-forms-samples/blob/main/Effects/TouchTrackingEffect/TouchTrackingEffect/TouchTrackingEffect.Droid/TouchEffect.cs
            // https://github.com/xamarin/xamarin-forms-samples/blob/main/Effects/TouchTrackingEffect/TouchTrackingEffect/TouchTrackingEffect.Droid/TouchEffect.cs#L79
            // https://gist.github.com/k3a/ef98593a3571b1b399169448327ad333
            // ---

        }

        /// <summary>
        /// Handles touch released events.
        /// </summary>
        /// <param name="e">The motion event arguments.</param>
        /// <returns><c>true</c> if the event was handled.</returns>
        private bool OnTouchUpEvent(MotionEvent e)
        {
            /*bool result*/ _ = this.ActualController.HandleTouchCompleted(this, e.ToTouchEventArgs(Scale));
            return true; // result;
        }


        private bool _isTrackerVerticalLineVisible = false;
        private TrackerHitResult _lastTrackerHitResult = null;
    }
}
