> This repository has been unarchived and updated to target .NET 9 due to a number of breaking changed over in maui which render `TouchBehavior` not fit for use. The aim is to backport the MCT verion to this library, and then reintroduce it to the MCT. Hopefully when that is done this repository can be archives for good!


# Maui.TouchEffect

[![NuGet Shield](https://img.shields.io/nuget/v/Axemasta.Maui.TouchEffect)](https://www.nuget.org/packages/Axemasta.Maui.TouchEffect/)

Maui port of TouchEffect from Xamarin Community Toolkit

> The originalaim of this library is to provide temporary support for the touch effect without having to take a dependency on [XCT's MauiCompat](https://devblogs.microsoft.com/xamarin/introducing-net-maui-compatibility-for-the-xamarin-community-toolkit/) library. My results of using the compat library have been extremely tempramental, alot of the times the touch effect does not work and due to the packages target framework (net6) & age (2 years old) I figured a new port would be the best option. Currently there is `TouchBehavior` available as part of the maui community toolkit however it is not stable due to a number of maui platform bugs regarding behaviors, and the maui team are unwilling to address these issues.

This library supports the following platforms:

| Platform     | Supported |
| ------------ | --------- |
| iOS          | ✅         |
| Android      | ✅         |
| Mac Catalyst | ❌         |
| Windows      | ✅         |
| Tizen        | ❌         |

> Mac Catalyst will be supported in the near future, I just need to enable the iOS code to compile for mac and we're golden

## Install

- Install Maui.TouchEffect package

- In your `MauiProgram.cs`, call `UseMauiTouchEffect`:
  ```diff
  var builder = MauiApp.CreateBuilder();
          builder
              .UseMauiApp<App>()
  ++          .UseMauiTouchEffect()
              .ConfigureFonts(fonts =>
              {
                  fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                  fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
              });
  ```

  

## Usage

See the samples app from this project, it is a port of the `TouchEffectPage` from the XCT samples app.

<img src="assets/Sample_Demonstration.gif" alt="Sample app included with this project" width="500">

Declare xmlns:

```xml
xmlns:touch="http://axemasta.com/schemas/2023/toucheffect"
```

The api has been updated to match the naming conventions seen in MCT, this will help porting between the versions easier

```xml
<StackLayout
    touch:TouchEffect.AnimationDuration="250"
    touch:TouchEffect.AnimationEasing="{x:Static Easing.CubicInOut}"
    touch:TouchEffect.Command="{Binding Command, Source={x:Reference Page}}"
    touch:TouchEffect.PressedOpacity="0.6"
    touch:TouchEffect.PressedScale="0.8"
    HorizontalOptions="CenterAndExpand"
    Orientation="Horizontal">
    <BoxView
        HeightRequest="20"
        WidthRequest="20"
        Color="Gold" />
    <Label Text="The entire layout receives touches" />
    <BoxView
        HeightRequest="20"
        WidthRequest="20"
        Color="Gold" />
</StackLayout>
```

## Native Animation

I have got this somewhat working again.

### iOS

This provides extra opacity & touch feedback, its hard to see the value in this feature currently

### Android

This provides ripples on touches. There are some wierd interactions when views have nested `TouchEffect`s so be careful when applying them to complicated views.

## Acknowlegements

This code is ported from:

- [Xamarin Community Toolkit](https://github.com/xamarin/XamarinCommunityToolkit)
- [Aminparsa18 Maui.TouchEffect](https://github.com/aminparsa18/Maui.TouchEffect/tree/master)
