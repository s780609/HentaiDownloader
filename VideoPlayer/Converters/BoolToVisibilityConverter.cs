using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace VideoPlayer2.Converters;

/// <summary>
/// Boolean 轉 Visibility 轉換器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            // 如果有參數 "Inverse"，則反轉結果
            if (parameter is string param && param == "Inverse")
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        // 如果值不是 null，視為有值 (用於檢查縮圖是否存在)
        if (value != null)
        {
            if (parameter is string param && param == "Inverse")
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        if (parameter is string p && p == "Inverse")
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            bool result = visibility == Visibility.Visible;
            if (parameter is string param && param == "Inverse")
            {
                return !result;
            }
            return result;
        }
        return false;
    }
}
