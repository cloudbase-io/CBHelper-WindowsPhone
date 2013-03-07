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
using Microsoft.Xna.Framework.Media;
using System.IO;
using Microsoft.Phone;
using System.Windows.Media.Imaging;
using Cloudbase.DataCommands;
using Cloudbase;

namespace CloudbaseTestApp
{
    public class TestObject
    {
        private string _firstName;
        public string FirstName
        {
            get { return _firstName; }
            set { _firstName = value; }
        }
        private string _lastName;
        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value; }
        }
        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }
    }
    public partial class DataPage : PhoneApplicationPage
    {
        public DataPage()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // loop over the images in the media library looking for downloaded.jpg. This image comes from the 
            // download method in the SettingsScreen
            MediaLibrary lib = new MediaLibrary();
            Picture attachmentPic = null;
            foreach (Picture curPic in lib.Pictures)
            {
                if (curPic.Name.Equals("downloaded.jpg"))
                {
                    attachmentPic = curPic;
                    break;
                }
            }

            if (attachmentPic == null)
            {
                MessageBox.Show("Please use the settings page to save a test picture before running this API");
                return; 
            }
            // create a new attachment with the resized picture file
            CBHelperAttachment attachment = new CBHelperAttachment();
            attachment.FileName = attachmentPic.Name;
            WriteableBitmap pic = PictureDecoder.DecodeJpeg(attachmentPic.GetImage());
            Stream picStream = new MemoryStream();
            pic.SaveJpeg(picStream, 400, 400, 0, 100);
            picStream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[picStream.Length];   
            picStream.Read(buffer, 0, (int)picStream.Length);
            attachment.FileData = (byte[])buffer;
            picStream.Close();
   
            List<CBHelperAttachment> attList = new List<CBHelperAttachment>();
            attList.Add(attachment);
            TestObject newObj = new TestObject();
            newObj.FirstName = "Cloud";
            newObj.LastName = "Base";
            newObj.Title = ".io";

            App.helper.InsertDocument("users", newObj, attList, delegate(CBResponseInfo resp)
            {
                this.OutputBox.Text = "OUTPUT: " + resp.OutputString;
                return true;
            });
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CBHelperSearchCondition cond = new CBHelperSearchCondition("FirstName", CBConditionOperator.CBOperatorEqual, "Cloud");
            /*
            List<CBDataAggregationCommand> commands = new List<CBDataAggregationCommand>();

            CBDataAggregationCommandProject project = new CBDataAggregationCommandProject();
            project.IncludeFields.Add("Symbol");
            project.IncludeFields.Add("Price");
            project.IncludeFields.Add("AveragePrice");
            commands.Add(project);

            CBDataAggregationCommandGroup group = new CBDataAggregationCommandGroup();
            group.AddOutputField("Symbol");
            group.AddGroupFormulaForField("AveragePrice", CBDataAggregationGroupOperator.CBDataAggregationGroupAvg, "Price");
            commands.Add(group);
            */
            if (App.helper != null)
            {
                // search documents in the test collection
                //App.helper.DebugMode = true;
                //App.helper.SearchDocumentAggregate("security_master_3", commands, delegate(CBHelper.CBResponseInfo resp)
                App.helper.SearchDocument("users", cond, delegate(CBResponseInfo resp)
                {
                    this.OutputBox.Text = "OUTPUT: " + resp.OutputString;
                    return true;
                });
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            TestObject newObj = new TestObject();
            newObj.FirstName = "Cloud";
            newObj.LastName = "Base";
            newObj.Title = ".io";

            if (App.helper != null)
            {
                App.helper.InsertDocument("users", newObj, delegate(CBResponseInfo resp)
                {
                    this.OutputBox.Text = "OUTPUT: " + resp.OutputString;
                    return true;
                });
            }
        }

        private void ContentPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.helper != null)
            {
                App.helper.LogNavigation("DataPage");
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string ImageID = this.fileIdBox.Text;

            App.helper.DownloadFile(ImageID, delegate(byte[] imageData)
            {
                System.Diagnostics.Debug.WriteLine("called whenDone with data " + imageData.Length);
                MediaLibrary mediaLibrary = new MediaLibrary();
                mediaLibrary.SavePicture("new_image.jpg", imageData); // Saved Pictures album  
                    
                return true;
            });
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