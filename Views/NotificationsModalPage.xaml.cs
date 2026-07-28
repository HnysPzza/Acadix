using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class NotificationsModalPage : ContentPage
{
    public NotificationsModalPage()
    {
        InitializeComponent();
        LoadNotifications();
    }

    private void LoadNotifications()
    {
        int lastSeenLevel = ScopedPreferences.Get("LastSeenLevel", 1);
        var profile = App.ProfileService.GetProfile();
        
        NotificationList.Children.Clear();

        bool hasNotifications = false;

        if (profile.Level > lastSeenLevel)
        {
            hasNotifications = true;
            
            // Build Notification Item
            var card = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
                Stroke = GetColor("MobileStroke", "#C7D7D2"),
                BackgroundColor = GetColor("MobileSurface", "#F7FBF8"),
                Padding = new Thickness(16),
                Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.08f, Radius = 12 }
            };

            var hStack = new HorizontalStackLayout { Spacing = 16 };
            
            var iconFrame = new Frame { BackgroundColor = GetColor("MobileCoralMuted", "#FFE4D9"), CornerRadius = 14, Padding = new Thickness(12), HasShadow = false, BorderColor = Colors.Transparent, VerticalOptions = LayoutOptions.Center };
            iconFrame.Content = new Label { Text = "🎉", FontSize = 24, HorizontalOptions = LayoutOptions.Center };
            
            var vStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
            vStack.Children.Add(new Label { Text = "Level Up!", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = GetColor("TextPrimary", "#121A1C") });
            vStack.Children.Add(new Label { Text = $"Congratulations! You've reached Level {profile.Level}.", FontSize = 14, TextColor = GetColor("TextSecondary", "#59686A") });
            
            hStack.Children.Add(iconFrame);
            hStack.Children.Add(vStack);
            
            card.Content = hStack;
            NotificationList.Children.Add(card);

            // Mark as read
            ScopedPreferences.Set("LastSeenLevel", profile.Level);
        }

        if (!hasNotifications)
        {
            EmptyStateLabel.IsVisible = true;
        }
    }

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private static Color GetColor(string key, string fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
    }
}
