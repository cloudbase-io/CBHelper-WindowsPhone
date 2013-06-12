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
using Microsoft.Phone.Info;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Converters;
using System.Device.Location;
using System.IO;
using Cloudbase.DataCommands;

namespace Cloudbase
{

/*! \mainpage cloudbase.io Windows Phone Helper Class Reference
*
* \section intro_sec Introduction
*
* This program is free software; you can redistribute it and/or modify it under
* the terms of the GNU General Public License, version 2, as published by
* the Free Software Foundation.<br/><br/>
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
* or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
* for more details.<br/><br/>

* You should have received a copy of the GNU General Public License
* along with this program; see the file COPYING.  If not, write to the Free
* Software Foundation, 59 Temple Place - Suite 330, Boston, MA
* 02111-1307, USA.<br/><br/>
*
* \section install_sec Getting Started
* 
* The cloudbase.io Windows Phone helper class compiles to .dll library.<br/><br/>
* The cloudbase.io helper class for Windows Phone utilizes the Newtonsoft.Json package. While this package
* is included in the solution you can get new updates and fixes using NuGet<br/><br/>
* This full reference is a companion to <a href="/documentation/windows-phone-8/get-started" target="_blank">
* the tutorial on the cloudbase.io website<a/>
*/

    /// <summary>
    /// The possible log levels for the log APIs
    /// </summary>
    public enum CBLogLevel
    {
        CBLogLevelDebug = 0,
        CBLogLevelInfo = 1,
        CBLogLevelWarning = 2,
        CBLogLevelError = 3,
        CBLogLevelFatal = 4,
        CBLogLevelEvent = 5 /// This is used to log custom events which will then be used to generate analytics
    }

    class CBJsonDictionaryConverter : CustomCreationConverter<IDictionary<string, object>>
    {
        public override IDictionary<string, object> Create(Type objectType)
        {
            return new Dictionary<string, object>();
        }

        public override bool CanConvert(Type objectType)
        {
            // in addition to handling IDictionary<string, object>
            // we want to handle the deserialization of dict value
            // which is of type object
            return objectType == typeof(object) || base.CanConvert(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject
                || reader.TokenType == JsonToken.Null)
                return base.ReadJson(reader, objectType, existingValue, serializer);

            // if the next token is not an object
            // then fall back on standard deserializer (strings, numbers etc.)
            return serializer.Deserialize(reader);
        }
    }

    class CBJsonArrayConverter : CustomCreationConverter<IList<object>>
    {
        public override IList<object> Create(Type objectType)
        {
            return new List<object>();
        }

        public override bool CanConvert(Type objectType)
        {
            // in addition to handling IDictionary<string, object>
            // we want to handle the deserialization of dict value
            // which is of type object
            return objectType == typeof(List<>) || base.CanConvert(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray
                || reader.TokenType == JsonToken.Null)
                return base.ReadJson(reader, objectType, existingValue, serializer);

            // if the next token is not an object
            // then fall back on standard deserializer (strings, numbers etc.)
            return serializer.Deserialize(reader);
        }
    }

    /// <summary>
    /// This container class is used to send attachment to the data APIs. when inserting or updating a document with attached files
    /// then a List of CBHelperAttachment objects should be sent.
    /// </summary>
    public class CBHelperAttachment
    {
        private string fileName;
        private byte[] fileData;

        /// <summary>
        /// The byte[] representing the content of the file
        /// </summary>
        public byte[] FileData
        {
            get { return fileData; }
            set { fileData = value; }
        }
        /// <summary>
        /// The name of the original file
        /// </summary>
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
    }

    /// <summary>
    /// The response information object the helper class will send to the delegate methods once
    /// a request is completed
    /// </summary>
    public class CBResponseInfo
    {
        private string _CBFunction;
        /// <summary>
        /// The name of the API functionality being used
        /// This could be
        ///     - Log
        ///     - Data
        ///     - Notifications
        ///     - Cloudfunction
        ///     - Applet
        /// </summary>
        public string CBFunction
        {
            get { return _CBFunction; }
            set { _CBFunction = value; }
        }

        private bool _status;
        /// <summary>
        /// Whether the call was successful or an error was thrown
        /// </summary>
        public bool Status
        {
            get { return _status; }
            set { _status = value; }
        }

        private object _data;
        /// <summary>
        /// A nested collection of Dictionary&lt;string, object&gt;
        /// </summary>
        public object Data
        {
            get { return _data; }
            set { _data = value; }
        }
        private int _httpStatus;
        /// <summary>
        /// The http status code the request completed with
        /// </summary>
        public int HttpStatus
        {
            get { return _httpStatus; }
            set { _httpStatus = value; }
        }

        private string _errorMessage;
        /// <summary>
        /// The error message returned by the cloudbase.io APIs, if any.
        /// </summary>
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        private string _outputString;
        /// <summary>
        /// The full JSON output returned by the cloudbase.io APIs
        /// </summary>
        public string OutputString
        {
            get { return _outputString; }
            set { _outputString = value; }
        }
    }

    /// <summary>
    /// This is the main cloudbase.io helper class. All API calls are accessible from this class.
    /// </summary>
    public class CBHelper
    {
        private string appCode;
        private string appUniq;
        private string _deviceUniqueIdentifier;
        /// <summary>
        /// a unique identifier for the device. By default we'll try to use the DeviceUniqueId from the DeviceExtendedProperties
        /// </summary>
        public string DeviceUniqueIdentifier
        {
            get { return _deviceUniqueIdentifier; }
            set { _deviceUniqueIdentifier = value; }
        }
        private string password;
        private string _authUsername;
        /// <summary>
        /// The username of the client application. If this field is populated it will be sent with each request to the cloudbase.io APIs
        /// and if the application security settings requeire it cloudbase.io will try to authenticate this user against the users collection
        /// </summary>
        public string AuthUsername
        {
            get { return _authUsername; }
            set { _authUsername = value; }
        }
        private string _authPassword;
        /// <summary>
        /// The password for the client application.
        /// </summary>
        public string AuthPassword
        {
            get { return _authPassword; }
            set { _authPassword = value; }
        }
        private string sessionId;
        /// <summary>
        /// If this variable is populated then the helper class will send cloudbase.io the position data with each request.
        /// </summary>
        public GeoPosition<GeoCoordinate> CurrentLocation;
        private string apiUrl;
        public string ApiUrl
        {
            get { return apiUrl; }
            set { apiUrl = value; }
        }

        private string userAgent;
        private string httpRequestBoundary;
        private bool isHttps;
        /// <summary>
        /// Whether or not to use the https APIs. By default this is set to true and we recommend always using https
        /// </summary>
        public bool IsHttps
        {
            get { return isHttps; }
            set { isHttps = value; }
        }
        private static string[] CBLogLevelDecode = { "DEBUG", "INFO", "WARNING", "ERROR", "FATAL", "EVENT" };
        private string logDefaultCategory;
        /// <summary>
        /// This string is used when no category is specified in the Log calls.
        /// </summary>
        public string LogDefaultCategory
        {
            get { return logDefaultCategory; }
            set { logDefaultCategory = value; }
        }

        private bool debugMode;
        /// <summary>
        /// If this parameter is set to true then the helper class will print out verbose debugging messages while executing
        /// the api calls
        /// </summary>
        public bool DebugMode
        {
            get { return debugMode; }
            set { debugMode = value; }
        }

        /// <summary>
        /// Creates a new instance of the CBHelper class. Right after the new object has been created you should call the SetPassword method to 
        /// complete the initialisation.
        /// </summary>
        /// <param name="appCode">The application code on cloudbase.io</param>
        /// <param name="appUniq">The unique code for the application generated by cloudbase.io when the application is
        /// created</param>
        public CBHelper(string appCode, string appUniq)
        {
            this.DebugMode = false;

            this.appCode = appCode;
            this.appUniq = appUniq;
            this.DeviceUniqueIdentifier = this.GetDeviceUniqueID(); // try and get the unique identifier. this can be overwritten at runtime by the user

            // default params
            this.apiUrl = "api.cloudbase.io";
            this.LogDefaultCategory = "DEFAULT";
            this.IsHttps = true;
            this.userAgent = "CBHelper-WinMo 0.1";
            this.httpRequestBoundary = "---------------------------14737809831466499882746641449";
        }

        /// <summary>
        /// Sets the application password for cloudbase.io. This is the second step of the helper class initialization and it is required
        /// for the object to work correctly. The moment the password is set the device is registered with cloudbase.io and can start calling
        /// the APIs and generating data for the analytics.
        /// </summary>
        /// <param name="apppwd">The MD5 hash of the cloudbase.io password. The CBHelper namespace contains an MD5 class which can be used
        /// to generate this: CBHelper.MD5Core.GetHashString("application password");</param>
        public void SetPassword(string apppwd) {
            this.password = apppwd.ToLower();
            this.RegisterDevice();
        }

        private void RegisterDevice()
        {
            string url = this.getUrl() + this.appCode + "/register";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("device_type", "WinMo");
            values.Add("device_name", DeviceStatus.DeviceName);
            values.Add("device_model", DeviceStatus.DeviceManufacturer + " - " + DeviceStatus.DeviceHardwareVersion);
            values.Add("language", Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
            values.Add("country", Thread.CurrentThread.CurrentCulture.Name.Split('-')[1]);

            this.sendRequest("register-device", url, values, null, null, delegate(CBResponseInfo resp) {
                // TODO: Parse response and get SessionID
                this.sessionId = Convert.ToString(((Dictionary<string, object>)resp.Data)["sessionid"]);
                if (this.debugMode)
                    System.Diagnostics.Debug.WriteLine("retrieved session: " + this.sessionId);
                return true;
            });
        }

        /// <summary>
        /// Sends a debug log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        public void LogDebug(string log)
        {
            LogDebug(log, this.LogDefaultCategory);
        }
        /// <summary>
        /// Sends a debug log message to the cloudbase.io log. This will be accessible from the "log" page of your account.
        /// </summary>
        /// <param name="log">The line to be logged</param>
        /// <param name="category">The category for the log entry</param>
        public void LogDebug(string log, string category)
        {
            SendLogMessage(log, CBLogLevel.CBLogLevelDebug, category);
        }
        /// <summary>
        /// Sends an info log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        public void LogInfo(string log)
        {
            LogInfo(log, this.LogDefaultCategory);
        }
        /// <summary>
        /// Sends an info log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        /// <param name="category">The category for the log entry</param>
        public void LogInfo(string log, string category)
        {
            SendLogMessage(log, CBLogLevel.CBLogLevelInfo, category);
        }
        /// <summary>
        /// Sends a warning log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        public void LogWarning(string log)
        {
            LogWarning(log, this.LogDefaultCategory);
        }
        /// <summary>
        /// Sends a warning log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        /// <param name="category">The category for the log entry</param>
        public void LogWarning(string log, string category)
        {
            SendLogMessage(log, CBLogLevel.CBLogLevelWarning, category);
        }
        /// <summary>
        /// Sends an error log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        public void LogError(string log)
        {
            LogError(log, this.LogDefaultCategory);
        }
        /// <summary>
        /// Sends an error log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        /// <param name="category">The category for the log entry</param>
        public void LogError(string log, string category)
        {
            SendLogMessage(log, CBLogLevel.CBLogLevelError, category);
        }
        /// <summary>
        /// Sends a fatal log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        public void LogFatal(string log)
        {
            LogFatal(log, this.LogDefaultCategory);
        }
        /// <summary>
        /// Sends a fatal log message to the cloudbase.io log. This will be accessible from the "log" page of your account
        /// </summary>
        /// <param name="log">The line to be logged</param>
        /// <param name="category">The category for the log entry</param>
        public void LogFatal(string log, string category)
        {
            SendLogMessage(log, CBLogLevel.CBLogLevelFatal, category);
        }
        /// <summary>
        /// Sends an event log message to the cloudbase.io log. These messages are used to generate custom event analytics.
        /// </summary>
        /// <param name="log">The line to be logged</param>
        /// /// <param name="category">The category for the log entry</param>
        public void LogEvent(string log, string category)
        {
            SendLogMessage(log, CBLogLevel.CBLogLevelEvent, category);
        }
        private void SendLogMessage(string log, CBLogLevel logLevel, string category)
        {
            string url = this.getUrl() + this.appCode + "/log";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("category", category);
            values.Add("level", CBLogLevelDecode[(int)logLevel]);
            values.Add("log_line", log);
            values.Add("device_name", "WinMo");
            values.Add("device_model", "0.1");

            this.sendRequest("log", url, values, null);
        }
        /// <summary>
        /// This call tells the cloudbase.io APIs that the user has moved to a new page. Data collected through this API
        /// is used in the usage flow analytics to show how your users interact with your application
        /// </summary>
        /// <param name="ScreenName">The name of the page</param>
        public void LogNavigation(string ScreenName)
        {
            if (this.sessionId == null)
                return;
    
            string url = this.getUrl() + this.appCode + "/lognavigation";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("session_id", this.sessionId);
            values.Add("screen_name", ScreenName);
    
            this.sendRequest("log-navigation", url, values, null);
        }

        /// <summary>
        /// Inserts the given object in a cloudbase collection. If the collection does not exist it is automatically created.
        /// Similarly if the data structure of the given object is different from documents already present in the collection
        /// the structure is automatically altered to accommodate the new object.
        /// The system will automatically try to serialise any object sent to this function. However, we recommend you use
        /// the simplest possible objects to hold data if not a Dictionary or a List directly. 
        /// Once the call to the APIs is completed the requestCompleted method is triggered in the delegate.
        /// </summary>
        /// <param name="collection">The name of the collection to save the object into</param>
        /// <param name="document">The object to be serialized.</param>
        /// <param name="whenDone">The delegate to be called with the response once the request is completed</param>
        public void InsertDocument(string collection, object document, Func<CBResponseInfo, bool> whenDone)
        {
            this.InsertDocument(collection, document, null, whenDone);
        }

        /// <summary>
        /// Inserts the given object in a cloudbase collection. If the collection does not exist it is automatically created.
        /// Similarly if the data structure of the given object is different from documents already present in the collection
        /// the structure is automatically altered to accommodate the new object.
        /// The system will automatically try to serialise any object sent to this function. However, we recommend you use
        /// the simplest possible objects to hold data if not a Dictionary or a List directly.
        /// The attachments will be stored in the cloudbase file system and an additional "cb_files" field will be created in your document.
        /// This contains a list of all the files attached to the document which you will be able to retrieve using the DownloadFile
        /// method with the ID from the cb_file field.
        /// Once the call to the APIs is completed the requestCompleted method is triggered in the delegate.
        /// </summary>
        /// <param name="collection">The name of the collection to save the object into</param>
        /// <param name="document">The object to be serialized</param>
        /// <param name="fileAttachments">A list of attachments for the document</param>
        /// <param name="whenDone">The delegate to be called with the response once the request is completed</param>
        public void InsertDocument(string collection, object document, List<CBHelperAttachment> fileAttachments, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/" + collection + "/insert";

            List<object> finalDocument;
            if (document.GetType() == typeof(List<>))
                finalDocument = (List<object>)document;
            else
            {
                finalDocument = new List<object>();
                finalDocument.Add(document);
            }

            this.sendRequest("data", url, finalDocument, null, fileAttachments, whenDone);
        }
        /// <summary>
        /// Runs a search over a collection with the given criteria. The documents matching the search criteria are then
        /// returned in the CBResponseInfo object passed to the requestCompleted method of the delegate.
        /// </summary>
        /// <param name="collection">The name of the collection to search over</param>
        /// <param name="conditions">A set of conditions for the search</param>
        /// <param name="whenDone">The delegate to be called once the request is completed</param>
        public void SearchDocument(string collection, CBHelperSearchCondition conditions, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/" + collection + "/search";

            this.sendRequest("data", url, conditions.SerializeConditions(), null, null, whenDone);
        }
        /// <summary>
        /// Runs a search over a collection and applies the given list of aggregation commands to the output.
        /// </summary>
        /// <param name="collection">The name of the collection to search over</param>
        /// <param name="aggregateConditions">A set of conditions for the search</param>
        /// <param name="whenDone">The delegate to be called once the request is completed</param>
        public void SearchDocumentAggregate(string collection, List<CBDataAggregationCommand> aggregateConditions, Func<CBResponseInfo, bool> whenDone)
        {
            List<Dictionary<string, object>> serializedAggregateConditions = new List<Dictionary<string, object>>();

            foreach (CBDataAggregationCommand comm in aggregateConditions)
            {
                Dictionary<string, object> serializedCommand = new Dictionary<string, object>();
                serializedCommand.Add(comm.GetCommandTypeString(), comm.SerializeAggregateConditions());

                serializedAggregateConditions.Add(serializedCommand);
            }

            Dictionary<string, object> finalConditions = new Dictionary<string, object>();
            finalConditions.Add("cb_aggregate_key", serializedAggregateConditions);

            string url = this.getUrl() + this.appCode + "/" + collection + "/aggregate";

            this.sendRequest("data", url, finalConditions, null, null, whenDone);
        }

        /// <summary>
        /// Updates one or many documents in a cloudbase collection with the given document/list of documents.
        /// Only documents that match the conditions are updated.
        /// </summary>
        /// <param name="collection">The name of the collection</param>
        /// <param name="conditions">A set of conditions to lookup the documents to be updated</param>
        /// <param name="document">The document to be updated. Documents matching the search conditions will be replaced with this value</param>
        /// <param name="whenDone">The delegate to be called once the request is completed</param>
        public void UpdateDocument(string collection, CBHelperSearchCondition conditions, object document, Func<CBResponseInfo, bool> whenDone)
        {
            this.UpdateDocument(collection, conditions, document, null, whenDone);
        }
        /// <summary>
        /// Updates one or many documents in a cloudbase collection with the given document/list of documents.
        /// Only documents that match the conditions are updated.
        /// </summary>
        /// <param name="collection">The name of the collection</param>
        /// <param name="conditions">A set of conditions to lookup the documents to be updated</param>
        /// <param name="document">The document to be updated. Documents matching the search conditions will be replaced with this value</param>
        /// <param name="fileAttachments">A List of file attachments for the new documents</param>
        /// <param name="whenDone">The delegate to be called once the request is completed</param>
        public void UpdateDocument(string collection, CBHelperSearchCondition conditions, object document, List<CBHelperAttachment> fileAttachments, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/" + collection + "/update";

            List<object> finalDocument = new List<object>(); ;
            if (document.GetType() == typeof(List<>))
            {
                foreach (object curDoc in (List<object>)document)
                {
                    string jsonObj = JsonConvert.SerializeObject(curDoc);
                    IDictionary<string, object> parsedObject = JsonConvert.DeserializeObject<IDictionary<string, object>>(
                                jsonObj, new JsonConverter[] { new CBJsonDictionaryConverter(), new CBJsonArrayConverter() });
                    parsedObject.Add("cb_search_key", conditions);
                    finalDocument.Add(parsedObject);
                }
            }
            else
            {
                string jsonObj = JsonConvert.SerializeObject(document);
                IDictionary<string, object> parsedObject = JsonConvert.DeserializeObject<IDictionary<string, object>>(
                            jsonObj, new JsonConverter[] { new CBJsonDictionaryConverter(), new CBJsonArrayConverter() });
                parsedObject.Add("cb_search_key", conditions);
                    
                finalDocument.Add(document);
            }

            this.sendRequest("data", url, finalDocument, null, fileAttachments, whenDone);
        }

        /// <summary>
        /// This method retrieves a file owned by the current application from the cloudbase.io file system using the file id string
        /// from a cb_files field.
        /// </summary>
        /// <param name="FileID">The file id taken from the cb_files value of a document in a collection</param>
        /// <param name="whenDone">Once the download is finished this delegate receives the content of the file in the form of a byte[]</param>
        public void DownloadFile(string FileID, Func<byte[], bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/file/" + FileID;

            this.sendRequest("download", url, null, null, null, null, whenDone);
        }

        /// <summary>
        /// Subscribes the devices with the current Uri received from Microsoft to a notification channel. All devices are
        /// autmatically subscribed to the channel <strong>all</strong>.
        /// </summary>
        /// <param name="NotificationUri">The Uri token returned by HttpNotificationChannel</param>
        /// <param name="channel">The name of the channel to subscribe to in addition to all. This field can be sent as null</param>
        public void NotificationSubscribeDeviceToChannel(string NotificationUri, string channel)
        {
            string url = this.getUrl() + this.appCode + "/notifications-register";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("action", "subscribe");
            values.Add("device_key", NotificationUri);
            values.Add("device_network", "win");
            values.Add("channel", channel);

            this.sendRequest("notifications-register", url, values, null);
        }

        /// <summary>
        /// Unsubscribes the devices with the current Uri token received from Microsoft from a notification channel.
        /// </summary>
        /// <param name="NotificationUri">The Uri token returned by HttpNotificationChannel</param>
        /// <param name="channel">The name of the channel to unsubscribe from</param>
        /// <param name="andAll">This parameter tells the cloudbase.io APIs to remove the device completely from the notification channels
        /// including the "all" channel</param>
        public void NotificationUnsubscribeDeviceFromChannel(string NotificationUri, string channel, bool andAll)
        {
            string url = this.getUrl() + this.appCode + "/notifications-register";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("action", "unsubscribe");
            values.Add("device_key", NotificationUri);
            values.Add("device_network", "win");
            values.Add("channel", channel);
            if (andAll)
                values.Add("from_all", "true");

            this.sendRequest("notifications-register", url, values, null);
        }

        /// <summary>
        /// Sends a toast push notification to all devices subscribed to the channel.
        /// Client devices should be explicitly allowed to send push notifications in the application security settings on cloudbase.io
        /// </summary>
        /// <param name="channel">The name of the channel to send the notification to - only devices subscibed to this channel will be notified</param>
        /// <param name="title">The first line of text for the toast notification (&lt;wp:Text1&gt;)</param>
        /// <param name="subtitle">The second line of text for the toast notification (&lt;wp:Text2&gt;)</param>
        public void SendToastNotification(string channel, string title, string subtitle)
        {
            string url = this.getUrl() + this.appCode + "/notifications";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("channel", channel);
            values.Add("win_type", "toast");
            values.Add("alert", title);
            values.Add("alert2", subtitle);

            this.sendRequest("notifications", url, values, null);
        }

        /// <summary>
        /// Sends a tile push notification to all devices subscribed to the channel.
        /// Client devices should be explicitly allowed to send push notifications in the application security settings on cloudbase.io
        /// 
        /// For more information see http://msdn.microsoft.com/en-us/library/windowsphone/develop/hh202970(v=vs.92).aspx
        /// </summary>
        /// <param name="channel">The name of the channel to send the notification to - only devices subscibed to this channel will be notified</param>
        /// <param name="title">The title for the tile notification (&lt;wp:Title&gt;)</param>
        /// <param name="count">The number to be displayed on the tile (&lt;wp:Count&gt;)</param>
        /// <param name="backgroundImage">The background image parameter for the notification (&lt;wp:BackgroundImage&gt;)</param>
        /// <param name="backBackgroundImage">The back background image for the notification (&lt;wp:BackBackgroundImage&gt;)</param>
        /// <param name="backTitle">The back title parameter for the notification (&lt;wp:BackTitle&gt;)</param>
        /// <param name="backContent">The back content for the tile notification (&lt;wp:BackContent&gt;)</param>
        public void SendTileNotification(string channel, string title, int count, string backgroundImage, string backBackgroundImage, string backTitle, string backContent)
        {
            string url = this.getUrl() + this.appCode + "/notifications";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("channel", channel);
            values.Add("win_type", "tile");
            values.Add("alert", title);
            values.Add("win_count", Convert.ToString(count));
            values.Add("win_background_image", backgroundImage);
            values.Add("win_back_background_image", backgroundImage);
            values.Add("win_back_title", backTitle);
            values.Add("win_back_content", backContent);

            this.sendRequest("notifications", url, values, null);
        }

        /// <summary>
        /// Sends a raw push notification to all devices in the given channel. This will be sent as a standard text notification to iOS
        /// and Android devices.
        /// </summary>
        /// <param name="channel">The name of the channel to send the notification to - only devices subscibed to this channel will be notified</param>
        /// <param name="value1">The first line of text for the raw notification</param>
        /// <param name="value2">The second line of text</param>
        public void SendRawNotification(string channel, string value1, string value2)
        {
            string url = this.getUrl() + this.appCode + "/notifications";
            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("channel", channel);
            values.Add("win_type", "raw");
            values.Add("alert", value1);
            values.Add("alert2", value2);

            this.sendRequest("notifications", url, values, null);
        }

        /// <summary>
        /// Send an email to the specified recipient using the given template.
        /// 
        /// Before sending emails through the cloudbase.io APIs please verify your DNS configuration as described here
        /// http://cloudbase.io/documentation/rest-apis/emails
        /// </summary>
        /// <param name="Template">The template code as created on cloudbase.io</param>
        /// <param name="Recipient">The email address of the recipient</param>
        /// <param name="Subject">The subject of the email</param>
        /// <param name="vars">A set of variables to fill the template</param>
        public void SendEmail(string Template, string Recipient, string Subject, Dictionary<string, string> vars)
        {
            string url = this.getUrl() + this.appCode + "/email";
            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("template_code", Template);
            values.Add("recipient", Recipient);
            values.Add("subject", Subject);
            values.Add("variables", vars);

            this.sendRequest("email", url, values, null);
        }

        /// <summary>
        /// Executes a CloudFunction on demand.
        /// </summary>
        /// <param name="FunctionCode">The unique code identifying the function as created on cloudbase.io</param>
        /// <param name="Params">Additional parameters to be passed to the function - these will be accessible to the function in the form
        /// of Http POST parameters</param>
        /// <param name="whenDone">This delegate will receive the output of the function once the execution is completed.</param>
        public void ExecuteCloudFunction(string FunctionCode, Dictionary<string, string> Params, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/cloudfunction/" + FunctionCode;

            this.sendRequest("cloudfunction", url, null, Params, null, whenDone);
        }
        /// <summary>
        /// Executes an Applet on demand. http://cloudbase.io/documentation/applets/get-started
        /// </summary>
        /// <param name="FunctionCode">The unique code identifying the applet on cloudbase.io</param>
        /// <param name="Params">Additional parameters to be passed to the applet</param>
        /// <param name="whenDone">This delegate will receive the output of the applet once the execution is completed.</param>
        public void ExecuteApplet(string AppletCode, Dictionary<string, string> Params, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/applet/" + AppletCode;

            this.sendRequest("applet", url, null, Params, null, whenDone);
        }

        /// <summary>
        /// Executes a Shared Api on demand. http://cloudbase.io/documentation/windows-phone-8/cloud-functions
        /// </summary>
        /// <param name="ApiCode">The unique code identifying the Shared API on cloudbase.io</param>
        /// <param name="Password">The password to access the Shared API if necessary</param>
        /// <param name="Params">Additional parameters to be passed to the Shared Api</param>
        /// <param name="whenDone">This delegate will receive the output of the Shared Api once the execution is completed.</param>
        public void ExecuteSharedApi(string ApiCode, string Password, Dictionary<string, string> Params, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/shared/" + ApiCode;

            if (Password != null && !Password.Equals(""))
            {
                Params.Add("cb_shared_password", Password);
            }

            this.sendRequest("shared-api", url, null, Params, null, whenDone);
        }

        /// <summary>
        /// Calls PayPal and requests a token for the express checkout of digital goods.
        /// The PayPal API credentials must be set in the cloudbase.io control panel for this method to work.
        /// </summary>
        /// <param name="bill">A populated CBPayPalBill object with at least one detail item</param>
        /// <param name="isLiveEnvironment">Whether the call should be made to the PayPal production or sandbox environments</param>
        /// <param name="whenDone">A delegate to manage the results returned from the server - specifically the token and checkout url</param>
        public void PreparePayPalPurchase(CBPayPalBill bill, bool isLiveEnvironment, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/paypal/prepare";

            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("purchase_details", bill.serializePurchase());
            values.Add("environment", (isLiveEnvironment ? "live" : "sandbox"));
            values.Add("currency", bill.Currency);
            values.Add("type", "purchase");
            values.Add("completed_cloudfunction", bill.PaymentCompletedFunction);
            values.Add("cancelled_cloudfunction", bill.PaymentCancelledFunction);
            if (bill.PaymentCompletedUrl != null)
                values.Add("payment_completed_url", bill.PaymentCompletedUrl);
            if (bill.PaymentCancelledUrl != null)
                values.Add("payment_cancelled_url", bill.PaymentCancelledUrl);

            this.sendRequest("paypal", url, values, null, null, whenDone);
        }
        
        /// <summary>
        /// This method checks whether a PayPal transaction initiated with the PreparePayPalPurchase method is completed.
        /// This should be called on the Navigating event of the WebBrowser object. The Uri from the Navigating Event should
        /// be passed to this method.
        /// The Payment status on cloudbase.io will automatically be updated. Once that is completed your whenDone method will
        /// be triggered.
        /// The method will return right away and you can start closing the Page and handle the outcome of the payment in the 
        /// whenDone method
        /// </summary>
        /// <param name="browserUri">The Uri received from the Navigating event of the WebBrowser object</param>
        /// <param name="whenDone">A delegate to handle the outcome of the payment</param>
        /// <returns>true if the payment is complete and you can close the Browser. false if PayPal is still interacting
        /// with the user and processing the payment</returns>
        public bool IsPayPayPaymentComplete(Uri browserUri, Func<CBResponseInfo, bool> whenDone)
        {
            string redirectUrl = browserUri.AbsoluteUri;

            if (redirectUrl.IndexOf("/paypal/update-status") != -1)
            {
                this.sendRequest("paypal", redirectUrl, new Dictionary<string, string>(), null, null, whenDone);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Retrieves the information about a PayPal purchase which has been initiated with the preparePayPalPurchase method.
        /// The paymentId is returned when the payment is prepared and completed.
        /// </summary>
        /// <param name="paymentId">The payment id returned by cloudbase.io</param>
        /// <param name="whenDone">A delegate to use the details returned by the cloudbase.io APIs</param>
        public void GetPayPalPaymentDetails(string paymentId, Func<CBResponseInfo, bool> whenDone)
        {
            string url = this.getUrl() + this.appCode + "/paypal/payment-details";

            Dictionary<string, string> values = new Dictionary<string, string>();
            values.Add("payment_id", paymentId);

            this.sendRequest("paypal", url, values, null, null, whenDone);
        }

        private string getUrl()
        {
            return (this.isHttps ? "https://" : "http://") + this.apiUrl + "/";
        }

        private void requestBodyForParameter(Stream stream, string paramName, string paramValue)
        {
            StreamWriter sw = new StreamWriter(stream);
            sw.Write("--" + this.httpRequestBoundary + "\r\n");
            sw.Write("Content-Disposition: form-data; name=\"" + paramName + "\"\r\n\r\n");
            sw.Write(paramValue);
            sw.Write("\r\n");
            sw.Flush();
        }

        private void requestBodyForFile(Stream stream, CBHelperAttachment file, int sequenceNumber)
        {
            //string output = "";
            StreamWriter sw = new StreamWriter(stream);
            sw.Write("--" + this.httpRequestBoundary + "\r\n");
            sw.Write("Content-Disposition: attachment; name=\"file_" + sequenceNumber + "\"; filename=\"" + file.FileName + "\"\r\n");
            sw.Write("Content-Type: application/octet-stream\r\n\r\n");
            sw.Flush();
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(file.FileData);
            bw.Flush();

            sw = new StreamWriter(stream); 
            sw.Write("\r\n");
            sw.Flush();
            //return output;
        }

        /// <summary>
        /// Recives a byte[] and return an ASCII encoded string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string AsciiToString(byte[] bytes) { 
            StringBuilder sb = new StringBuilder(bytes.Length); 
            foreach(byte b in bytes) {
                sb.Append(b<=0x7f ? (char)b : '?'); 
            } 
            return sb.ToString(); 
        }

        /// <summary>
        /// Receives a string and returns a byte[] using ASCII encoding
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] StringToAscii(string s)
        {
            byte[] retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix)
            {
                char ch = s[ix];
                if (ch <= 0x7f) retval[ix] = (byte)ch;
                else retval[ix] = (byte)'?';
            }
            return retval;
        }

        private void prepareParameters(Stream sw, object postData, Dictionary<string, string> additionalParameters, List<CBHelperAttachment> files)
        {
            this.requestBodyForParameter(sw, "app_uniq", this.appUniq);
            this.requestBodyForParameter(sw, "app_pwd", this.password);
            this.requestBodyForParameter(sw, "device_uniq", this.DeviceUniqueIdentifier);
            this.requestBodyForParameter(sw, "post_data", (postData != null?JsonConvert.SerializeObject(postData):""));
            if (this.debugMode)
                System.Diagnostics.Debug.WriteLine("object data: " + (postData != null ? JsonConvert.SerializeObject(postData) : ""));

            if (additionalParameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in additionalParameters)
                {
                    this.requestBodyForParameter(sw, kvp.Key, kvp.Value);
                }
            }

            if (this.AuthUsername != null)
            {
                this.requestBodyForParameter(sw, "cb_auth_user", this.AuthUsername);
                this.requestBodyForParameter(sw, "cb_auth_password", this.AuthPassword);
            }

            if (this.CurrentLocation != null)
            {
                Dictionary<string, string> location = new Dictionary<string, string>();
                location.Add("lat", Convert.ToString(this.CurrentLocation.Location.Latitude));
                location.Add("lng", Convert.ToString(this.CurrentLocation.Location.Longitude));
                location.Add("alt", Convert.ToString(this.CurrentLocation.Location.Altitude));

                this.requestBodyForParameter(sw, "location_data", JsonConvert.SerializeObject(location));
                if (this.debugMode)
                    System.Diagnostics.Debug.WriteLine("sending location: " + JsonConvert.SerializeObject(location));
            }

            if (files != null)
            {
                int filesCounter = 0;
                foreach (CBHelperAttachment curFile in files)
                {
                    this.requestBodyForFile(sw, curFile, filesCounter);
                    filesCounter++;
                }
            }
            StreamWriter writer = new StreamWriter(sw);
            writer.Write("--" + this.httpRequestBoundary + "--\r\n");
            writer.Flush();
        }

        private void sendRequest(string function, string url, object postData, Dictionary<string, string> additionalPostParameters)
        {
            sendRequest(function, url, postData, additionalPostParameters, null, null, null);
        }

        private void sendRequest(string function, string url, object postData, Dictionary<string, string> additionalPostParameters, List<CBHelperAttachment> fileAttachments, Func<CBResponseInfo, bool> whenDone)
        {
            sendRequest(function, url, postData, additionalPostParameters, fileAttachments, whenDone, null);
        }

        private void sendRequest(string function, string url, object postData, Dictionary<string, string> additionalPostParameters, List<CBHelperAttachment> fileAttachments, Func<CBResponseInfo, bool> whenDone, Func<byte[], bool> downloadDone)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.ContentType = "multipart/form-data; boundary=" + this.httpRequestBoundary;
            req.UserAgent = this.userAgent;
            req.Method = "POST";
            
            req.BeginGetRequestStream(delegate(IAsyncResult asynchronousResult)
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

                // End the operation
                Stream postStream = request.EndGetRequestStream(asynchronousResult);

                this.prepareParameters(postStream, postData, additionalPostParameters, fileAttachments);
                postStream.Close();

                // Start the asynchronous operation to get the response
                request.BeginGetResponse(new AsyncCallback(delegate(IAsyncResult responseResult)
                {
                    HttpWebRequest respReq = (HttpWebRequest)asynchronousResult.AsyncState;

                    // End the operation
                    HttpWebResponse response = (HttpWebResponse)respReq.EndGetResponse(responseResult);
                    Stream streamResponse = response.GetResponseStream();
                    
                    if (function.Equals("download"))
                    {
                        byte[] responseBytes = new byte[streamResponse.Length];
                        streamResponse.Read(responseBytes, 0, (int)streamResponse.Length);

                        if (downloadDone != null)
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                downloadDone(responseBytes);
                            });  
                        }
                        streamResponse.Close();
                    }
                    else
                    {
                        StreamReader streamRead = new StreamReader(streamResponse);
                        string responseString = streamRead.ReadToEnd();

                        Dictionary<string, object> parsedObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                responseString, new JsonConverter[] { new CBJsonDictionaryConverter(), new CBJsonArrayConverter() });
                        //JsonConvert.DeserializeObject(responseString, IDictionary<string, object>, new JsonConverter[] { new CBJsonDictionaryConverter(), new CBJsonArrayConverter() });
                        //Dictionary<string, object> parsedObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);//JsonConvert.DeserializeObject<Dictionary<string, string>>(output);//JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(output);

                        if (this.debugMode)
                            System.Diagnostics.Debug.WriteLine("oubject out: " + JsonConvert.SerializeObject(parsedObject));

                        if (whenDone != null)
                        {
                            CBResponseInfo resp = new CBResponseInfo();
                            resp.HttpStatus = 200;
                            resp.Status = (Convert.ToString(((Dictionary<string, object>)parsedObject[function])["status"]).Equals("OK"));
                            resp.CBFunction = function;
                            resp.Data = ((Dictionary<string, object>)parsedObject[function])["message"];
                            resp.OutputString = JsonConvert.SerializeObject(((Dictionary<string, object>)parsedObject[function])["message"]);
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                whenDone(resp);
                            });  
                            
                        }
                        streamResponse.Close();
                        streamRead.Close();
                    }

                    // Release the HttpWebResponse
                    response.Close();

                }), request);

            }, req);
        }

        private string GetDeviceUniqueID()
        {
            //object DeviceUniqueID;

            byte[] DeviceIDbyte = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");

            //if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out DeviceUniqueID))
            //{
                //DeviceIDbyte = (byte[])DeviceUniqueID;

                string DeviceID = Convert.ToBase64String(DeviceIDbyte);
                if (this.debugMode)
                    System.Diagnostics.Debug.WriteLine("generated unique device ID: " + DeviceID);
                return DeviceID;
            //}
            
        }
    }
}
