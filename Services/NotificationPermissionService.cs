using Plugin.LocalNotification;

namespace AcadsJulie.Services;

public static class NotificationPermissionService
{
    public static async Task<bool> EnsureNotificationsAllowedAsync(ProfileService profileService)
    {
        if (!profileService.GetProfile().NotificationsEnabled)
            return false;

        try
        {
            var permission = new NotificationPermission { AskPermission = true };
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled(permission))
                return true;

            return await LocalNotificationCenter.Current.RequestNotificationPermission(permission);
        }
        catch
        {
            return false;
        }
    }
}
