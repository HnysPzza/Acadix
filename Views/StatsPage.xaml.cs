using AcadsJulie.Models;
using AcadsJulie.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace AcadsJulie.Views;

public partial class StatsPage : ContentPage
{
    private readonly ProfileService _profileService;
    private readonly ProgressService _progressService;
    private List<int> _weeklyActivity = [];
    private List<int> _monthlyActivity = [];
    private int[] _categoryScores = [0, 0, 0, 0];
    private List<TriviaCategoryStat> _triviaStats = [];

    public StatsPage()
    {
        InitializeComponent();
        _profileService = App.ProfileService;
        _progressService = App.ProgressService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadStats();
        WeeklyBarChart.InvalidateSurface();
        HistoryLineChart.InvalidateSurface();
        RadarChart.InvalidateSurface();
        TriviaChart.InvalidateSurface();
    }

    private void LoadStats()
    {
        var profile = _profileService.GetProfile();
        _weeklyActivity = _progressService.GetWeeklyActivityCounts();
        _monthlyActivity = _progressService.GetMonthlyScoreProgression();
        _categoryScores = [profile.MemoryScore, profile.FocusScore, profile.LogicScore, profile.SpeedScore];
        _triviaStats = _progressService.GetTriviaCategoryStats();

        var brainScore = profile.BrainScore;
        BrainScoreLabel.Text = brainScore.ToString();
        BrainScoreProgress.Progress = Math.Min(1.0, brainScore / 1000.0);
        BrainScoreSubtitle.Text = brainScore switch
        {
            0 => "Start playing to build your brain score!",
            < 250 => "Good start! Keep training daily 💪",
            < 500 => "Nice progress! You're getting stronger 🧠",
            < 750 => "Impressive! Your brain is sharp 🌟",
            _ => "You're a Brain Champion! 🏆"
        };

        MemoryScoreLabel.Text = profile.MemoryScore.ToString();
        MemoryProgress.Progress = Math.Min(1.0, profile.MemoryScore / 1000.0);
        
        double memHitRate = _progressService.GetAverageCategoryAccuracy("Memory");
        MemoryHitRateLabel.Text = $"Hit Rate: {Math.Round(memHitRate, 1)}%";

        FocusScoreLabel.Text = profile.FocusScore.ToString();
        FocusProgress.Progress = Math.Min(1.0, profile.FocusScore / 1000.0);

        LogicScoreLabel.Text = profile.LogicScore.ToString();
        LogicProgress.Progress = Math.Min(1.0, profile.LogicScore / 1000.0);

        SpeedScoreLabel.Text = profile.SpeedScore.ToString();
        SpeedProgress.Progress = Math.Min(1.0, profile.SpeedScore / 1000.0);
    }

    private void OnDrawWeeklyChart(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_weeklyActivity.Count == 0) return;

        var info = e.Info;
        var width = info.Width;
        var height = info.Height;
        var padding = new SKRect(40, 20, 20, 30);
        var chartWidth = width - padding.Left - padding.Right;
        var chartHeight = height - padding.Top - padding.Bottom;

        var maxVal = Math.Max(1, _weeklyActivity.Max());
        var barCount = _weeklyActivity.Count;
        var barWidth = chartWidth / barCount * 0.6f;
        var spacing = chartWidth / barCount;

        var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        // Map today's day to last index
        var todayIndex = ((int)DateTime.Today.DayOfWeek + 6) % 7; // Mon=0..Sun=6

        using var barPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var font = new SKFont
        {
            Size = 28
        };
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColor.Parse("#7E8D90")
        };
        using var gridPaint = new SKPaint { Color = SKColor.Parse("#D9E5E0"), StrokeWidth = 1 };

        // Draw grid lines
        for (int i = 0; i <= 4; i++)
        {
            float y = padding.Top + chartHeight - (chartHeight * i / 4f);
            canvas.DrawLine(padding.Left, y, width - padding.Right, y, gridPaint);
        }

        for (int i = 0; i < barCount; i++)
        {
            float x = padding.Left + spacing * i + spacing / 2f;
            float barHeight = chartHeight * (_weeklyActivity[i] / (float)maxVal);
            float barX = x - barWidth / 2;
            float barY = padding.Top + chartHeight - barHeight;

            // Color: today's bar uses the mobile primary token, older bars use the muted surface.
            var dayIdx = (todayIndex - (barCount - 1 - i) + 7) % 7;
            barPaint.Color = i == barCount - 1 ? SKColor.Parse("#0F766E") : SKColor.Parse("#D8F3EC");

            var rect = new SKRoundRect(new SKRect(barX, barY, barX + barWidth, padding.Top + chartHeight), 6, 6);
            canvas.DrawRoundRect(rect, barPaint);

            // Day labels
            canvas.DrawText(days[dayIdx], x, height - 4, SKTextAlign.Center, font, textPaint);
        }
    }

    private void OnDrawHistoryChart(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_monthlyActivity == null || _monthlyActivity.Count < 2) return;

        var info = e.Info;
        var width = info.Width;
        var height = info.Height;
        var padding = new SKRect(20, 20, 20, 40); // extra bottom padding for labels
        
        var chartWidth = width - padding.Left - padding.Right;
        var chartHeight = height - padding.Top - padding.Bottom;

        var maxVal = Math.Max(1, _monthlyActivity.Max());
        var stepX = chartWidth / (_monthlyActivity.Count - 1);

        using var gridPaint = new SKPaint { Color = SKColor.Parse("#D9E5E0"), StrokeWidth = 1 };
        using var linePaint = new SKPaint { Color = SKColor.Parse("#16A34A"), StrokeWidth = 4, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Round };
        using var fillPaint = new SKPaint { Color = SKColor.Parse("#16A34A").WithAlpha(50), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var dotPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
        using var dotStrokePaint = new SKPaint { Color = SKColor.Parse("#16A34A"), StrokeWidth = 2, IsAntialias = true, Style = SKPaintStyle.Stroke };

        // Draw horizontal grid lines
        for (int i = 0; i <= 3; i++)
        {
            float y = padding.Top + chartHeight - (chartHeight * i / 3f);
            canvas.DrawLine(padding.Left, y, width - padding.Right, y, gridPaint);
        }

        var path = new SKPath();
        for (int i = 0; i < _monthlyActivity.Count; i++)
        {
            float x = padding.Left + (i * stepX);
            float val = _monthlyActivity[i] / (float)maxVal;
            float y = padding.Top + chartHeight - (val * chartHeight);

            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        // Fill area under line
        var fillPath = new SKPath(path);
        fillPath.LineTo(padding.Left + chartWidth, padding.Top + chartHeight);
        fillPath.LineTo(padding.Left, padding.Top + chartHeight);
        fillPath.Close();
        canvas.DrawPath(fillPath, fillPaint);

        // Draw line
        canvas.DrawPath(path, linePaint);

        // Draw dots
        for (int i = 0; i < _monthlyActivity.Count; i++)
        {
            float x = padding.Left + (i * stepX);
            float val = _monthlyActivity[i] / (float)maxVal;
            float y = padding.Top + chartHeight - (val * chartHeight);
            canvas.DrawCircle(x, y, 5, dotPaint);
            canvas.DrawCircle(x, y, 5, dotStrokePaint);
        }
        
        // Month Labels (Start, Middle, End)
        using var font = new SKFont { Size = 24 };
        using var textPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#7E8D90") };
        canvas.DrawText("30d ago", padding.Left, height - 10, SKTextAlign.Left, font, textPaint);
        canvas.DrawText("15d ago", padding.Left + (chartWidth / 2f), height - 10, SKTextAlign.Center, font, textPaint);
        canvas.DrawText("Today", padding.Left + chartWidth, height - 10, SKTextAlign.Right, font, textPaint);
    }

    private void OnDrawRadarChart(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var info = e.Info;
        float cx = info.Width / 2f;
        float cy = info.Height / 2f;
        float radius = Math.Min(cx, cy) - 40;

        var labels = new[] { "Memory", "Focus", "Logic", "Speed" };
        var values = _categoryScores.Select(s => Math.Min(1f, s / 1000f)).ToArray();
        var colors = new[] { "#4856D6", "#16A34A", "#F9735B", "#F6A623" };
        int n = labels.Length;
        double angleStep = 2 * Math.PI / n;
        double startAngle = -Math.PI / 2;

        using var gridPaint = new SKPaint { Color = SKColor.Parse("#C7D7D2"), StrokeWidth = 1.5f, IsAntialias = true, Style = SKPaintStyle.Stroke };
        using var fillPaint = new SKPaint { Color = SKColor.Parse("#0F766E").WithAlpha(60), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var strokePaint = new SKPaint { Color = SKColor.Parse("#0F766E"), StrokeWidth = 2f, IsAntialias = true, Style = SKPaintStyle.Stroke };
        using var font = new SKFont { Size = 32 };
        using var labelPaint = new SKPaint { Color = SKColor.Parse("#121A1C"), IsAntialias = true };
        using var dotPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

        // Draw concentric grid polygons
        for (int ring = 1; ring <= 4; ring++)
        {
            float r = radius * ring / 4f;
            var gridPath = new SKPath();
            for (int i = 0; i < n; i++)
            {
                double angle = startAngle + i * angleStep;
                float px = cx + (float)(r * Math.Cos(angle));
                float py = cy + (float)(r * Math.Sin(angle));
                if (i == 0) gridPath.MoveTo(px, py);
                else gridPath.LineTo(px, py);
            }
            gridPath.Close();
            canvas.DrawPath(gridPath, gridPaint);
        }

        // Draw spokes
        for (int i = 0; i < n; i++)
        {
            double angle = startAngle + i * angleStep;
            float px = cx + (float)(radius * Math.Cos(angle));
            float py = cy + (float)(radius * Math.Sin(angle));
            canvas.DrawLine(cx, cy, px, py, gridPaint);
        }

        // Draw data polygon
        var dataPath = new SKPath();
        for (int i = 0; i < n; i++)
        {
            double angle = startAngle + i * angleStep;
            float r = radius * values[i];
            float px = cx + (float)(r * Math.Cos(angle));
            float py = cy + (float)(r * Math.Sin(angle));
            if (i == 0) dataPath.MoveTo(px, py);
            else dataPath.LineTo(px, py);
        }
        dataPath.Close();
        canvas.DrawPath(dataPath, fillPaint);
        canvas.DrawPath(dataPath, strokePaint);

        // Draw dots and labels
        for (int i = 0; i < n; i++)
        {
            double angle = startAngle + i * angleStep;
            float r = radius * values[i];
            float px = cx + (float)(r * Math.Cos(angle));
            float py = cy + (float)(r * Math.Sin(angle));
            dotPaint.Color = SKColor.Parse(colors[i]);
            canvas.DrawCircle(px, py, 6, dotPaint);

            // Labels at edge
            float lx = cx + (float)((radius + 30) * Math.Cos(angle));
            float ly = cy + (float)((radius + 30) * Math.Sin(angle)) + 10;
            canvas.DrawText(labels[i], lx, ly, SKTextAlign.Center, font, labelPaint);
        }
    }

    private void OnDrawTriviaChart(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_triviaStats.Count == 0)
        {
            using var emptyFont = new SKFont { Size = 28 };
            using var emptyTextPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#7E8D90") };
            canvas.DrawText("Play Trivia to see your mastery here!", e.Info.Width / 2f, e.Info.Height / 2f - 14, SKTextAlign.Center, emptyFont, emptyTextPaint);
            return;
        }

        var info = e.Info;
        var width = info.Width;
        var height = info.Height;
        var leftLabelWidth = 100f;
        var barStart = leftLabelWidth + 8;
        var chartWidth = width - barStart - 40;
        var barHeight = 20f;
        var spacing = barHeight + 12;

        var maxScore = Math.Max(1, _triviaStats.Max(s => s.BestScore));

        using var barPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var font = new SKFont { Size = 22 };
        using var textPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#59686A") };

        for (int i = 0; i < _triviaStats.Count; i++)
        {
            var stat = _triviaStats[i];
            var label = stat.SubCategory.Replace("_", " ");
            if (label.Length > 14) label = label[..11] + "...";

            float y = 16 + i * spacing;

            canvas.DrawText(label, 4, y + barHeight / 2f + 6, SKTextAlign.Left, font, textPaint);

            float barLen = chartWidth * (stat.BestScore / (float)maxScore);
            if (barLen < 8) barLen = 8;

            barPaint.Color = SKColor.Parse("#134E4A");
            var rect = new SKRoundRect(new SKRect(barStart, y, barStart + barLen, y + barHeight), 4, 4);
            canvas.DrawRoundRect(rect, barPaint);

            canvas.DrawText(stat.BestScore.ToString(), barStart + barLen + 8, y + barHeight / 2f + 6, SKTextAlign.Left, font, textPaint);
        }
    }
}
