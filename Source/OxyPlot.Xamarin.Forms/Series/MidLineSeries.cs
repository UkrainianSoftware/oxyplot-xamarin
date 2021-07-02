using Xamarin.Forms;

namespace OxyPlot
{
    public class MidLineSeries : LineSeriesForDragTracker
    {
        // Note: [alex-d] right, no custom logic here.
        //       this class is for lookup in PlotModelExtended.cs
        // -

        public MidLineSeries()
        {
            IsVisible = true;
            Color = OxyColors.Transparent;

            //byte colorAlpha = 0;
            //Color = OxyColor.FromAColor(
            //    a: colorAlpha,
            //    color: OxyColors.Pink);
        }
    }
}
