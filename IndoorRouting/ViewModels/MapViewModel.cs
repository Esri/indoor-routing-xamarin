// <copyright file="MapViewModel.cs" company="Esri, Inc">
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
namespace IndoorRouting
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Mapping;

    /// <summary>
    /// Map view model handles all business logic to do with the map navigation and layers
    /// </summary>
    internal class MapViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The default home location text.
        /// </summary>
        public const string DefaultHomeLocationText = "Set home location";

        /// <summary>
        /// The default floor level.
        /// </summary>
        public const string DefaultFloorLevel = "1";

        /// <summary>
        /// The map used in the application.
        /// </summary>
        private Map map;

        /// <summary>
        /// The viewpoint of the map.
        /// </summary>
        private Viewpoint viewpoint;

        /// <summary>
        /// The selected floor level.
        /// </summary>
        private string selectedFloorLevel;

        /// <summary>
        /// Event handler property changed. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the map.
        /// </summary>
        /// <value>The map.</value>
        public Map Map
        {
            get
            {
                return this.map;
            }

            set
            {
                if (this.map != value && value != null)
                {
                    this.map = value;
                    this.OnPropertyChanged(nameof(this.Map));
                }
            }
        }

        /// <summary>
        /// Gets or sets the viewpoint.
        /// </summary>
        /// <value>The viewpoint.</value>
        public Viewpoint Viewpoint
        {
            get
            {
                return this.viewpoint;
            }

            set
            {
                if (this.viewpoint != value && value != null)
                {
                    this.viewpoint = value;
                    this.OnPropertyChanged(nameof(this.Viewpoint));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected floor level.
        /// </summary>
        /// <value>The selected floor level.</value>
        public string SelectedFloorLevel
        {
            get
            {
                return this.selectedFloorLevel;
            }

            set
            {
                if (this.selectedFloorLevel != value && value != null)
                {
                    this.SetFloorVisibility(true);
                    this.selectedFloorLevel = value;
                    this.OnPropertyChanged(nameof(this.SelectedFloorLevel));
                }
            }
        }

        /// <summary>
        /// Loads the mobile map package and the map 
        /// </summary>
        /// <returns>Async task</returns>
        internal async Task InitializeAsync()
        {
            // Get Mobile Map Package from the location on device
            var mmpk = await this.LoadMMPKAsync().ConfigureAwait(false);

            // Display map from the mmpk. Assumption is made that the first map of the mmpk is the one used
            this.Map = mmpk.Maps.FirstOrDefault();

            // Sets a basemap from ArcGIS Online if specified
            // Replace basemap with any online basemap 
            if (AppSettings.CurrentSettings.UseOnlineBasemap)
            {
                var basemap = Basemap.CreateLightGrayCanvasVector();
                this.Map.Basemap = basemap;
            }

            // Load map
            await Map.LoadAsync().ConfigureAwait(false);

            // Get the locator to be used in the app
            var locator = mmpk.LocatorTask;
            await locator.LoadAsync().ConfigureAwait(false);

            // Create instance of the Location View Model
            if (LocationViewModel.Instance == null)
            {
                LocationViewModel.Instance = LocationViewModel.Create(Map, locator);
            }

            // Set viewpoint of the map depending on user's setting
            await this.SetInitialViewPointAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the initial view point based on user settings. 
        /// </summary>
        /// <returns>Async task</returns>
        internal async Task SetInitialViewPointAsync()
        {
            // Get initial viewpoint from settings
            // If error occurs, do not set an initial viewpoint
            double x = 0, y = 0, wkid = 0, zoomLevel = 0;

            try
            {
                for (int i = 0; i < AppSettings.CurrentSettings.InitialViewpointCoordinates.Length; i++)
                {
                    switch (AppSettings.CurrentSettings.InitialViewpointCoordinates[i].Key)
                    {
                        case "X":
                            x = AppSettings.CurrentSettings.InitialViewpointCoordinates[i].Value;
                            break;
                        case "Y":
                            y = AppSettings.CurrentSettings.InitialViewpointCoordinates[i].Value;
                            break;
                        case "WKID":
                            wkid = AppSettings.CurrentSettings.InitialViewpointCoordinates[i].Value;
                            break;
                        case "ZoomLevel":
                            zoomLevel = AppSettings.CurrentSettings.InitialViewpointCoordinates[i].Value;
                            break;
                    }
                }

                // Location based, location services are on
                // Home settings, location services are off but user has a home set
                // Default setting, Location services are off and user has no home set
                if (!AppSettings.CurrentSettings.IsLocationServicesEnabled)
                {
                    Viewpoint = new Viewpoint(new MapPoint(x, y, new SpatialReference(Convert.ToInt32(wkid))), zoomLevel);
                }
            }
            catch
            {
                // Supress all errors since. 
                // If initial viewpoint cannot be set, the map will just load to the default extent of the mmpk
            }
            finally
            {
                // Set minimum and maximum scale for the map
                Map.MaxScale = AppSettings.CurrentSettings.MapViewMinScale;
                Map.MinScale = AppSettings.CurrentSettings.MapViewMaxScale;
            }
        }

        /// <summary>
        /// Moves map to home location.
        /// </summary>
        /// <returns>The viewpoint with coordinates for the home location.</returns>
        internal MapPoint MoveToHomeLocation()
        {
            this.SelectedFloorLevel = AppSettings.CurrentSettings.HomeFloorLevel;

            double x = 0, y = 0, wkid = 0;

            try
            {
                for (int i = 0; i < AppSettings.CurrentSettings.HomeCoordinates.Length; i++)
                {
                    switch (AppSettings.CurrentSettings.HomeCoordinates[i].Key)
                    {
                        case "X":
                            x = AppSettings.CurrentSettings.HomeCoordinates[i].Value;
                            break;
                        case "Y":
                            y = AppSettings.CurrentSettings.HomeCoordinates[i].Value;
                            break;
                        case "WKID":
                            wkid = AppSettings.CurrentSettings.HomeCoordinates[i].Value;
                            break;
                        default:
                            break;
                    }
                }

                Viewpoint = new Viewpoint(new MapPoint(x, y, new SpatialReference((int)wkid)), 150);
                return new MapPoint(x, y);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Changes the visibility of the rooms and walls layers based on floor selected
        /// TODO: Modify this if any other layer's visibility is desired to be controlled
        /// </summary>
        /// <param name="areLayersOn">If set to <c>true</c> operational layers are turned on</param>
        internal void SetFloorVisibility(bool areLayersOn)
        {
            foreach (var opLayer in this.Map.OperationalLayers.OfType<FeatureLayer>())
            {
                    var featureLayer = opLayer as FeatureLayer;

                    if (this.SelectedFloorLevel == string.Empty)
                    {
                        this.SelectedFloorLevel = DefaultFloorLevel;
                    }

                    // select chosen floor
                    featureLayer.DefinitionExpression = string.Format(
                        "{0} = '{1}'",
                        AppSettings.CurrentSettings.RoomsLayerFloorColumnName,
                    this.SelectedFloorLevel);

                    opLayer.IsVisible = areLayersOn;
            }
        }

        /// <summary>
        /// Called when a property changes to trigger PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    
        /// <summary>
        /// Loads the MMPK from the location on disk
        /// </summary>
        /// <returns>The MMPKA sync.</returns>
        private async Task<MobileMapPackage> LoadMMPKAsync()
        {
            try
            {
                var mmpk = await MobileMapPackage.OpenAsync(Path.Combine(DownloadViewModel.GetDataFolder(), AppSettings.CurrentSettings.PortalItemName));
                return mmpk;
            }
            catch
            {
                return null;
            }
        }
    }
}
