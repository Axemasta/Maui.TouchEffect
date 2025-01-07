﻿using System.ComponentModel;
using System.Globalization;

namespace Maui.TouchEffect.Converters;

internal class TimeSpanMillisecondTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        => destinationType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        var strValue = value?.ToString();

        if (strValue != null && int.TryParse(strValue, out var intValue))
        {
            return TimeSpan.FromMilliseconds(intValue);
        }

        throw new InvalidOperationException(string.Format("Cannot convert \"{0}\" into {1}", strValue, typeof(TimeSpanMillisecondTypeConverter)));
    }


    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is not TimeSpan timespan)
        {
            throw new NotSupportedException();
        }

        return ((int)timespan.TotalMilliseconds).ToString();
    }
}