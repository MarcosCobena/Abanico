using System;
using System.Threading.Tasks;
using SkiaSharp;
using Xamarin.Forms;

namespace HSVRange
{
    public partial class HSVRangePage : ContentPage
    {
        int stepsOutterRange = 360;
		float angleOutterRange = 0;
		float angleInnerRange = 0;
        bool drawText = false;
        bool valueEnabled = true;
        bool hslOrHsvEnabled = true;

        public HSVRangePage()
        {
            InitializeComponent();

            canvasView.Opacity = 0;
            toolBarAbsoluteLayout.Opacity = 0;
            innerRandeAngleSlider.Opacity = 0;
            outterRandeAngleSlider.Opacity = 0;
            stepsSlider.Opacity = 0;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var appearingAnim = new Animation(steps =>
            {
                this.stepsOutterRange = (int)steps;
                canvasView.InvalidateSurface();
            }, stepsOutterRange, stepsSlider.Minimum, Easing.SinOut);

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await canvasView.FadeTo(1, 1000, Easing.SinInOut);
                appearingAnim.Commit(this, "Appearing", length: 5000, finished: async (_, __) =>
	            {
                    uint duration = 200;
                    var easing = Easing.CubicOut;
                    await toolBarAbsoluteLayout.FadeTo(1, duration, easing);
                    await stepsSlider.FadeTo(1, duration, easing);
                    await outterRandeAngleSlider.FadeTo(1, duration, easing);
                    await innerRandeAngleSlider.FadeTo(1, duration, easing);

	                drawText = true;
                    canvasView.InvalidateSurface();

                    outterRandeAngleSlider.Maximum = innerRandeAngleSlider.Maximum = stepsSlider.Value;
	            });
            });
        }

        void Handle_Tapped(object sender, System.EventArgs e)
        {
            valueEnabled = !valueEnabled;
            canvasView.InvalidateSurface();
        }

        //void Handle_Toggled(object sender, Xamarin.Forms.ToggledEventArgs e)
        //{
        //    hslOrHsvEnabled = !hslOrHsvEnabled;
        //    canvasView?.InvalidateSurface();
        //}

        void InnerRandeAngleSlider_ValueChanged(object sender, ValueChangedEventArgs args)
        {
            UpdateAngleInnerRange(args.NewValue);
            canvasView.InvalidateSurface();
        }

        void UpdateAngleInnerRange(double value)
        {
            angleInnerRange = (float)(Math.Floor(value) * (360 / innerRandeAngleSlider.Maximum));
        }

        void OutterRandeAngleSlider_ValueChanged(object sender, ValueChangedEventArgs args)
        {
            UpdateAngleOutterRange(args.NewValue);
            canvasView.InvalidateSurface();
        }

        private void UpdateAngleOutterRange(double value)
        {
            angleOutterRange = (float)(Math.Floor(value) * (360 / outterRandeAngleSlider.Maximum));
        }

        void StepsSlider_ValueChanged(object sender, Xamarin.Forms.ValueChangedEventArgs e)
        {
			stepsOutterRange = (int)e.NewValue;

            var prevOutterValue = outterRandeAngleSlider.Value;
            var prevOutterMaximum = outterRandeAngleSlider.Maximum;
            outterRandeAngleSlider.Maximum = stepsOutterRange;
            outterRandeAngleSlider.Value = (stepsOutterRange * prevOutterValue) / prevOutterMaximum;
            UpdateAngleOutterRange(outterRandeAngleSlider.Value);

            var prevInnerValue = innerRandeAngleSlider.Value;
            var prevInnerMaximum = innerRandeAngleSlider.Maximum;
            innerRandeAngleSlider.Maximum = stepsOutterRange;
            innerRandeAngleSlider.Value = (stepsOutterRange * prevInnerValue) / prevInnerMaximum;
            UpdateAngleInnerRange(innerRandeAngleSlider.Value);

            canvasView.InvalidateSurface();
        }

        void Handle_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.Clear(hslOrHsvEnabled ? SKColors.White : SKColors.Black);
            DrawRange(e.Surface.Canvas, e.Info.Width, e.Info.Height, angleOutterRange, stepsOutterRange, 
                      (w, h) => Math.Min(w, h) / 2 - (float)toolbarStackLayout.Margin.Bottom, 50, drawText);
            DrawRange(e.Surface.Canvas, e.Info.Width, e.Info.Height, 0, stepsOutterRange, 
                      (w, h) => ((Math.Min(w, h) / 2) / 3) * 2, valueEnabled ? 67 : 50, drawText);
            DrawRange(e.Surface.Canvas, e.Info.Width, e.Info.Height, angleInnerRange, stepsOutterRange, 
                      (w, h) => (Math.Min(w, h) / 2) / 3, valueEnabled ? 84 : 50, drawText);
        }

        void DrawRange(SKCanvas canvas, int width, int height, float degreesOffset, int steps,
                       Func<int, int, float> calcStepSide, float value, bool drawColorValuesAsText = false)
        {
            var degreesStep = 360f / steps;
            var halfWidth = width / 2f;
            var halfHeight = height / 2f;
            var stepSide = calcStepSide(width, height);
            var scale = width / (float)canvasView.Width;
            var textBrush = new SKPaint 
            { 
                IsAntialias = true, 
                Style = SKPaintStyle.Fill, 
                Color = SKColors.White,
                TextSize = 10 * scale
            };
            var arcRect = new SKRect(halfWidth - stepSide, halfHeight - stepSide, 
                                     halfWidth + stepSide, halfHeight + stepSide);

            for (var i = 0; i < steps; i++)
            {
                var degrees = i * degreesStep;
                var degreesWithOffset = degrees + degreesOffset;
                var brush = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = hslOrHsvEnabled ? 
                        SKColor.FromHsl(degrees, 100, value, 255) : 
                        SKColor.FromHsv(degrees, 100, value, 255)
                };

                canvas.RotateDegrees(degreesWithOffset, halfWidth, halfHeight);
                using (var path = new SKPath())
                {
                    path.MoveTo(halfWidth, halfHeight);
                    //path.LineTo(halfWidth, halfHeight - stepSide);
                    //path.LineTo(halfWidth + (float)Math.Tan((Math.PI / 180) * degreesStep) * stepSide, halfHeight - stepSide);
                    path.ArcTo(arcRect, 270, degreesStep, false);
                    path.Close();
                    canvas.DrawPath(path, brush);
                }

                if (drawColorValuesAsText)
                {
                    var textX = halfWidth;
                    var textY = halfHeight - 3 * (stepSide / 4);
                    canvas.DrawText(brush.Color.ToString(), textX, textY, textBrush);
                }

                canvas.RotateDegrees(-degreesWithOffset, halfWidth, halfHeight);
            }
        }
    }
}
