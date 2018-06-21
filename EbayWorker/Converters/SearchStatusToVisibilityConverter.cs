using EbayWorker.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EbayWorker.Converters
{
    public class SearchStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (SearchStatus)value; ;
            if (status == SearchStatus.Complete || status == SearchStatus.Failed || status == SearchStatus.Cancelled)
                return Visibility.Visible;

            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
