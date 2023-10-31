using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
namespace Maui.TouchEffect.Hosting;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiTouchEffect(this MauiAppBuilder builder)
	{
		builder.ConfigureEffects(effects =>
		{
#if IOS
			effects.Add<TouchEffect, PlatformTouchEffect>();
#endif
#if ANDROID
			effects.Add<TouchEffect, PlatformTouchEffect>();
#endif
#if MACOS
			effects.Add<TouchEffect, PlatformTouchEffect>();
#endif
		});

		return builder;
	}
}
