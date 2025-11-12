namespace Maui.TouchEffect.Extensions;

internal static class ColorConversionExtensions
{
    /// <summary>
    /// Applies the supplied <paramref name="redComponent"/> to this <see cref="Color"/>.
    /// </summary>
    /// <param name="color">The <see cref="Color"/> to modify.</param>
    /// <param name="redComponent">The red component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
    /// <returns>A <see cref="Color"/> with the supplied <paramref name="redComponent"/> applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="redComponent"/> is <b>not</b> between 0 and 1.</exception>
    internal static Color WithRed(this Color color, double redComponent)
    {
        ArgumentNullException.ThrowIfNull(color);
        return redComponent is < 0 or > 1
                ? throw new ArgumentOutOfRangeException(nameof(redComponent))
                : Color.FromRgba(redComponent, color.Green, color.Blue, color.Alpha);
    }

    /// <summary>
    /// Applies the supplied <paramref name="greenComponent"/> to this <see cref="Color"/>.
    /// </summary>
    /// <param name="color">The <see cref="Color"/> to modify.</param>
    /// <param name="greenComponent">The green component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
    /// <returns>A <see cref="Color"/> with the supplied <paramref name="greenComponent"/> applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="greenComponent"/> is <b>not</b> between 0 and 1.</exception>
    internal static Color WithGreen(this Color color, double greenComponent)
    {
        ArgumentNullException.ThrowIfNull(color);
        return greenComponent is < 0 or > 1
                ? throw new ArgumentOutOfRangeException(nameof(greenComponent))
                : Color.FromRgba(color.Red, greenComponent, color.Blue, color.Alpha);
    }

    /// <summary>
    /// Applies the supplied <paramref name="blueComponent"/> to this <see cref="Color"/>.
    /// </summary>
    /// <param name="color">The <see cref="Color"/> to modify.</param>
    /// <param name="blueComponent">The blue component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
    /// <returns>A <see cref="Color"/> with the supplied <paramref name="blueComponent"/> applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blueComponent"/> is <b>not</b> between 0 and 1.</exception>
    internal static Color WithBlue(this Color color, double blueComponent)
    {
        ArgumentNullException.ThrowIfNull(color);

        return blueComponent is < 0 or > 1
                ? throw new ArgumentOutOfRangeException(nameof(blueComponent))
                : Color.FromRgba(color.Red, color.Green, blueComponent, color.Alpha);
    }
}