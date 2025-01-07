using Maui.TouchEffect.Sample.ViewModels;

namespace Maui.TouchEffect.Sample.Pages;

public partial class ImagePage : ContentPage
{
	public ImagePage(TouchEffectViewModel viewmodel)
    {
        BindingContext = viewmodel;
        
		InitializeComponent();
	}
}