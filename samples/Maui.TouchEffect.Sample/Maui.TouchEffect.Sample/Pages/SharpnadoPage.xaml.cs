using Maui.TouchEffect.Sample.ViewModels;

namespace Maui.TouchEffect.Sample.Pages;

public partial class SharpnadoPage : ContentPage
{
    public SharpnadoPage(ItemsViewModel itemsViewModel)
    {
        BindingContext = itemsViewModel;
        InitializeComponent();
    }
}