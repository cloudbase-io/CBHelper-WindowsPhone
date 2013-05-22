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
using Cloudbase;

namespace CloudbaseTestApp
{
    public partial class FunctionsPage : PhoneApplicationPage
    {
        public FunctionsPage()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            App.helper.ExecuteCloudFunction(this.FunctionCodeBox.Text, new Dictionary<string, string>(), delegate(CBResponseInfo resp)
            {
                this.OutputTextBlock.Text = "CloudFunction output: " + resp.OutputString;
                return true;
            });
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (App.helper.CurrentLocation == null)
            {
                this.OutputTextBlock.Text = "We need to have your current location before executing this applet." +
                        "The test application automatically switches on location services. If you are using the emulator " +
                        " then you can set the current location using the additional tools";
            }
            else
            {
                Dictionary<string, string> appletParams = new Dictionary<string, string>();
                appletParams.Add("lat", Convert.ToString(App.helper.CurrentLocation.Location.Latitude));
                appletParams.Add("lng", Convert.ToString(App.helper.CurrentLocation.Location.Longitude));
                App.helper.ExecuteApplet("cb_get_address_from_coordinates", appletParams, delegate(CBResponseInfo resp)
                {
                    this.OutputTextBlock.Text = "Applet output: " + resp.OutputString;
                    return true;
                });
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> Params = new Dictionary<string,string>();
            Params.Add("shared_message", "Hello from the Windows Phone demo app");
            App.helper.ExecuteSharedApi("f_demo-application_demo-shared-api", "cb_demo,1!", Params, delegate(CBResponseInfo resp)
            {
                this.OutputTextBlock.Text = "Shared API output: " + resp.OutputString;
                return true;
            });
        }
    }
}