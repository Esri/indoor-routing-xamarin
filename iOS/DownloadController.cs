using Foundation;
using System;
using UIKit;
using Esri.ArcGISRuntime.Portal;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace IndoorNavigation.iOS
{
     public partial class DownloadController : UIViewController
	{
		public DownloadController(IntPtr handle) : base(handle)
		{
		}

		/// <summary>
		/// This is where the MMPK will be saved
		/// </summary>
		public static string targetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		public static string targetFilename = Path.Combine(targetPath, "EsriCampus.mmpk");

		/// <summary>
		/// Every session needs a unique identifier.
		/// </summary>
		const string sessionId = "com.esri.transfersession";

		/// <summary>
		/// Our session used for transfer.
		/// </summary>
		public NSUrlSession session;

		/// <summary>
		/// Gets called by the delegate and will update the progress bar as the download runs.
		/// </summary>
		/// <param name="percentage">Percentage.</param>
		public void UpdateProgress(float percentage)
		{
			this.progressView.SetProgress(percentage, true);
		}

		public void UpdateLabel(string text)
		{
			statusLabel.Text = text;
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
		}

		/// <summary>
		/// Gets called by the delegate and tells the controller to load the map controller
		/// </summary>
		public void LoadMapView()
		{
			var navController = Storyboard.InstantiateViewController("NavController");
			try
			{
				UIApplication.SharedApplication.KeyWindow.RootViewController = navController;
			}
			catch (NullReferenceException)
			{
				UIApplication.SharedApplication.Windows[0].RootViewController = navController;
			}
		}


		public override async void ViewDidLoad()
		{
			base.ViewDidLoad();

			List<string> files = Directory.EnumerateFiles(targetPath).ToList();

			// Test network connection. If it's available, check for new version of mmpk, then load map vieww
			if (Reachability.IsNetworkAvailable() == true)
			{
				statusLabel.Text = "Checking for Map Package Updates ...";
				progressView.Hidden = false;
				RetryButton.Hidden = true;
				// Setup the NSUrlSession.
				InitializeSession();

				// Get item from Portal
				try
				{
					var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);
					var item = await PortalItem.CreateAsync(portal, GlobalSettings.currentSettings.ItemID).ConfigureAwait(false);


					// Check to see if the item has been updated since the last download
					// If so, just return the existing mmpk


					if (!files.Contains(targetFilename) ||
						item.Modified.LocalDateTime > GlobalSettings.currentSettings.MmpkDate)
					{
						// Otherwise, download the new mmpk
						InvokeOnMainThread(() => UpdateLabel("Downloading Mobile Map Package ..."));
						string downloadUrl = item.Url.AbsoluteUri.ToString() + "/data";
						EnqueueDownload(downloadUrl);

						GlobalSettings.currentSettings.MmpkDate = DateTime.Now;
					}
					else
					{
						InvokeOnMainThread(() => LoadMapView());
					}
				}
				catch (Exception ex)
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
			// If no network, check if mmpk has been donloaded and open itt
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

		void LoadOfflineMessage()
		{
			statusLabel.Text = "Device does not seem to be connected to the network and the necessary data has not been downloaded. Please retry when in network range";
			progressView.Hidden = true;
			RetryButton.Hidden = false;
		}

		// Reload the view 
		partial void RetryButton_TouchUpInside(UIButton sender)
		{
			ViewDidLoad();
		}

		/// <summary>
		/// Initializes the session.
		/// </summary>
		void InitializeSession()
		{
			// Initialize our session config. We use a background session to enabled out of process uploads/downloads.
			using (var sessionConfig = UIDevice.CurrentDevice.CheckSystemVersion(8, 0)
				? NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(sessionId)
				: NSUrlSessionConfiguration.BackgroundSessionConfiguration(sessionId))
			{
				// Allow downloads over cellular network too.
				sessionConfig.AllowsCellularAccess = true;

				// Give the OS a hint about what we are downloading. This helps iOS to prioritize. For example "Background" is used to download data that was not requested by the user and
				// should be ready if the app gets activated.
				sessionConfig.NetworkServiceType = NSUrlRequestNetworkServiceType.Default;

				// Configure how many downloads we allow at the same time. Set to 1 since we only meed to download one file
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
		void EnqueueDownload(string downloadUrl)
		{
			// Create a new download task.
			var downloadTask = session.CreateDownloadTask(NSUrl.FromString(downloadUrl));

			// Alert user if download fails
			if (downloadTask == null)
			{
				//new UIAlertView(string.Empty, "Failed to create download task! Please retry.", null, "OK").Show();
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