// <copyright file="AppDelegate.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;

    /// <summary>
    /// The UIApplicationDelegate for the application. This class is responsible for launching the
    /// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    /// </summary>
    [Register("AppDelegate")]
    internal class AppDelegate : UIApplicationDelegate
    {
        /// <summary>
        /// The task identifier, will hold the value of the finite background task
        /// </summary>
        private nint taskID = -1;

        /// <summary>
        /// Gets or sets the main window.
        /// </summary>
        /// <value>The main window.</value>
        public override UIWindow Window
        {
            get;
            set;
        }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // create a new window instance based on the screen size
            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            Window.RootViewController = new DownloadController();

            // make the window visible
            Window.MakeKeyAndVisible();

            return true;
        }

        /// <summary>
        /// Runs before the app finishes launching.
        /// </summary>
        /// <returns><c>true</c>, if finish launching was willed, <c>false</c> otherwise.</returns>
        /// <param name="application">Application handle.</param>
        /// <param name="launchOptions">Launch options.</param>
        public override bool WillFinishLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // TODO: Set up your app key here to remove "For developers use only" watermark. 
            // Go to the documentation for instructions on how to get a license key
            ////string licenseKey = "runtimelite,1000,rud#########,day-month-year,####################";
            ////Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense(licenseKey);

            return true;
        }

        /// <summary>
        /// Overrides the behavior of the application when it has entered background mode
        /// </summary>
        /// <param name="application">Main Application.</param>
        public override async void DidEnterBackground(UIApplication application)
        {
            // Begin Finite-Length Task.
            this.taskID = UIApplication.SharedApplication.BeginBackgroundTask(null);

            // Start saving the user choices.
            await Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));

            // End Finite-Length Task, finished.
            if (this.taskID != -1)
            {
                UIApplication.SharedApplication.EndBackgroundTask(this.taskID);
                this.taskID = -1;
            }
        }

        /// <summary>
        /// Call this when the transfer of all files is done.
        /// </summary>
        public static Action BackgroundSessionCompletionHandler;
    }
}
