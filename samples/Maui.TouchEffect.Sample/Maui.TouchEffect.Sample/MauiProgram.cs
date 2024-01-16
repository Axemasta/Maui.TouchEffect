using Maui.TouchEffect.Hosting;
using Maui.TouchEffect.Sample.Pages;
using Maui.TouchEffect.Sample.ViewModels;

using Microsoft.Extensions.Logging;

using Sharpnado.CollectionView;

namespace Maui.TouchEffect.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiTouchEffect()
            .UseSharpnadoCollectionView(true)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddTransient<TouchEffectPage>();
        builder.Services.AddTransient<SharpnadoPage>();
        builder.Services.AddTransient<ItemsViewModel>();

        return builder.Build();
    }
}