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
using System.ComponentModel;
using System.IO;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Download controller contains the UI and logic for the download screen.
    /// </summary>
    internal class DownloadController : UIViewController
    {
        private UIActivityIndicatorView _progressView;
        private UIButton _retryButton;
        private UILabel _statusLabel;

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

            try
            {
                // When the application has finished loading, bring in the settings
                string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                AppSettings.CurrentSettings = await AppSettings.CreateAsync(Path.Combine(settingsPath, "AppSettings.xml")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);

                // This will cause the retry button to be shown
                ViewModel.IsDownloading = false;
            }
        }

        public override void LoadView()
        {
            // Create the view model.
            ViewModel = new DownloadViewModel();

            // Create the views.
            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor, TintColor = ApplicationTheme.ActionBackgroundColor };

            _progressView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray) { TranslatesAutoresizingMaskIntoConstraints = false };
            _progressView.HidesWhenStopped = true;
            _retryButton = new UIButton { TranslatesAutoresizingMaskIntoConstraints = false };
            _statusLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextAlignment = UITextAlignment.Center,
                Text = "MapDownloadInProgressStatusLabel".Localize()
            };

            // Add subviews
            View.AddSubviews(_progressView, _retryButton, _statusLabel);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _statusLabel.CenterXAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterXAnchor),
                _progressView.CenterXAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterXAnchor),
                _retryButton.CenterXAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterXAnchor),
                _progressView.CenterYAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.CenterYAnchor),
                _statusLabel.BottomAnchor.ConstraintEqualTo(_progressView.TopAnchor, -ApplicationTheme.Margin),
                _retryButton.TopAnchor.ConstraintEqualTo(_progressView.BottomAnchor, 16),
                _progressView.WidthAnchor.ConstraintEqualTo(44),
                _progressView.HeightAnchor.ConstraintEqualTo(44),
                _retryButton.WidthAnchor.ConstraintEqualTo(_progressView.WidthAnchor),
                _statusLabel.WidthAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.WidthAnchor)
            });
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            try
            {
                // Call GetData to download or load the mobile map package
                await ViewModel.GetDataAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the error
                ErrorLogger.Instance.LogException(ex);

                // Create a new Alert Controller
                UIAlertController errorAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.Alert);

                // Add Actions
                errorAlert.AddAction(UIAlertAction.Create("RetryDownloadAlertText".Localize(), UIAlertActionStyle.Destructive, (action) => ViewModel.GetDataAsync().ConfigureAwait(false)));
                errorAlert.AddAction(UIAlertAction.Create("CloseAppAlertText".Localize(), UIAlertActionStyle.Default, (action) => System.Threading.Thread.CurrentThread.Abort()));

                // Display the alert
                PresentViewController(errorAlert, true, null);
            }
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

        /// <summary>
        /// Gets called by the delegate and tells the controller to load the map controller
        /// </summary>
        internal void LoadMapView()
        {
            var navController = new UINavigationController(new MapViewController(new MapViewModel()));

            // KeyWindow only works if the application loaded fully. If key window is null, use the first available window
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
        /// Attempts to download data when retry is clicked
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e"></param>
        private void RetryButton_TouchUpInside(object sender, EventArgs e) => ViewModel.GetDataAsync().ConfigureAwait(false);

        /// <summary>
        /// Updates UI in reaction to DownloadViewModel property changes
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event Args.</param>
        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.IsDownloading):
                    if (ViewModel.IsDownloading)
                    {
                        InvokeOnMainThread(() =>
                        {
                            ViewModel.Status = "MapDownloadInProgressStatusLabel".Localize();
                            _progressView.StartAnimating();
                            _retryButton.Hidden = true;
                        });
                    }
                    else
                    {
                        InvokeOnMainThread(() =>
                        {
                            _progressView.StopAnimating();
                            _retryButton.Hidden = false;
                        });
                    }

                    InvokeOnMainThread(() =>
                    {
                        _statusLabel.Text = ViewModel.Status;
                    });
                    break;

                case nameof(ViewModel.IsReady):
                    {
                        InvokeOnMainThread(LoadMapView);
                    }

                    break;
            }
        }
    }
}