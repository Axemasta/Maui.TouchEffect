using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
namespace Maui.TouchEffect.Hosting;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiTouchEffect(this MauiAppBuilder builder)
	{
		builder.ConfigureEffects(effects =>
		{
            // Implemented only on iOS & Android
            // Partial touch effect really did not want to work.... a partial behavior did work
#if IOS
			effects.Add<TouchEffect, PlatformTouchEffect>();
#endif
#if ANDROID
			effects.Add<TouchEffect, PlatformTouchEffect>();
#endif
#if WINDOWS
            effects.Add<TouchEffect, PlatformTouchEffect>();
#endif
        });

		return builder;
	}
}
