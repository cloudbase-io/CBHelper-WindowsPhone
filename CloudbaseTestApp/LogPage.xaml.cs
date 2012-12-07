/* Copyright (C) 2012 cloudbase.io
 
 This program is free software; you can redistribute it and/or modify it under
 the terms of the GNU General Public License, version 2, as published by
 the Free Software Foundation.
 
 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 for more details.
 
 You should have received a copy of the GNU General Public License
 along with this program; see the file COPYING.  If not, write to the Free
 Software Foundation, 59 Temple Place - Suite 330, Boston, MA
 02111-1307, USA.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace CloudbaseTestApp
{
    public partial class LogPage : PhoneApplicationPage
    {
        public LogPage()
        {
            InitializeComponent();
        }

       
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (App.helper != null)
            {
                App.helper.LogDebug(this.LogTextBox.Text, this.CategoryTextBox.Text);
            }
        }

        private void ContentPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.helper != null)
            {
                App.helper.LogNavigation("LogPage");
            }
        }

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/LogPage.xaml", UriKind.Relative));
        }

        private void ApplicationBarIconButton_Click_2(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/FunctionsPage.xaml", UriKind.Relative));
        }

        private void ApplicationBarIconButton_Click_3(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/DataPage.xaml", UriKind.Relative));
        }

        private void ApplicationBarIconButton_Click_4(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/NotificationsPage.xaml", UriKind.Relative));
        }

        private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }
    }
}