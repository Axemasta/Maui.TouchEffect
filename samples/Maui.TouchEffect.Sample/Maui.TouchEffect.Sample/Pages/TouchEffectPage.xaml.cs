using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Maui.TouchEffect.Sample.ObjectModel;

using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace Maui.TouchEffect.Sample.Pages;

public partial class TouchEffectPage : ContentPage
{
    public TouchEffectPage()
    {
        On<iOS>().SetPrefersHomeIndicatorAutoHidden(true);

        Command = CommandFactory.Create(() =>
        {
            TouchCount++;
            OnPropertyChanged(nameof(TouchCount));
        });

        LongPressCommand = CommandFactory.Create(() =>
        {
            LongPressCount++;
            OnPropertyChanged(nameof(LongPressCount));
        });

        ParentCommand = CommandFactory.Create(() => DisplayAlert("Parent clicked", null, "Ok"));

        ChildCommand = CommandFactory.Create(() => DisplayAlert("Child clicked", null, "Ok"));

        
        InitializeComponent();
    }
    
    public ICommand Command { get; }

    public ICommand LongPressCommand { get; }

    public ICommand ParentCommand { get; }

    public ICommand ChildCommand { get; }

    public int TouchCount { get; private set; }

    public int LongPressCount { get; private set; }

    bool nativeAnimationBorderless;

    public bool NativeAnimationBorderless
    {
        get => nativeAnimationBorderless;
        set
        {
            nativeAnimationBorderless = value;
            OnPropertyChanged();
        }
    }
}