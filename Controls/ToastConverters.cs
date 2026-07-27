using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

#nullable enable
namespace DbdModManager.Controls
{
    public class ToastKindToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value is ToastKind kind ? kind switch
            {
                ToastKind.Success => "Toast.Success",
                ToastKind.Error => "Toast.Error",
                ToastKind.Warning => "Toast.Warning",
                _ => "Toast.Info"
            } : "Toast.Info";

            return System.Windows.Application.Current.Resources[key] as Brush ?? Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class ToastKindToIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value is ToastKind kind ? kind switch
            {
                ToastKind.Success => "Icon.CheckCircle",
                ToastKind.Error => "Icon.XCircle",
                ToastKind.Warning => "Icon.AlertTriangle",
                _ => "Icon.InfoCircle"
            } : "Icon.InfoCircle";

            return System.Windows.Application.Current.Resources[key] as Geometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
