﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a visual element that displays a PlotModel.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Xamarin.Forms
{
    using System;

    using OxyPlot;

    using global::Xamarin.Forms;

    /// <summary>
    /// Represents a visual element that displays a <see cref="PlotModel" />.
    /// </summary>
    public class PlotView : View
    {
        /// <summary>
        /// Identifies the <see cref="Controller" />  bindable property.
        /// </summary>
        public static readonly BindableProperty ControllerProperty =
            BindableProperty.Create(
                propertyName: nameof(Controller),
                returnType: typeof(PlotController),
                declaringType: typeof(PlotView));

        /// <summary>
        /// Identifies the <see cref="Model" />  bindable property.
        /// </summary>
        public static readonly BindableProperty ModelProperty =
            BindableProperty.Create(
                propertyName: nameof(Model),
                returnType: typeof(PlotModel),
                declaringType: typeof(PlotView));


        public static readonly BindableProperty IsZoomingAndPanningGestureAllowedProperty =
            BindableProperty.Create(
                propertyName: nameof(IsZoomingAndPanningGestureAllowed),
                returnType: typeof(bool),
                declaringType: typeof(PlotView));

        public static readonly BindableProperty IsTrackerLineShouldMatchDataPointsExactlyProperty =
            BindableProperty.Create(
                propertyName: nameof(IsTrackerLineShouldMatchDataPointsExactly),
                returnType: typeof(bool),
                declaringType: typeof(PlotView),
                defaultValue: false);


        public bool IsZoomingAndPanningGestureAllowed
        {
            get { return (bool)this.GetValue(IsZoomingAndPanningGestureAllowedProperty); }
            set { this.SetValue(IsZoomingAndPanningGestureAllowedProperty, value); }
        }

        public bool IsTrackerLineShouldMatchDataPointsExactly
        {
            get { return (bool)this.GetValue(IsTrackerLineShouldMatchDataPointsExactlyProperty); }
            set { this.SetValue(IsTrackerLineShouldMatchDataPointsExactlyProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Renderer is not initialized</exception>
        public PlotView()
        {
            if (!IsRendererInitialized && !DesignMode.IsDesignModeEnabled)
            {
                var platform = Device.RuntimePlatform == Device.macOS ? "MacOS" : Device.RuntimePlatform.ToString();
                throw new InvalidOperationException(
                    "Renderer is not initialized.\nRemember to call `OxyPlot.Xamarin.Forms.Platform." + platform +
                    ".PlotViewRenderer.Init();` after `Xamarin.Forms.Forms.Init(e);` in the " + platform +
                    " app project.");
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="PlotModel"/> to view.
        /// </summary>
        /// <value>The model.</value>
        public PlotModel Model
        {
            get { return (PlotModel)this.GetValue(ModelProperty); }
            set { this.SetValue(ModelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="PlotController"/> for the view.
        /// </summary>
        /// <value>The controller.</value>
        public PlotController Controller
        {
            get { return (PlotController)this.GetValue(ControllerProperty); }
            set { this.SetValue(ControllerProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the renderer is "initialized".
        /// </summary>
        /// <value><c>true</c> if the renderer is initialized; otherwise, <c>false</c>.</value>
        public static bool IsRendererInitialized { get; set; }
    }
}
