// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    /// <summary>
    /// Download delegate handles logic of the background download.
    /// </summary>
    internal class DownloadDelegate : NSUrlSessionDownloadDelegate
    {
        /// <summary>
        /// Reference to the download controller 
        /// </summary>
        private readonly DownloadController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers.DownloadDelegate"/> class.
        /// </summary>
        /// <param name="controller">Download Controller.</param>
        internal DownloadDelegate(DownloadController controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// Gets called as data is being received
        /// </summary>
        /// <param name="session">NSUrl Session.</param>
        /// <param name="downloadTask">Download task.</param>
        /// <param name="bytesWritten">Bytes written.</param>
        /// <param name="totalBytesWritten">Total bytes written.</param>
        /// <param name="totalBytesExpectedToWrite">Total bytes expected to write.</param>
        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
        {
            var percentage = (float)totalBytesWritten / totalBytesExpectedToWrite;

            InvokeOnMainThread(() => _controller.UpdateProgress(percentage));
        }

        /// <summary>
        /// Gets called when the download has been completed.
        /// </summary>
        /// <param name="session">NSUrl Session.</param>
        /// <param name="downloadTask">Download task.</param>
        /// <param name="location">NSUrl Location.</param>
        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
        {
            // The download location is the location of the file
            var sourceFile = location.Path;

            // Copy over to documents folder. Note that we must use NSFileManager here! File.Copy() will not be able to access the source location.
            var fileManager = NSFileManager.DefaultManager;

            // Remove any existing files in our destination
            fileManager.Remove(Path.Combine(DownloadViewModel.GetDataFolder(), AppSettings.CurrentSettings.PortalItemName), out var error);
            var success = fileManager.Copy(sourceFile, Path.Combine(DownloadViewModel.GetDataFolder(), AppSettings.CurrentSettings.PortalItemName), out error);
            if (!success)
            {
                Console.WriteLine("Error during the copy: {0}", error.LocalizedDescription);
            }

            InvokeOnMainThread(() => _controller.LoadMapView());
        }

        /// <summary>
        /// Gets called when a download is done. Does not necessarily indicate an error
        /// unless the NSError parameter is not null.
        /// </summary>
        /// <param name="session">NSUrl Session.</param>
        /// <param name="task">Session Task.</param>
        /// <param name="error">Error received.</param>
        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
            if (error == null)
            {
                AppSettings.CurrentSettings.MmpkDownloadDate = DateTime.Now;

                // Save user settings
                Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
                return;
            }

            // If error indeed occured, cancel the task
            task.Cancel();
        }

        /// <summary>
        /// Gets called by iOS if all pending transfers are done. This will only be called if the app was backgrounded.
        /// </summary>
        /// <param name="session">NSUrl Session.</param>
        public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
        {
            // Nothing more to be done. This is the place where we have to call the completion handler we get passed in in AppDelegate.
            var handler = AppDelegate.BackgroundSessionCompletionHandler;
            AppDelegate.BackgroundSessionCompletionHandler = null;
            if (handler != null)
            {
                _controller.BeginInvokeOnMainThread(() =>
                {
                    // Bring up a local notification to take the user back to our app.
                    var notification = new UILocalNotification
                    {
                        AlertBody = "Indoor Routing: Map has been downloaded successfully."
                    };
                    UIApplication.SharedApplication.PresentLocalNotificationNow(notification);

                    // Invoke the completion handler. This will tell iOS to update the snapshot in the task manager.
                    handler.Invoke();
                });
            }
        }
    }
}
