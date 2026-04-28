using System.IO;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class EditProfilePage : ContentPage
{
    private readonly ProfileService _profileService;
    private string? _currentTempImagePath;

    public EditProfilePage()
    {
        InitializeComponent();
        _profileService = App.ProfileService;
        LoadInitialData();
    }

    private void LoadInitialData()
    {
        var profile = _profileService.GetProfile();
        NameEntry.Text = profile.Name;
        AvatarLabel.Text = profile.AvatarInitials;

        if (!string.IsNullOrEmpty(profile.ProfileImagePath) && File.Exists(profile.ProfileImagePath))
        {
            AvatarImage.Source = ImageSource.FromFile(profile.ProfileImagePath);
            AvatarImage.IsVisible = true;
            AvatarLabel.IsVisible = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var profile = _profileService.GetProfile();
        
        // Save Name
        string newName = NameEntry.Text?.Trim() ?? "User";
        if (!string.IsNullOrEmpty(newName))
        {
            profile.Name = newName;
        }

        // Save new photo if one was chosen
        if (!string.IsNullOrEmpty(_currentTempImagePath))
        {
            profile.ProfileImagePath = _currentTempImagePath;
        }

        _profileService.SaveProfile(profile);
        await Shell.Current.GoToAsync("..");
    }

    private async void OnChangePhotoClicked(object? sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Profile Picture", "Cancel", null, "Take Photo", "Choose from Gallery", "Remove Photo");

        try
        {
            if (action == "Take Photo")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    FileResult? photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null) await UpdateAvatarPreview(photo);
                }
            }
            else if (action == "Choose from Gallery")
            {
                FileResult? photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null) await UpdateAvatarPreview(photo);
            }
            else if (action == "Remove Photo")
            {
                var profile = _profileService.GetProfile();
                profile.ProfileImagePath = null; // Mark for removal but don't save yet until they hit "Save"
                _profileService.SaveProfile(profile); // We have to save it immediately because 'Remove' has no intermediate state
                
                AvatarImage.IsVisible = false;
                AvatarLabel.IsVisible = true;
                _currentTempImagePath = null;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not update photo: {ex.Message}", "OK");
        }
    }

    private async Task UpdateAvatarPreview(FileResult photo)
    {
        string localFilePath = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);

        using Stream sourceStream = await photo.OpenReadAsync();
        using FileStream localFileStream = File.OpenWrite(localFilePath);
        await sourceStream.CopyToAsync(localFileStream);

        _currentTempImagePath = localFilePath;

        // Update preview
        AvatarImage.Source = ImageSource.FromFile(localFilePath);
        AvatarImage.IsVisible = true;
        AvatarLabel.IsVisible = false;
    }
}
