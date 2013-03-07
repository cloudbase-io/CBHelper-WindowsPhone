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
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Media;
using Cloudbase;

namespace CloudbaseTestApp
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains("app_code"))
            {
                string appCode = Convert.ToString(settings["app_code"]);
                string appUniq = Convert.ToString(settings["app_uniq"]);
                
                this.AppCodeBox.Text = appCode;
                this.AppUniqBox.Text = appUniq;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Saving Settings...");
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains("app_code"))
            {
                settings.Remove("app_code");
                settings.Add("app_code", this.AppCodeBox.Text);
            }
            else
                settings.Add("app_code", this.AppCodeBox.Text);
            if (settings.Contains("app_uniq"))
            {
                settings.Remove("app_uniq");
                settings.Add("app_uniq", this.AppUniqBox.Text);
            }
            else
                settings.Add("app_uniq", this.AppUniqBox.Text);
            if (settings.Contains("app_pwd"))
            {
                settings.Remove("app_pwd");
                settings.Add("app_pwd", MD5Core.GetHashString(this.AppPwd.Text));
            }
            else
                settings.Add("app_pwd", MD5Core.GetHashString(this.AppPwd.Text));
            
            settings.Save();

            App.helper = new CBHelper(this.AppCodeBox.Text, this.AppUniqBox.Text);
            App.helper.SetPassword(MD5Core.GetHashString(this.AppPwd.Text));
            
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

        private void PhoneApplicationPage_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (App.helper != null)
            {
                //NavigationService.Navigate(new Uri("/LogPage.xaml", UriKind.Relative));
                App.helper.LogNavigation("SettingsPage");
            }
        }

        // this method downloads a static image from flickr and saves it into the gallery. This image is then
        // used to test the data APIs when inserting objects with files.
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WebClient wc = new WebClient();  
            wc.OpenReadCompleted += delegate(object webSender, OpenReadCompletedEventArgs webE)  
            {
                System.Diagnostics.Debug.WriteLine("downloaded...");
                if (webE.Error != null)  
                {
                    System.Diagnostics.Debug.WriteLine("Failed image download " + webE.Error.Message + "\n" + webE.Error.StackTrace);
                }

                using (Stream stream = webE.Result)  
                {  
                    MediaLibrary mediaLibrary = new MediaLibrary();  
                    mediaLibrary.SavePicture("downloaded.jpg", stream); // Saved Pictures album  
                }  
            };  
            
            wc.OpenReadAsync(new Uri("http://farm8.staticflickr.com/7276/7856122900_0de82be225_m.jpg"));  
        }

        private void PayPalButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.helper != null)
            {
                CBPayPalBillItem item = new CBPayPalBillItem();
                item.Name = "Test item 1";
                item.Description = "Test item 1 for $9.99";
                item.Amount = 9.99;
                item.Tax = 0;
                item.Quantity = 1;

                CBPayPalBill bill = new CBPayPalBill();
                bill.Name = "Test PayPal bill 1";
                bill.Description = "Test PayPal bill 1 for $9.99";
                bill.Currency = "USD";
                bill.InvoiceNumber = "TEST_INV_1";
                bill.AddNewItem(item);

                App.helper.PreparePayPalPurchase(bill, false, delegate(CBResponseInfo resp)
                {
                    if (resp.Status)
                    {
                        if (((Dictionary<string, object>)resp.Data).ContainsKey("checkout_url"))
                        {
                            App.PayPalCheckoutUrl = Convert.ToString(((Dictionary<string, object>)resp.Data)["checkout_url"]);
                            NavigationService.Navigate(new Uri("/PayPalBrowser.xaml", UriKind.Relative));
                        }
                    }
                    return true;
                });
            }
        }
    }
}