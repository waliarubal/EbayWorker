using EbayWorker.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace EbayWorker.Converters
{
    public class SearchStatusToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uriFormat = "/Assets/{0}.png";
            switch((SearchStatus)value)
            {
                case SearchStatus.Working:
                    return string.Format(uriFormat, "Working16");

                case SearchStatus.Complete:
                    return string.Format(uriFormat, "Ok16");

                case SearchStatus.Failed:
                    return string.Format(uriFormat, "Error16");

                case SearchStatus.NotStarted:
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
