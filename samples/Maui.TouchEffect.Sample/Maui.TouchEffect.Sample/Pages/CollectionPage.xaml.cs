using Maui.TouchEffect.Sample.ViewModels;

namespace Maui.TouchEffect.Sample.Pages;

public partial class CollectionPage : ContentPage
{
    public CollectionPage(GameDevelopersViewModel viewmodel)
    {
        BindingContext = viewmodel;
        InitializeComponent();
    }
}