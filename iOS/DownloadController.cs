using Foundation;
using System;
using UIKit;
using Esri.ArcGISRuntime.Portal;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Download controller contains the UI and logic for the download screen.
	/// </summary>
    partial class DownloadController : UIViewController
	{
		DownloadController(IntPtr handle) : base(handle)
		{
		}

		/// <summary>
		/// This is where the MMPK will be saved
		/// </summary>
		static string targetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		internal static string targetFilename = Path.Combine(targetPath, "EsriCampus.mmpk");

		/// <summary>
		/// Unique identifier for the download session.
		/// </summary>
		const string sessionId = "com.esri.indoornavsession";

		/// <summary>
		/// Session used for transfer.
		/// </summary>
		NSUrlSession session;

		/// <summary>
		/// Gets called by the delegate and will update the progress bar as the download runs.
		/// </summary>
		/// <param name="percentage">Percentage.</param>
		internal void UpdateProgress(float percentage)
		{
			progressView.SetProgress(percentage, true);
		}

		/// <summary>
		/// Updates the label on the download controller.
		/// </summary>
		/// <param name="text">Label Text.</param>
		void UpdateLabel(string text)
		{
			statusLabel.Text = text;
		}

		/// <summary>
		/// Gets called by the delegate and tells the controller to load the map controller
		/// </summary>
		internal void LoadMapView()
		{
			var navController = Storyboard.InstantiateViewController("NavController");

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
		/// Overrides the behavior of the controller when view has finished loading. 
		/// </summary>
		public override async void ViewDidLoad()
		{
			base.ViewDidLoad();

			// List of all files inside the Documents directory on the device
			List<string> files = Directory.EnumerateFiles(targetPath).ToList();

			// Test network connection. If it's available, check for new version of mmpk, then load map vieww
			if (Reachability.IsNetworkAvailable() == true)
			{
				statusLabel.Text = "Checking for Map Package Updates ...";
				progressView.Hidden = false;
				RetryButton.Hidden = true;

				// Setup the NSUrlSession.
				InitializeNSUrlSession();

				// Get item from Portal
				try
				{
					var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);
					var item = await PortalItem.CreateAsync(portal, AppSettings.currentSettings.ItemID).ConfigureAwait(false);


					// Check to see if the item has been updated since the last download
					// If so, just return the existing mmpk
					if (!files.Contains(targetFilename) ||
						item.Modified.LocalDateTime > AppSettings.currentSettings.MmpkDate)
					{
						// Otherwise, download the new mmpk
						InvokeOnMainThread(() => UpdateLabel("Downloading Mobile Map Package ..."));
						var downloadUrl = item.Url.AbsoluteUri.ToString() + "/data";
						EnqueueDownload(downloadUrl);

						AppSettings.currentSettings.MmpkDate = DateTime.Now;
					}
					// If no updates, just load the MapView
					else
					{
						InvokeOnMainThread(() => LoadMapView());
					}
				}
				catch
				{
					// If unable to get item from Portal, use already existing map package, unless this is the initial application download. 
					if (!files.Contains(targetFilename))
					{
						BeginInvokeOnMainThread(() =>
						{
							LoadOfflineMessage();
						});
					}
					else
						InvokeOnMainThread(() => LoadMapView());

				}
			}
			// If no network, check if mmpk has been donloaded and open it
			else if (files.Contains(targetFilename))
			{
				LoadMapView();
			}
			// If no connection and no mmpk downloaded, alert the user that they need to relaunch the app when in network
			else
			{
				LoadOfflineMessage();
			}
		}

		/// <summary>
		/// Displays message and turns proper controls on/off when device is offline and data has not been downloaded
		/// </summary>
		void LoadOfflineMessage()
		{
			statusLabel.Text = "Device does not seem to be connected to the network and the necessary data has not been downloaded. Please retry when in network range";
			progressView.Hidden = true;
			RetryButton.Hidden = false;
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
		/// <param name="downloadUrl">Download URL.</param>
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