using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using TELEBLASTER_PRO.ViewModels;

namespace TELEBLASTER_PRO.Converters
{
    public class StatusMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string contactId && values[1] is InviteGroupChannelViewModel viewModel)
            {
                return viewModel.InviteStatuses.TryGetValue(contactId, out var status) ? status : "Pending";
            }
            return "Pending";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 