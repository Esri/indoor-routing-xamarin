// <copyright file="DownloadController.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation.iOS
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Download controller contains the UI and logic for the download screen.
    /// </summary>
    internal partial class DownloadController : UIViewController
    {
        /// <summary>
        /// Unique identifier for the download session.
        /// </summary>
        private const string SessionId = "com.esri.indoorroutesession";

        /// <summary>
        /// Session used for transfer.
        /// </summary>
        private NSUrlSession session;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.DownloadController"/> class.
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        private DownloadController(IntPtr handle) : base(handle)
        {
            this.ViewModel = new DownloadViewModel();
            this.ViewModel.PropertyChanged += this.ViewModelPropertyChanged;
        }

        /// <summary>
        /// Gets or sets  the download view model containing the common logic for setting up the download
        /// </summary>
        public DownloadViewModel ViewModel { get; set; }

        /// <summary>
        /// Overrides the behavior when view is loaded
        /// </summary>
        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.InitializeNSUrlSession();

            // When the application has finished loading, bring in the settings
            string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            AppSettings.CurrentSettings = await AppSettings.CreateAsync(Path.Combine(settingsPath, "AppSettings.xml")).ConfigureAwait(false);

            // Call GetData to download or load the mmpk
            await this.ViewModel.GetDataAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Gets called by the delegate and will update the progress bar as the download runs.
        /// </summary>
        /// <param name="percentage">Percentage progress.</param>
        internal void UpdateProgress(float percentage)
        {
            progressView.SetProgress(percentage, true);
        }

        /// <summary>
        /// Gets called by the delegate and tells the controller to load the map controller
        /// </summary>
        internal void LoadMapView()
        {
            var navController = Storyboard.InstantiateViewController("NavController");

            // KeyWindow only works if the application loaded fully. If key window is null, use the first available windowo
            try
            {
                UIApplication.SharedApplication.KeyWindow.RootViewController = navController;
            }
            catch (NullReferenceException)
            {
                UIApplication.SharedApplication.Windows[0].RootViewController = navController;
            }
        }

        /// <summary>
        /// Handles button to reload the view 
        /// </summary>
        /// <param name="sender">Sender element.</param>
        partial void RetryButton_TouchUpInside(UIButton sender)
        {
            // Call GetData to download or load the mmpk
            this.ViewModel.GetDataAsync().ConfigureAwait(false);
        }

         /// <summary>
        /// Fires when properties change in the DownloadViewModel
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event Args.</param>
        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DownloadURL":
                    this.EnqueueDownload(this.ViewModel.DownloadURL);
                    break;

                case "IsDownloading":
                    if (this.ViewModel.IsDownloading == true)
                    {
                        this.InvokeOnMainThread(() =>
                        {
                            this.ViewModel.Status = "Downloading Map...";
                            progressView.Hidden = false;
                            RetryButton.Hidden = true;
                        });
                    }
                    else
                    {
                        this.InvokeOnMainThread(() =>
                        {
                            progressView.Hidden = true;
                            RetryButton.Hidden = false;
                        });
                    }

                    this.InvokeOnMainThread(() =>
                    {
                        statusLabel.Text = this.ViewModel.Status;
                    });
                    break;

                case "IsReady":
                    {
                        this.InvokeOnMainThread(() => this.LoadMapView());
                    }

                    break;
            }
        }

        /// <summary>
        /// Initializes the NSUrl session.
        /// </summary>
        private void InitializeNSUrlSession()
        {
            // Initialize session config. Use a background session to enabled out of process uploads/downloads.
            using (var sessionConfig = UIDevice.CurrentDevice.CheckSystemVersion(8, 0)
                ? NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionId)
                : NSUrlSessionConfiguration.BackgroundSessionConfiguration(SessionId))
            {
                // Allow downloads over cellular network
                sessionConfig.AllowsCellularAccess = true;

                // Give the OS a hint about what we are downloading. This helps iOS to prioritize. For example "Background" is used to download data that was not requested by the user and
                // should be ready if the app gets activated.
                sessionConfig.NetworkServiceType = NSUrlRequestNetworkServiceType.Default;

                // Configure how many downloads to allow at the same time. Set to 1 since we only meed to download one file
                sessionConfig.HttpMaximumConnectionsPerHost = 1;

                // Create a session delegate and the session itself
                // Initialize the session itself with the configuration and a session delegate.
                var sessionDelegate = new DownloadDelegate(this);
                this.session = NSUrlSession.FromConfiguration(sessionConfig, sessionDelegate, null);
            }
        }

        /// <summary>
        /// Adds the download to the session.
        /// </summary>
        /// <param name="downloadUrl">Download URL for the mmpk.</param>
        private void EnqueueDownload(string downloadUrl)
        {
            // Create a new download task.
            var downloadTask = this.session.CreateDownloadTask(NSUrl.FromString(downloadUrl));

            // Alert user if download fails
            if (downloadTask == null)
            {
                this.BeginInvokeOnMainThread(() =>
                {
                    var okAlertController = UIAlertController.Create("Download Error", "Failed to create download task, please retry", UIAlertControllerStyle.Alert);
                    okAlertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(okAlertController, true, null);
                });
                return;
            }

            // Resume / start the download.
            downloadTask.Resume();
        }
    }
}