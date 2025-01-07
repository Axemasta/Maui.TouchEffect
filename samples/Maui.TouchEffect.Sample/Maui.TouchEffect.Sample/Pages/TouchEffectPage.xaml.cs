using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Maui.TouchEffect.Sample.ObjectModel;
using Maui.TouchEffect.Sample.ViewModels;

using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace Maui.TouchEffect.Sample.Pages;

public partial class TouchEffectPage : ContentPage
{
    public TouchEffectPage(TouchEffectViewModel viewmodel)
    {
        BindingContext = viewmodel;

        On<iOS>().SetPrefersHomeIndicatorAutoHidden(true);

        InitializeComponent();
    }
}