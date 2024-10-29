﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TELEBLASTER_PRO.ViewModels;

namespace TELEBLASTER_PRO.Views.UserControls
{
    /// <summary>
    /// Interaction logic for GroupChannel.xaml
    /// </summary>
    public partial class GroupChannel : UserControl
    {
        public GroupChannel()
        {
            InitializeComponent();
            DataContext = new GroupChannelViewModel(new AccountViewModel());
        }
    }
}