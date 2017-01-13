using Foundation;
using System;
using UIKit;
using Esri.ArcGISRuntime.Portal;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Download controller contains the UI and logic for the download screen.
	/// </summary>
	partial class DownloadController : UIViewController
	{
		/// <summary>
		/// Unique identifier for the download session.
		/// </summary>
		const string sessionId = "com.esri.indoornavsession";

		/// <summary>
		/// Session used for transfer.
		/// </summary>
		NSUrlSession session;

		/// <summary>
		/// Download view model containing the common logic for setting up the download
		/// </summary>
		public DownloadViewModel ViewModel { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.DownloadController"/> class.
		/// </summary>
		DownloadController(IntPtr handle) : base(handle)
		{
			ViewModel = new DownloadViewModel();
			ViewModel.PropertyChanged += ViewModelPropertyChanged;
		}

		public override async void ViewDidLoad()
		{
			base.ViewDidLoad();
			InitializeNSUrlSession();
			// When the application has finished loading, bring in the settings
			string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			AppSettings.CurrentSettings = await AppSettings.CreateAsync(Path.Combine(settingsPath, "AppSettings.xml")).ConfigureAwait(false);

			// Call GetData to download or load the mmpk
			await ViewModel.GetDataAsync().ConfigureAwait(false);


		}

		/// <summary>
		/// Fires when properties change in the DownloadViewModel
		/// </summary>
		void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Status":
					if (ViewModel.Status == "Downloading")
					{
						InvokeOnMainThread(() => statusLabel.Text = "Downloading Mobile Map Package...");
					}
					else if (ViewModel.Status == "Ready")
					{
						InvokeOnMainThread(() => LoadMapView());
					}
					else {
						InvokeOnMainThread(() => statusLabel.Text = ViewModel.Status);
						progressView.Hidden = true;
						RetryButton.Hidden = false;
					}
					break;
				case "DownloadURL":
					EnqueueDownload(ViewModel.DownloadURL);
					break;

			}
		}

		/// <summary>
		/// Gets called by the delegate and will update the progress bar as the download runs.
		/// </summary>
		/// <param name="percentage">Percentage.</param>
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
		/// <param name="sender">Sender.</param>
		partial void RetryButton_TouchUpInside(UIButton sender)
		{
			ViewDidLoad();
		}

		/// <summary>
		/// Initializes the NSUrl session.
		/// </summary>
		void InitializeNSUrlSession()
		{
			// Initialize session config. Use a background session to enabled out of process uploads/downloads.
			using (var sessionConfig = UIDevice.CurrentDevice.CheckSystemVersion(8, 0)
				? NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(sessionId)
				: NSUrlSessionConfiguration.BackgroundSessionConfiguration(sessionId))
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
				session = NSUrlSession.FromConfiguration(sessionConfig, sessionDelegate, null);
			}
		}

		/// <summary>
		/// Adds the download to the session.
		/// </summary>
		void EnqueueDownload(string downloadUrl)
		{
			// Create a new download task.
			var downloadTask = session.CreateDownloadTask(NSUrl.FromString(downloadUrl));

			// Alert user if download fails
			if (downloadTask == null)
			{
				BeginInvokeOnMainThread(() =>
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