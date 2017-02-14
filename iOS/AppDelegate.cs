// <copyright file="AppDelegate.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorRouting.iOS
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
