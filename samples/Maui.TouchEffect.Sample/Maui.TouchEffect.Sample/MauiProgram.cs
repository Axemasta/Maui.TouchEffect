using Maui.TouchEffect.Hosting;
using Maui.TouchEffect.Sample.Pages;
using Maui.TouchEffect.Sample.ViewModels;

using Microsoft.Extensions.Logging;

namespace Maui.TouchEffect.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiTouchEffect()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddTransient<TouchEffectPage>();
        builder.Services.AddTransient<CollectionPage>();
        builder.Services.AddTransient<ImagePage>();

        builder.Services.AddTransient<GameDevelopersViewModel>();
        builder.Services.AddTransient<TouchEffectViewModel>();

        return builder.Build();
    }
}