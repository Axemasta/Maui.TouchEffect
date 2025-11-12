using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Maui.TouchEffect.Sample.ViewModels;

public partial class TouchEffectViewModel : ObservableObject
{
    [ObservableProperty]
    private int touchCount;

    [ObservableProperty]
    private int longPressCount;

    [ObservableProperty]
    private bool nativeAnimationBorderless;

    [RelayCommand]
    private void Touch()
    {
        TouchCount++;
    }

    [RelayCommand]
    private void LongPress()
    {
        LongPressCount++;
    }

    [RelayCommand]
    private async Task ChildPressed()
    {
        await Shell.Current.DisplayAlertAsync("Child clicked", null, "Ok");
    }

    [RelayCommand]
    private async Task ParentPressed()
    {
        await Shell.Current.DisplayAlertAsync("Parent clicked", null, "Ok");
    }
}