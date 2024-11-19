using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using TELEBLASTER_PRO.ViewModels;

namespace TELEBLASTER_PRO.Resources.Converters
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string contactId && parameter is InviteGroupChannelViewModel viewModel)
            {
                return viewModel.InviteStatuses.TryGetValue(contactId, out var status) ? status : "Pending";
            }
            return "Pending";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}