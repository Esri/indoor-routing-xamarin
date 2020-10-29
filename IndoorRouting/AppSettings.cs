// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// https://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// Class holding the settings for the application
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        // Backing fields for properties
        private string _portalItemId;
        private string _portalItemName;
        private DateTime _mmpkDownloadDate;
        private string _homeLocation;
        private bool _isLocationServicesEnabled;
        private bool _isRoutingEnabled;
        private bool _isPrefersElevatorsEnabled;
        private int _roomsLayerIndex;
        private int _floorPlanLinesLayerIndex;
        private string _homeFloorLevel;
        private int _mapViewMaxScale;
        private int _mapViewMinScale;
        private bool _useOnlineBasemap;
        private List<string> _contactCardDisplayFields;
        private List<string> _locatorFields;
        private string _roomsLayerFloorColumnName;
        private double _roomsLayerMinimumZoomLevel;
        private SerializableKeyValuePair<string, double>[] _initialViewpointCoordinates;
        private SerializableKeyValuePair<string, double>[] _homeCoordinates;
        private bool _showLocationNotFoundCard = false;

        /// <summary>
        /// Gets or sets the current settings.
        /// </summary>
        /// <value>Static instance of the settings for the application</value>
        public static AppSettings CurrentSettings { get; set; }

        /// <summary>
        /// 'Current Location' string needs to be consistent throughout the app for comparisons to be reliable.
        /// If localizing, set this string to the translated value at startup.
        /// </summary>
        public static string LocalizedCurrentLocationString { get; set; } = "Current Location";

        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>Portal Item ID</value>
        [XmlElement]
        public string PortalItemID
        {
            get => _portalItemId;
            set
            {
                if (value != _portalItemId)
                {
                    _portalItemId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PortalItemID)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the Portal item</value>
        [XmlElement]
        public string PortalItemName
        {
            get => _portalItemName;
            set
            {
                if (value != _portalItemName)
                {
                    _portalItemName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PortalItemName)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the mmpk date.
        /// </summary>
        /// <value>The date the mobile map package was downloaded</value>
        [XmlElement]
        public DateTime MmpkDownloadDate
        {
            get => _mmpkDownloadDate;
            set
            {
                if (value != _mmpkDownloadDate)
                {
                    _mmpkDownloadDate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MmpkDownloadDate)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the home location.
        /// </summary>
        /// <value>The home location set by the user. By default this is set to "Set home location"</value>
        [XmlElement]
        public string HomeLocation
        {
            get => _homeLocation;
            set
            {
                if(value != _homeLocation)
                {
                    _homeLocation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HomeLocation)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHomeSet)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:IndoorNavigation.AppSettings"/> is location services enabled.
        /// </summary>
        /// <value><c>true</c> if is location services switch enabled; otherwise, <c>false</c>.</value>
        [XmlElement]
        public bool IsLocationServicesEnabled
        {
            get => _isLocationServicesEnabled;
            set
            {
                if(value != _isLocationServicesEnabled)
                {
                    _isLocationServicesEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLocationServicesEnabled)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:IndoorNavigation.AppSettings"/> is routing enabled.
        /// </summary>
        /// <value><c>true</c> if is routing switch enabled; otherwise, <c>false</c>.</value>
        [XmlElement]
        public bool IsRoutingEnabled
        {
            get => _isRoutingEnabled;
            set
            {
                if (value != _isRoutingEnabled)
                {
                    _isRoutingEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRoutingEnabled)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:IndoorNavigation.AppSettings"/> is prefer elevators enabled.
        /// </summary>
        /// <value><c>true</c> if is prefer elevators switch is enabled; otherwise, <c>false</c>.</value>
        [XmlElement]
        public bool IsPreferElevatorsEnabled
        {
            get => _isPrefersElevatorsEnabled;
            set
            {
                if (value != _isPrefersElevatorsEnabled)
                {
                    _isPrefersElevatorsEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPreferElevatorsEnabled)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the rooms layer.
        /// </summary>
        /// <value>The index of the rooms layer.</value>
        [XmlElement]
        public int RoomsLayerIndex
        {
            get => _roomsLayerIndex;
            set
            {
                if (value != _roomsLayerIndex)
                {
                    _roomsLayerIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RoomsLayerIndex)));
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the index of the floor plan lines layer.
        /// </summary>
        /// <value>The index of the floor plan lines layer.</value>
        [XmlElement]
        public int FloorPlanLinesLayerIndex
        {
            get => _floorPlanLinesLayerIndex;
            set
            {
                if (value != _floorPlanLinesLayerIndex)
                {
                    _floorPlanLinesLayerIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FloorPlanLinesLayerIndex)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the zoom level to display room layers.
        /// </summary>
        /// <value>The zoom level to display room layers.</value>
        [XmlElement]
        public double RoomsLayerMinimumZoomLevel
        {
            get => _roomsLayerMinimumZoomLevel;
            set
            {
                if (value != _roomsLayerMinimumZoomLevel)
                {
                    _roomsLayerMinimumZoomLevel = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RoomsLayerMinimumZoomLevel)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the floor column in rooms table.
        /// </summary>
        /// <value>The floor column in rooms table.</value>
        [XmlElement]
        public string RoomsLayerFloorColumnName
        {
            get => _roomsLayerFloorColumnName;
            set
            {
                if(value != _roomsLayerFloorColumnName)
                {
                    _roomsLayerFloorColumnName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RoomsLayerFloorColumnName)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the home coordinates.
        /// </summary>
        /// <value>The coordinates and floor level for the home location. This also includes the WKID</value>
        [XmlArray("HomeCoordinates")]
        public SerializableKeyValuePair<string, double>[] HomeCoordinates
        {
            get => _homeCoordinates;
            set
            {
                if (value != _homeCoordinates)
                {
                    _homeCoordinates = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HomeCoordinates)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the home floor level.
        /// </summary>
        /// <value>The home floor level.</value>
        [XmlElement]
        public string HomeFloorLevel
        {
            get => _homeFloorLevel;
            set
            {
                if (_homeFloorLevel != value)
                {
                    _homeFloorLevel = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HomeFloorLevel)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the initial viewpoint coordinates.
        /// </summary>
        /// <value>The initial viewpoint coordinates used for the map.</value>
        [XmlArray("InitialViewpointCoordinates")]
        public SerializableKeyValuePair<string, double>[] InitialViewpointCoordinates
        {
            get => _initialViewpointCoordinates;
            set
            {
                if (value != _initialViewpointCoordinates)
                {
                    _initialViewpointCoordinates = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitialViewpointCoordinates)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the locator fields. If there is only one locator, make a list with one value 
        /// </summary>
        /// <value>The locator fields.</value>
        [XmlArray("LocatorFields")]
        public List<string> LocatorFields
        {
            get => _locatorFields;
            set
            {
                if (_locatorFields != value)
                {
                    _locatorFields = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocatorFields)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the contact card display fields. These are what is displayed on the Contact card when user searches or taps an office
        /// </summary>
        /// <value>The contact card display fields.</value>
        [XmlArray("ContactCardDisplayFields")]
        public List<string> ContactCardDisplayFields
        {
            get => _contactCardDisplayFields;
            set
            {
                if (_contactCardDisplayFields != value)
                {
                    _contactCardDisplayFields = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContactCardDisplayFields)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum scale of the map.
        /// </summary>
        /// <value>The minimum scale.</value>
        [XmlElement]
        public int MapViewMinScale
        {
            get => _mapViewMinScale;
            set
            {
                if (_mapViewMinScale != value)
                {
                    _mapViewMinScale = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MapViewMinScale)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum scale of the map.
        /// </summary>
        /// <value>The max scale.</value>
        [XmlElement]
        public int MapViewMaxScale
        {
            get => _mapViewMaxScale;
            set
            {
                if (_mapViewMaxScale != value)
                {
                    _mapViewMaxScale = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MapViewMaxScale)));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to supplement the MMPK basemap with an online basemap.
        /// </summary>
        [XmlElement]
        public bool UseOnlineBasemap
        {
            get => _useOnlineBasemap;
            set
            {
                if (_useOnlineBasemap != value)
                {
                    _useOnlineBasemap = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseOnlineBasemap)));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show a 'location not found' card when location isn't found.
        /// View transitions to default state automatically if this is false.
        /// </summary>
        [XmlElement]
        public bool ShowLocationNotFoundCard
        {
            get => _showLocationNotFoundCard;
            set
            {
                if (_showLocationNotFoundCard != value)
                {
                    _showLocationNotFoundCard = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowLocationNotFoundCard)));
                }
            }
        }

        /// <summary>
        /// Returns <value>true</value> if a home location has been set.
        /// </summary>
        public bool IsHomeSet => !string.IsNullOrWhiteSpace(HomeLocation);

        /// <summary>
        /// Loads the app settings if the file exists, otherwise it creates default settings. 
        /// </summary>
        /// <returns>The app settings.</returns>
        /// <param name="filePath">File path.</param>
        internal static async Task<AppSettings> CreateAsync(string filePath)
        {
            var directoryName = Path.GetDirectoryName(filePath);

            if (directoryName == null)
            {
                throw new DirectoryNotFoundException("Couldn't find settings directory");
            }
            // Get all the files in the device directory
            List<string> files = Directory.EnumerateFiles(directoryName).ToList();

            // If the settings file doesn't exist, create it
            // Otherwise load the settings from the settings file
            // If settings file is invalid, delete it and recreate it
            if (!files.Contains(filePath))
            {
                var appSettings = new AppSettings
                {
                    PortalItemID = "52346d5fc4c348589f976b6a279ec3e6",
                    PortalItemName = "RedlandsCampus.mmpk",
                    // Set the room and walls layers
                    RoomsLayerIndex = 1,
                    FloorPlanLinesLayerIndex = 2,
                    RoomsLayerFloorColumnName = "FLOOR",
                    UseOnlineBasemap = false,
                    // Set fields displayed in the bottom card
                    LocatorFields = new List<string> {"LONGNAME", "KNOWN_AS_N"},
                    ContactCardDisplayFields = new List<string> {"LONGNAME", "KNOWN_AS_N"},
                    // Change at what zoom levels the room data becomes visible
                    RoomsLayerMinimumZoomLevel = 750,
                    // Change map scale bounds
                    MapViewMinScale = 100,
                    MapViewMaxScale = 13000,
                    MmpkDownloadDate = new DateTime(1900, 1, 1),
                    HomeLocation = string.Empty,
                    IsLocationServicesEnabled = false,
                    IsRoutingEnabled = true,
                    IsPreferElevatorsEnabled = false,
                    InitialViewpointCoordinates = new[]
                    {
                        new SerializableKeyValuePair<string, double>("X", -13046209),
                        new SerializableKeyValuePair<string, double>("Y", 4036456),
                        new SerializableKeyValuePair<string, double>("WKID", 3857),
                        new SerializableKeyValuePair<string, double>("ZoomLevel", 13000),
                    }
                };

                var serializer = new XmlSerializer(appSettings.GetType());

                // Create settings file on a separate thread
                // this does not need to be awaited since the return is already set
                await Task.Factory.StartNew(delegate
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        serializer.Serialize(fileStream, appSettings);
                    }
                });
                return appSettings;
            }
            else
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var appSettings = new AppSettings();
                    try
                    {
                        var serializer = new XmlSerializer(appSettings.GetType());
                        return serializer.Deserialize(fileStream) as AppSettings;
                    }
                    catch (System.Xml.XmlException)
                    {
                        File.Delete(filePath);
                        return await AppSettings.CreateAsync(filePath).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the settings. If error occurs settings are not saved but application does not stop
        /// </summary>
        /// <param name="filePath">File path.</param>
        internal static void SaveSettings(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppSettings));

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fileStream, CurrentSettings);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Event notifies subscribers of property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
