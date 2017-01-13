using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Portal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;

namespace IndoorNavigation
{
	class DownloadViewModel: INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the name and path of the MMPK file.
		/// </summary>
		public string TargetFileName { get; private set;}

		/// <summary>
		/// list type="files inside the download folder"> > 
		/// </summary>
		public List<string> Files { get; private set; }

		private string _status;
		/// <summary>
		/// Map package status. This could be Downloading, Ready, or another status that represents error
		/// </summary>
		public string Status
		{
			get
			{
				return _status;
			}

			private set
			{
				if (_status != value && !string.IsNullOrEmpty(value))
				{
					_status = value;
					OnPropertyChanged(nameof(Status));
				}

			}
		}

		private string _downloadURL;
		/// <summary>
		/// Gets the download URL for the mmpk.
		/// </summary>
		public string DownloadURL
		{
			get
			{
				return _downloadURL;
			}

			private set
			{
				if (_downloadURL != value && !string.IsNullOrEmpty(value))
				{
					_downloadURL = value;
					OnPropertyChanged(nameof(DownloadURL));
				}
			}
		}


		/// <summary>
		/// Gets the data folder where the mmpk and settings file are stored.
		/// </summary>
		/// <returns>The data folder.</returns>
		internal static string GetDataFolder()
		{
#if __ANDROID__
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#elif __IOS__
				return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
		}

		/// <summary>
		/// Determines whether the device is connected or not.
		/// </summary>
		/// <returns><c>true</c>, if device connected was ised, <c>false</c> otherwise.</returns>
		internal static bool IsDeviceConnected()
		{
#if __ANDROID__
                //return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#elif __IOS__
			return Reachability.IsNetworkAvailable();
#endif
		}

		/// <summary>
		/// Gets the data for the map. It downloads the mmpk if it doesn't exist or if there's a newer one available
		/// </summary>
		public async Task GetDataAsync()
		{
			// List of all files inside the Documents directory on the device
			Files = Directory.EnumerateFiles(GetDataFolder()).ToList();
			TargetFileName = Path.Combine(GetDataFolder(), AppSettings.CurrentSettings.PortalItemName);

			// Test if device is online
			if (IsDeviceConnected())
			{
				try
				{
					// Get portal item
					var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);
					var item = await PortalItem.CreateAsync(portal, AppSettings.CurrentSettings.PortalItemID).ConfigureAwait(false);

					// Test if mmpk is not already downloaded or is older than current portal version
					if (!Files.Contains(TargetFileName) ||
					    item.Modified.LocalDateTime > AppSettings.CurrentSettings.MmpkDownloadDate)
					{
						Status = "Downloading";
						DownloadURL = item.Url.AbsoluteUri + "/data";
					}
					else
					{
						Status = "Ready";
					}
				}
				// Show error message if unable to downoad the mmpk. This is usually when the device is online but signal isn't strong enough and connection to Portal times out
				catch
				{
					// If unable to get item from Portal, use already existing map package, unless this is the initial application download. 
					if (Files.Contains(TargetFileName))
						Status = "Ready";
					else
						Status = "The application is online but is unable to connect to Portal and the necessary data has not been downloaded. This could mean there is not enough bandwidth. Please try again";		
				}

			}
			// If offline, test if mmpk exists and load it
			else if (Files.Contains(TargetFileName))
			{
				Status = "Ready";
			}
			// If offline and no mmpk, show error
			else
			{
				Status = "Device does not seem to be connected to the network and the necessary data has not been downloaded. Please retry when in network range";
			}
		}

		/// <summary>
		/// Event handler property changed. 
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Called when a property changes to trigger PropertyChanged event
		/// </summary>
		/// <param name="propertyName">Name of property that changed.</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}
