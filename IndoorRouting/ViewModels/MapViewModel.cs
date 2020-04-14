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
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;

    /// <summary>
    /// Map view model handles all business logic to do with the map navigation and layers
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
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
        private string _selectedFloorLevel;

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
        public Viewpoint CurrentViewpoint
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
                    this.OnPropertyChanged(nameof(this.CurrentViewpoint));
                    UpdateFloors();
                }
            }
        }


        private MapPoint _currentUserLocation;
        // TODO - view needs to update this
        public MapPoint CurrentUserLocation
        {
            get => _currentUserLocation;

            set
            {
                if (value != _currentUserLocation)
                {
                    value = _currentUserLocation;
                    OnPropertyChanged();
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
                return this._selectedFloorLevel;
            }

            set
            {
                if (this._selectedFloorLevel != value)
                {
                    _selectedFloorLevel = value;

                    if (_selectedFloorLevel != null)
                    {
                        _selectedFloorLevel = value;
                    }
                    else
                    {
                        _selectedFloorLevel = DefaultFloorLevel;
                    }
                    this.OnPropertyChanged(nameof(this.SelectedFloorLevel));
                    this.SetFloorVisibility(true);
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
            this.Map = mmpk.Maps.First();

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
            Locator = mmpk.LocatorTask;
            await Locator.LoadAsync().ConfigureAwait(false);

            // Set viewpoint of the map depending on user's setting
            SetInitialViewpoint();
        }

        /// <summary>
        /// Sets the initial view point based on user settings. 
        /// </summary>
        /// <returns>Async task</returns>
        internal void SetInitialViewpoint()
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
                    CurrentViewpoint = new Viewpoint(new MapPoint(x, y, new SpatialReference(Convert.ToInt32(wkid))), zoomLevel);
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
        public void MoveToHomeLocation()
        {
            if (string.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation))
            {
                return;
            }
            else
            {
                double x = 0, y = 0, wkid = 0;

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

                var homeLocation = new MapPoint(x, y, new SpatialReference((int)wkid));

                this.SelectedFloorLevel = AppSettings.CurrentSettings.HomeFloorLevel;
                CurrentlyIdentifiedRoom = new IdentifiedRoom { IsHome = true, Geometry = homeLocation, RoomNumber = AppSettings.CurrentSettings.HomeLocation };

                CurrentRoute = null;

                CurrentState = UIState.LocationFound;
            }
        }

        /// <summary>
        /// Keep floors in an observable collection.
        /// Currently visible floors are updated when viewpoint changes.
        /// </summary>
        public ObservableCollection<string> CurrentVisibleFloors { get; set; } = new ObservableCollection<string>();

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

                if (string.IsNullOrEmpty(SelectedFloorLevel))
                {
                    SelectedFloorLevel = DefaultFloorLevel;
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

        /// <summary>
        /// Gets the floors in visible area.
        /// </summary>
        /// <returns>The floors in visible area.</returns>
        /// <param name="mapView">Map view.</param>
        private async Task UpdateFloors()
        {
            // Run query to get all the polygons in the visible area
            var roomsLayer = Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;

            if (roomsLayer != null)
            {
                try
                {
                    var roomsTable = roomsLayer.FeatureTable;

                    // Set query parameters
                    var queryParams = new QueryParameters()
                    {
                        ReturnGeometry = false,
                        Geometry = CurrentViewpoint.TargetGeometry
                    };

                    // Query the feature table 
                    var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);

                    CurrentVisibleFloors.Clear();

                    if (queryResult != null)
                    {
                        // Group by floors to get the distinct list of floors in the table selection
                        var distinctFloors = queryResult.GroupBy(g => g.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName])
                                                        .Select(gr => gr.First().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString())
                                                        .OrderBy(f => f);
                        // Add each floor to the table
                        foreach (var item in distinctFloors)
                        {
                            CurrentVisibleFloors.Add(item);
                        }
                    }
                }
                catch
                {
                    CurrentVisibleFloors.Clear();
                }
            }
            else
            {
                CurrentVisibleFloors.Clear();
            }
        }

        /// <summary>
        /// Gets or sets the mmpk.
        /// </summary>
        /// <value>The mmpk.</value>
        public LocatorTask Locator { get; set; }

        /// <summary>
        /// Gets the location suggestions from the mmpk.
        /// </summary>
        /// <returns>List of location suggestions.</returns>
        /// <param name="userInput">User input.</param>
        internal async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestionsAsync(string userInput)
        {
            try
            {
                var locatorInfo = this.Locator.LocatorInfo;

                if (locatorInfo.SupportsSuggestions)
                {
                    // restrict the search to return no more than 10 suggestions
                    var suggestParams = new SuggestParameters { MaxResults = 10 };

                    // get suggestions for the text provided by the user
                    var suggestions = await this.Locator.SuggestAsync(userInput, suggestParams);
                    return suggestions;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Gets the features from query
        /// </summary>
        /// <returns>The features </returns>
        /// <param name="searchString">Search string.</param>
        internal async Task<string> GetFloorLevelFromQueryAsync(string searchString)
        {
            // Run query to get the floor of the selected room
            var roomsLayer = this.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;

            if (roomsLayer != null)
            {
                try
                {
                    var roomsTable = roomsLayer.FeatureTable;

                    // Fix the search string if it contains a '
                    var formattedSearchString = this.FormatStringForQuery(searchString);

                    // Set query parametersin 
                    var queryParams = new QueryParameters()
                    {
                        ReturnGeometry = true,
                        WhereClause = string.Format(string.Join(" = '{0}' OR ", AppSettings.CurrentSettings.LocatorFields) + " = '{0}'", formattedSearchString)
                    };

                    // Query the feature table 
                    var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);

                    if (queryResult != null)
                    {
                        var floorResult = queryResult.FirstOrDefault().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString();
                        return floorResult;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the searched location based on search terms user entered.
        /// </summary>
        /// <returns>The searched location.</returns>
        /// <param name="searchString">User input.</param>
        internal async Task<GeocodeResult> GetSearchedLocationAsync(string searchString)
        {
            try
            {
                var formattedSearchString = this.FormatStringForQuery(searchString);

                // Geocode location and return the best match from the list
                var matches = await this.Locator.GeocodeAsync(formattedSearchString);
                var bestMatch = matches.FirstOrDefault();
                return bestMatch;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the room feature async.
        /// </summary>
        /// <returns>The room feature.</returns>
        /// <param name="searchString">Search string for the query.</param>
        internal async Task<Feature> GetRoomFeatureAsync(string searchString)
        {
            // Run query to get the floor of the selected room
            var roomsLayer = this.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;

            if (roomsLayer != null)
            {
                var roomsTable = roomsLayer.FeatureTable;

                var formattedSearchString = this.FormatStringForQuery(searchString);

                // Set query parametersin 
                var queryParams = new QueryParameters()
                {
                    ReturnGeometry = true,
                    WhereClause = string.Format(string.Join(" = '{0}' OR ", AppSettings.CurrentSettings.LocatorFields) + " = '{0}'", formattedSearchString)
                };

                // Query the feature table 
                try
                {
                    var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);
                    return queryResult.FirstOrDefault();
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the requested route based on start and end location points.
        /// </summary>
        /// <returns>The requested route.</returns>
        /// <param name="fromLocation">From location.</param>
        /// <param name="toLocation">To location.</param>
        internal async Task<RouteResult> GetRequestedRouteAsync(MapPoint fromLocation, MapPoint toLocation)
        {
            if (this.Map.LoadStatus != LoadStatus.Loaded)
            {
                try
                {
                    await this.Map.LoadAsync();
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                var routeTask = await RouteTask.CreateAsync(this.Map.TransportationNetworks[0]);

                if (routeTask != null)
                {
                    // Get the default route parameters
                    var routeParams = await routeTask.CreateDefaultParametersAsync();

                    // Explicitly set values for some params
                    // Indoor networks do not support turn by turn navigation
                    routeParams.ReturnRoutes = true;
                    routeParams.ReturnDirections = true;

                    // Create stops
                    var startPoint = new Stop(fromLocation);
                    var endPoint = new Stop(toLocation);

                    // assign the stops to the route parameters
                    routeParams.SetStops(new List<Stop> { startPoint, endPoint });

                    // Execute routing
                    var routeResult = await routeTask.SolveRouteAsync(routeParams);

                    return routeResult;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Formats the string for query.
        /// </summary>
        /// <returns>The formatted string.</returns>
        /// <param name="searchString">String to be formatted.</param>
        private string FormatStringForQuery(string searchString)
        {
            if (searchString.Contains("'"))
            {
                var newSearchString = searchString.Replace("'", "''");
                return newSearchString;
            }
            else
            {
                return searchString;
            }
        }

        public enum UIState
        {
            ReadyWaiting, //-> SearchInProgress
            SearchingForDestination,
            SearchingForOrigin,
            SearchingForFeature,
            DestinationFound,
            OriginFound,
            FeatureSearchEntered,
            PlanningRoute, //-> RouteFound, RouteNotFound
            LocationFound, //-> PlanningRoute, AwaitingSearch
            RouteFound, //-> AwaitingSearch, LocationFound
            RouteNotFound, //-> AwaitingSearch
            LocationNotFound, //-> AwaitingSearch
        }

        private UIState _currentState = UIState.ReadyWaiting;

        public UIState CurrentState
        {
            get => _currentState;
            set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    OnPropertyChanged();
                }
            }
        }

        private IdentifiedRoom _currentlyIdentifiedRoom;

        public IdentifiedRoom CurrentlyIdentifiedRoom
        {
            get => _currentlyIdentifiedRoom;
            set
            {
                if (value != _currentlyIdentifiedRoom)
                {
                    _currentlyIdentifiedRoom = value;
                    OnPropertyChanged();
                }
            }
        }

        private RouteResult _route;

        public RouteResult CurrentRoute
        {
            get => _route;
            set
            {
                if (value != _route)
                {
                    _route = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _featureSearchText = string.Empty;
        private string _originSearchText = string.Empty;
        private string _destinationSearchText = string.Empty;

        public string FeatureSearchText
        {
            get => _featureSearchText;
            set
            {
                if (value != _featureSearchText)
                {
                    _featureSearchText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OriginSearchText
        {
            get => _originSearchText;
            set
            {
                if (value != _originSearchText)
                {
                    _originSearchText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DestinationSearchText
        {
            get => _destinationSearchText;
            set
            {
                if (value != _destinationSearchText)
                {
                    _destinationSearchText = value;
                    OnPropertyChanged();
                }
            }
        }

        //TODO restructure this further
        public async void PerformRouteSearch()
        {
            // Geocode the locations selected by the user
            try
            {
                if (OriginSearchText != "CurrentLocationLabel".AsLocalized()) // TODO get rid of localization stuff
                {
                    FromLocationFeature = await GetRoomFeatureAsync(OriginSearchText);
                    ToLocationFeature = await GetRoomFeatureAsync(DestinationSearchText);

                    var fromLocationPoint = FromLocationFeature.Geometry.Extent.GetCenter();
                    var toLocationPoint = ToLocationFeature.Geometry.Extent.GetCenter();

                    var route = await GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);

                    CurrentRoute = route;
                }
                else
                {
                    ToLocationFeature = await GetRoomFeatureAsync(DestinationSearchText);

                    var fromLocationPoint = CurrentUserLocation; // TODO - 
                    var toLocationPoint = ToLocationFeature.Geometry.Extent.GetCenter();

                    var route = await GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);

                    CurrentRoute = route;
                }

                if (CurrentRoute != null)
                {
                    CurrentState = UIState.RouteFound;
                }
                else
                {
                    CurrentState = UIState.RouteNotFound;
                }
            }
            catch
            {
                CurrentRoute = null;
                CurrentState = UIState.RouteNotFound;
            }
        }

        /// <summary>
        /// Gets or sets from location feature.
        /// </summary>
        /// <value>From location feature.</value>
        public Feature FromLocationFeature { get; set; }

        /// <summary>
        /// Gets or sets to location feature.
        /// </summary>
        /// <value>To locationfeature.</value>
        public Feature ToLocationFeature { get; set; }

        public void StartSearchFromFoundFeature()
        {
            if (CurrentlyIdentifiedRoom.IsHome)
            {
                OriginSearchText = string.Empty;
                DestinationSearchText = CurrentlyIdentifiedRoom.RoomNumber;
            }
            else
            {
                OriginSearchText = CurrentlyIdentifiedRoom.RoomNumber;
                DestinationSearchText = string.Empty;
            }

            CurrentState = UIState.PlanningRoute;
        }

        public void CloseLocationInfo()
        {
            CurrentlyIdentifiedRoom = null;
            FeatureSearchText = null;
            CurrentState = UIState.ReadyWaiting;
        }

        public void CloseRouteResult()
        {
            CurrentRoute = null;
            CurrentState = UIState.PlanningRoute;
        }

        public void ReturnToWaitingState() => CurrentState = UIState.ReadyWaiting;

        public void SelectOriginSearch() => CurrentState = UIState.SearchingForOrigin;

        public void SelectDestinationSearch() => CurrentState = UIState.SearchingForDestination;

        public void SwapOriginDestinationSearch()
        {
            string oldOriginSearch = OriginSearchText;
            string oldDestinationSearch = DestinationSearchText;

            DestinationSearchText = oldOriginSearch;
            OriginSearchText = oldDestinationSearch;
        }

        public void DismissNotFound()
        {
            if (CurrentState == UIState.LocationNotFound)
            {
                CurrentState = UIState.SearchingForFeature;
            }
            else if (CurrentState == UIState.RouteNotFound)
            {
                CurrentState = UIState.PlanningRoute;
            }
        }

        public void CancelRouteSearch()
        {
            OriginSearchText = null;
            DestinationSearchText = null;

            CurrentState = UIState.LocationFound;
        }

        public void StartEditingInLocationSearch()
        {
            if (CurrentState == UIState.ReadyWaiting)
            {
                CurrentState = UIState.SearchingForFeature;
            }
        }

        public void StopEditingInLocationSearch()
        {
            switch (CurrentState)
            {
                case UIState.SearchingForDestination:
                case UIState.SearchingForOrigin:
                    CurrentState = UIState.PlanningRoute;
                    break;
                case UIState.SearchingForFeature:
                    FeatureSearchText = null;
                    CurrentState = UIState.ReadyWaiting;
                    break;
            }
        }

        public async Task CommitSearchAsync(string text)
        {
            switch (CurrentState)
            {
                case UIState.SearchingForFeature:
                    FeatureSearchText = text;
                    CurrentState = UIState.FeatureSearchEntered;
                    await SearchFeatureAsync(FeatureSearchText);
                    break;
                case UIState.SearchingForOrigin:
                    OriginSearchText = text;
                    CurrentState = UIState.PlanningRoute;
                    break;
                case UIState.SearchingForDestination:
                    DestinationSearchText = text;
                    CurrentState = UIState.PlanningRoute;
                    break;
            }
        }

        public void IdentifyRoomFromLayerResult(IdentifyLayerResult result)
        {
            CurrentlyIdentifiedRoom = IdentifiedRoom.ConstructFromIdentifyResult(result);

            if (CurrentlyIdentifiedRoom != null)
            {
                CurrentState = UIState.LocationFound;
            }
        }

        public void MoveToCurrentLocation()
        {
            CurrentlyIdentifiedRoom = IdentifiedRoom.ConstructCurrentLocation();
            CurrentState = UIState.LocationFound;
            // TODO - set floor based on user position?
            SelectedFloorLevel = null;
            return;
        }

        public void SelectFloor(string floor)
        {
            SelectedFloorLevel = floor;
        }

        /// <summary>
        /// Zooms to geocode result of the searched feature
        /// </summary>
        /// <param name="searchText">Search text entered by user.</param>
        /// <returns>The searched feature</returns>
        public async Task SearchFeatureAsync(string searchText)
        {
            if (searchText == "Current Location")
            {
                MoveToCurrentLocation();
            }
            else
            {
                var geocodeResult = await GetSearchedLocationAsync(searchText);
                SelectedFloorLevel = await GetFloorLevelFromQueryAsync(searchText);

                if (geocodeResult != null)
                {
                    // Get the feature to populate the Contact Card
                    Feature roomFeature = await GetRoomFeatureAsync(searchText);

                    IdentifiedRoom room = IdentifiedRoom.ConstructFromFeature(roomFeature);

                    CurrentlyIdentifiedRoom = room;

                    if (roomFeature != null)
                    {
                        CurrentState = UIState.LocationFound;
                    }
                    else
                    {
                        CurrentState = UIState.LocationNotFound;
                    }
                }
                else
                {
                    CurrentState = UIState.LocationNotFound;
                }
            }
        }
    }
}
