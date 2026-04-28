using Android.App;
using Android.Content;
using Android.Content.PM;
using AcadsJulie.Services;

namespace AcadsJulie;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = GoogleOAuthService.CallbackScheme,
    DataHost = GoogleOAuthService.CallbackHost)]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
