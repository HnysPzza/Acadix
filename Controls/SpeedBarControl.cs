using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace AcadsJulie.Controls
{
    public class SpeedBarControl : SKCanvasView
    {
        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(nameof(Progress), typeof(double), typeof(SpeedBarControl), 1.0, propertyChanged: OnPropertyChanged);

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SpeedBarControl control)
            {
                control.InvalidateSurface();
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var info = e.Info;
            var canvas = e.Surface.Canvas;

            canvas.Clear();

            float totalWidth = info.Width;
            float height = info.Height;
            float cornerRadius = height / 2;
            
            // Draw background track
            var bgRect = new SKRect(0, 0, totalWidth, height);
            using var bgPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 50),
                IsAntialias = true
            };
            canvas.DrawRoundRect(bgRect, cornerRadius, cornerRadius, bgPaint);

            // Draw progress bar
            float fillWidth = (float)(Progress * totalWidth);
            if (fillWidth <= 0) return;

            var fillRect = new SKRect(0, 0, fillWidth, height);
            
            // Gradient from Green to Yellow to Red (Left to Right representing full time vs no time)
            var colors = new SKColor[] 
            { 
                new SKColor(255, 107, 107), // Red at left (time's up)
                new SKColor(255, 179, 71),  // Orange in middle
                new SKColor(6, 214, 160)    // Green at right (full time)
            };
            
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(totalWidth, 0), // Full width gradient
                colors,
                null,
                SKShaderTileMode.Clamp);

            using var progressPaint = new SKPaint
            {
                Shader = shader,
                IsAntialias = true
            };

            // Clip to avoid spilling out
            using var clipPath = new SKPath();
            clipPath.AddRoundRect(fillRect, cornerRadius, cornerRadius);
            
            // Save state, clip, draw, restore to get perfect clipping
            canvas.Save();
            canvas.ClipPath(clipPath, antialias: true);
            canvas.DrawRect(bgRect, progressPaint); // Draw rect across whole bounds, but clipped
            canvas.Restore();
        }
    }
}
