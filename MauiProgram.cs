using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using AcadsJulie.Views;
using AcadsJulie.Views.Games;
using AcadsJulie.ViewModels;
using Plugin.LocalNotification;
using CommunityToolkit.Maui;

namespace AcadsJulie
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseLocalNotification()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Pages
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<CardMatchPage>();
            builder.Services.AddTransient<OddOneOutPage>();
            builder.Services.AddTransient<SequenceGamePage>();
            builder.Services.AddTransient<QuickMathPage>();
            builder.Services.AddTransient<AchievementsPage>();

            // ViewModels
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<CardMatchViewModel>();
            builder.Services.AddTransient<OddOneOutViewModel>();
            builder.Services.AddTransient<SequenceGameViewModel>();
            builder.Services.AddTransient<QuickMathViewModel>();
            builder.Services.AddTransient<AchievementsViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
