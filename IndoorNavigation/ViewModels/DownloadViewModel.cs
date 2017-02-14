// <copyright file="DownloadViewModel.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation
{ 
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Portal;

    /// <summary>
    /// View model to handle common download logic between platforms
    /// </summary>
    internal class DownloadViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets set to true when the mmpk is downloading.
        /// </summary>
        private bool isDownloading;

        /// <summary>
        /// Gets set to true when the map is ready to be loaded
        /// </summary>
        private bool isReady;

        /// <summary>
        /// The download URL.
        /// </summary>
        private string downloadURL;

        /// <summary>
        /// Event handler property changed. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the name and path of the MMPK file.
        /// </summary>
        public string TargetFileName { get; private set; }

        /// <summary>
        /// Gets the list of type="files inside the download folder"> > 
        /// </summary>
        public List<string> Files { get; private set; }

        /// <summary>
        /// Gets or sets the map package status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets the download URL for the mmpk.
        /// </summary>
        public string DownloadURL
        {
            get
            {
                return this.downloadURL;
            }

            private set
            {
                if (this.downloadURL != value)
                {
                    this.downloadURL = value;
                    this.OnPropertyChanged(nameof(this.DownloadURL));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:IndoorNavigation.DownloadViewModel"/> is downloading.
        /// </summary>
        /// <value><c>true</c> if is downloading; otherwise, <c>false</c>.</value>
        public bool IsDownloading
        {
            get
            {
                return this.isDownloading;
            }

            private set
            {

                this.isDownloading = value;
                this.OnPropertyChanged(nameof(this.IsDownloading));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:IndoorNavigation.DownloadViewModel"/> is ready.
        /// </summary>
        /// <value><c>true</c> if is ready; otherwise, <c>false</c>.</value>
        public bool IsReady
        {
            get
            {
                return this.isReady;
            }

            private set
            {
                if (this.isReady != value)
                {
                    this.isReady = value;
                    this.OnPropertyChanged(nameof(this.IsReady));
                }
            }
        }

        /// <summary>
        /// Gets the data for the map. It downloads the mmpk if it doesn't exist or if there's a newer one available
        /// </summary>
        /// <returns>The map data.</returns>
        public async Task GetDataAsync()
        {
            // List of all files inside the Documents directory on the device
            this.Files = Directory.EnumerateFiles(GetDataFolder()).ToList();
            this.TargetFileName = Path.Combine(GetDataFolder(), AppSettings.CurrentSettings.PortalItemName);

            // Test if device is online
            // If offline, test if mmpk exists and load it
            // If offline and no mmpk, show error
            // Show error message if unable to downoad the mmpk. This is usually when the device is online but signal isn't strong enough and connection to Portal times out
            if (IsDeviceConnected())
            {
                try
                {
                    // Get portal item
                    var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);
                    var item = await PortalItem.CreateAsync(portal, AppSettings.CurrentSettings.PortalItemID).ConfigureAwait(false);

                    // Test if mmpk is not already downloaded or is older than current portal version
                    if (!this.Files.Contains(this.TargetFileName) ||
                        item.Modified.LocalDateTime > AppSettings.CurrentSettings.MmpkDownloadDate)
                    {
                        this.IsDownloading = true;
                        this.DownloadURL = item.Url.AbsoluteUri + "/data";
                    }
                    else
                    {
                        this.IsReady = true;
                    }
                }
                catch (Exception ex)
                {
                    // If unable to get item from Portal, use already existing map package, unless this is the initial application download. 
                    if (this.Files.Contains(this.TargetFileName))
                    {
                        this.IsReady = true;
                    }
                    else
                    {
                        this.Status = ex.Message;
                        this.IsDownloading = false;
                    }
                }
            }
            else if (this.Files.Contains(this.TargetFileName))
            {
                this.IsReady = true;
            }
            else
            {
                this.IsDownloading = false;
                this.Status = "Device does not seem to be connected to the network and the necessary data has not been downloaded. Please retry when in network range";
            }
        }

        /// <summary>
        /// Gets the data folder where the mmpk and settings file are stored.
        /// </summary>
        /// <returns>The data folder.</returns>
        internal static string GetDataFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Determines whether the device is connected or not.
        /// </summary>
        /// <returns><c>true</c>, if device connected was ised, <c>false</c> otherwise.</returns>
        internal static bool IsDeviceConnected()
        {
            return Reachability.IsNetworkAvailable();
        }

        /// <summary>
        /// Called when a property changes to trigger PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
