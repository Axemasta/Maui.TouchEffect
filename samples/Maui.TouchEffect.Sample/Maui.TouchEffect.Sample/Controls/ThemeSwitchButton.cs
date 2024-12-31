namespace Maui.TouchEffect.Sample.Controls;

public class ThemeSwitchButton : Button
{
    public ThemeSwitchButton() 
    {
        Padding = new Thickness(10);
        CornerRadius = 5;
        this.SetAppThemeColor(Button.TextColorProperty, Colors.White, Colors.Black);
        this.SetAppThemeColor(Button.BackgroundColorProperty, Colors.Black, Colors.White);
        this.SetAppThemeColor(Button.BorderColorProperty, Colors.DarkGray, Colors.LightGray);
        BorderWidth = 2;

        switch (Application.Current!.RequestedTheme)
        {
            case AppTheme.Dark:
                {
                    SetDarkAppearance();
                    break;
                }

            default:
            case AppTheme.Light:
                {
                    SetLightAppearance();
                    break;
                }
        }
        
        this.Clicked += OnClicked;
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        switch (Application.Current!.RequestedTheme)
        {
            case AppTheme.Dark:
                {
                    SetLightAppearance();
                    Application.Current.UserAppTheme = AppTheme.Light;
                    break;
                }

            case AppTheme.Light:
                {
                    SetDarkAppearance();
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    break;
                }
        }
    }

    private void SetDarkAppearance()
    {
        Text = "Change To Light";

    }

    private void SetLightAppearance()
    {
        Text = "Change To Dark";
    }
}