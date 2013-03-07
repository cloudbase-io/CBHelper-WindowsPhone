using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using cloudbase;

namespace CloudbaseTestApp
{
    public partial class PayPalBrowser : PhoneApplicationPage
    {
        public PayPalBrowser()
        {
            InitializeComponent();
            
            this.PPBrowser.IsScriptEnabled = true;
            this.PPBrowser.Navigate(new Uri(App.PayPalCheckoutUrl));
            this.PPBrowser.Navigating += delegate(object sender, NavigatingEventArgs e)
            {
                //System.Diagnostics.Debug.WriteLine("started navigating to " + e.Uri.AbsoluteUri);
                if (App.helper.IsPayPayPaymentComplete(e.Uri, delegate(CBResponseInfo resp)
                {
                    // you can read the payment details
                    MessageBox.Show("Payment completed");
                    return true;
                }))
                {
                    NavigationService.GoBack();
                }
            };
        }
    }
}