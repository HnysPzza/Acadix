using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace AcadsJulie.Controls
{
    public class RingTimerControl : SKCanvasView
    {
        public static readonly BindableProperty MaxTimeProperty =
            BindableProperty.Create(nameof(MaxTime), typeof(double), typeof(RingTimerControl), 8.0, propertyChanged: OnPropertyChanged);

        public static readonly BindableProperty TimeLeftProperty =
            BindableProperty.Create(nameof(TimeLeft), typeof(double), typeof(RingTimerControl), 8.0, propertyChanged: OnPropertyChanged);

        public double MaxTime
        {
            get => (double)GetValue(MaxTimeProperty);
            set => SetValue(MaxTimeProperty, value);
        }

        public double TimeLeft
        {
            get => (double)GetValue(TimeLeftProperty);
            set => SetValue(TimeLeftProperty, value);
        }

        private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is RingTimerControl control)
            {
                control.InvalidateSurface();
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var info = e.Info;
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            float strokeWidth = 14f;
            float radius = (Math.Min(info.Width, info.Height) - strokeWidth) / 2;
            var center = new SKPoint(info.Width / 2, info.Height / 2);
            var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

            // Background track
            using var bgPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(255, 255, 255, 40),
                StrokeWidth = strokeWidth,
                IsAntialias = true
            };
            canvas.DrawCircle(center, radius, bgPaint);

            // Arc progress
            double progress = Math.Max(0, Math.Min(1, TimeLeft / MaxTime));
            float sweepAngle = (float)(progress * 360);

            var ringColor = TimeLeft < 3 ? new SKColor(255, 107, 107) : // Red
                            TimeLeft < 5 ? new SKColor(255, 179, 71) :  // Orange
                            new SKColor(6, 214, 160);                   // Green

            using var glowPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ringColor.WithAlpha(120),
                StrokeWidth = strokeWidth + 8,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6)
            };

            using var progressPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ringColor,
                StrokeWidth = strokeWidth,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true
            };

            // Start at top (-90 degrees)
            using var path = new SKPath();
            path.AddArc(rect, -90, sweepAngle);
            canvas.DrawPath(path, glowPaint);
            canvas.DrawPath(path, progressPaint);

            // Centered text
            string text = Math.Ceiling(TimeLeft).ToString();
            using var font = new SKFont
            {
                Size = radius * 0.8f,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            using var textPaint = new SKPaint
            {
                Color = ringColor,
                IsAntialias = true
            };

            // Measure text for vertical centering
            var textBounds = new SKRect();
            font.MeasureText(text, out textBounds);
            float textY = center.Y - textBounds.MidY;

            canvas.DrawText(text, center.X, textY, SKTextAlign.Center, font, textPaint);
        }
    }
}
