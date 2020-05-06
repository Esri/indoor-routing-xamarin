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

using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Portal;

    /// <summary>
    /// View model to handle common download logic between platforms
    /// </summary>
    public class DownloadViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets set to true when the mmpk is downloading.
        /// </summary>
        private bool _isDownloading;

        /// <summary>
        /// Gets set to true when the map is ready to be loaded
        /// </summary>
        private bool _isReady;

        /// <summary>
        /// The download URL.
        /// </summary>
        private string _downloadUrl;

        /// <summary>
        /// Event handler property changed. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the full path to the MMPK file, including file name.
        /// </summary>
        public static string TargetFileName => Path.Combine(GetDataFolder(), AppSettings.CurrentSettings.PortalItemName);

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
        public string DownloadUrl
        {
            get => _downloadUrl;

            private set
            {
                if (_downloadUrl != value)
                {
                    _downloadUrl = value;
                    OnPropertyChanged(nameof(DownloadUrl));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:IndoorNavigation.DownloadViewModel"/> is downloading.
        /// </summary>
        /// <value><c>true</c> if is downloading; otherwise, <c>false</c>.</value>
        public bool IsDownloading
        {
            get => _isDownloading;

            set
            {
                if (_isDownloading != value)
                {
                    _isDownloading = value;
                    OnPropertyChanged(nameof(IsDownloading));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:IndoorNavigation.DownloadViewModel"/> is ready.
        /// </summary>
        /// <value><c>true</c> if is ready; otherwise, <c>false</c>.</value>
        public bool IsReady
        {
            get => _isReady;

            private set
            {
                if (_isReady != value)
                {
                    _isReady = value;
                    OnPropertyChanged(nameof(IsReady));
                }
            }
        }

        /// <summary>
        /// Determines whether the device is connected or not.
        /// </summary>
        /// <returns><c>true</c>, if device connected was used, <c>false</c> otherwise.</returns>
        public static bool IsDeviceConnected() => Reachability.IsNetworkAvailable();

        /// <summary>
        /// Gets the data folder where the mmpk and settings file are stored.
        /// </summary>
        /// <returns>The data folder.</returns>
        public static string GetDataFolder() => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// Gets the data for the map. It downloads the mmpk if it doesn't exist or if there's a newer one available
        /// </summary>
        /// <returns>The map data.</returns>
        public async Task GetDataAsync()
        {
            // List of all files inside the Documents directory on the device
            Files = Directory.EnumerateFiles(GetDataFolder()).ToList();

            // Test if device is online
            // If offline, test if mmpk exists and load it
            // If offline and no mmpk, show error
            // Show error message if unable to download the mmpk. This is usually when the device is online but signal isn't strong enough and connection to Portal times out
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
                        IsDownloading = true;
                        var data = await item.GetDataAsync();

                        using (var dataStream = await item.GetDataAsync())
                        {
                            using (var fileStream = File.Create(Path.Combine(GetDataFolder(), TargetFileName)))
                            {
                                await dataStream.CopyToAsync(fileStream);
                            }
                        }
                        IsDownloading = false;
                        IsReady = true;
                    }
                    else
                    {
                        IsReady = true;
                    }
                }
                catch (Exception ex)
                {
                    // If unable to get item from Portal, use already existing map package, unless this is the initial application download. 
                    if (Files.Contains(TargetFileName))
                    {
                        IsReady = true;
                    }
                    else
                    {
                        Status = ex.Message;
                        IsDownloading = false;
                    }
                }
            }
            else if (Files.Contains(TargetFileName))
            {
                IsReady = true;
            }
            else
            {
                IsDownloading = false;
                Status = "Device does not seem to be connected to the network and the necessary data has not been downloaded. Please retry when in network range";
            }
        }

        /// <summary>
        /// Called when a property changes to trigger PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
