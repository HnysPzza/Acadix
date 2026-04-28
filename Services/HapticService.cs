namespace AcadsJulie.Services;

public class HapticService : IHapticService
{
    public void LightTap()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Keep gameplay flow even on unsupported platforms.
        }
    }
}
