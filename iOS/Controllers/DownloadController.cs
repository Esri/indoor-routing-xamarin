// <copyright file="DownloadController.cs" company="Esri, Inc">
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
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Download controller contains the UI and logic for the download screen.
    /// </summary>
    internal class DownloadController : UIViewController
    {
        private UIProgressView _progressView;

        private UIButton _retryButton;

        private UILabel _statusLabel;

        /// <summary>
        /// Unique identifier for the download session.
        /// </summary>
        private const string SessionId = "com.esri.indoorroutesession";

        /// <summary>
        /// Session used for transfer.
        /// </summary>
        private NSUrlSession session;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorRouting.iOS.DownloadController"/> class.
        /// </summary>
        public DownloadController() { }

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
            _progressView.SetProgress(percentage, true);
        }

        /// <summary>
        /// Gets called by the delegate and tells the controller to load the map controller
        /// </summary>
        internal void LoadMapView()
        {
            var navController = new UINavigationController();
            navController.PushViewController(new MapViewController(), false);

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
        private void RetryButton_TouchUpInside(object sender, EventArgs e)
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
                            _progressView.Hidden = false;
                            _retryButton.Hidden = true;
                        });
                    }
                    else
                    {
                        this.InvokeOnMainThread(() =>
                        {
                            _progressView.Hidden = true;
                            _retryButton.Hidden = false;
                        });
                    }

                    this.InvokeOnMainThread(() =>
                    {
                        _statusLabel.Text = this.ViewModel.Status;
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
                this.session = NSUrlSession.FromConfiguration(sessionConfig, (INSUrlSessionDelegate)sessionDelegate, null);
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

        public override void LoadView()
        {
            base.LoadView();

            ViewModel = new DownloadViewModel();

            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _progressView = new UIProgressView { TranslatesAutoresizingMaskIntoConstraints = false };
            _retryButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _statusLabel = new UILabel { TranslatesAutoresizingMaskIntoConstraints = false };

            _statusLabel.TextAlignment = UITextAlignment.Center;
            _statusLabel.Text = "Downloading Map...";

            View.AddSubviews(_progressView, _retryButton, _statusLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _statusLabel.CenterXAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterXAnchor),
                _progressView.CenterXAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterXAnchor),
                _retryButton.CenterXAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterXAnchor),
                _progressView.CenterYAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterYAnchor),
                _statusLabel.BottomAnchor.ConstraintEqualTo(_progressView.TopAnchor, -16),
                _retryButton.TopAnchor.ConstraintEqualTo(_progressView.BottomAnchor, 16),
                _progressView.WidthAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.WidthAnchor, 0.5f),
                _retryButton.WidthAnchor.ConstraintEqualTo(_progressView.WidthAnchor),
                _statusLabel.WidthAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.WidthAnchor)
            });
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            _retryButton.TouchUpInside += RetryButton_TouchUpInside;
            ViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            _retryButton.TouchUpInside -= RetryButton_TouchUpInside;
            ViewModel.PropertyChanged -= ViewModelPropertyChanged;
        }
    }
}