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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    /// <summary>
    /// Map view model handles all business logic to do with the map navigation and layers
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The default floor level.
        /// </summary>
        public const string DefaultFloorLevel = "1";

        #region Backing fields
        /// <summary>
        /// The map used in the application.
        /// </summary>
        private Map _map;

        /// <summary>
        /// The viewpoint of the map.
        /// </summary>
        private Envelope _currentVisibleArea;

        /// <summary>
        /// The selected floor level.
        /// </summary>
        private string _selectedFloorLevel;

        private IEnumerable<string> _currentVisibleFloors;

        private string _featureSearchText = string.Empty;
        private string _originSearchText = string.Empty;
        private string _destinationSearchText = string.Empty;

        private Room _currentRoom;
        private RouteResult _route;
        private UiState _currentState = UiState.ReadyWaiting;
        private MapPoint _currentUserLocation;

        #endregion Backing fields

        #region Properties

        /// <summary>
        /// Gets or sets the map.
        /// </summary>
        /// <value>The map.</value>
        public Map Map
        {
            get => _map;
            private set
            {
                if (_map != value)
                {
                    _map = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the viewpoint.
        /// </summary>
        /// <value>The viewpoint.</value>
        public Envelope CurrentViewArea
        {
            get => _currentVisibleArea;

            set
            {
                if (_currentVisibleArea != value)
                {
                    _currentVisibleArea = value;
                    OnPropertyChanged();
                    UpdateFloors();
                }
            }
        }

        public MapPoint CurrentUserLocation
        {
            get => _currentUserLocation;

            set
            {
                if (value != _currentUserLocation)
                {
                    _currentUserLocation = value;
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
            get => _selectedFloorLevel;
            private set
            {
                if (_selectedFloorLevel != value)
                {
                    _selectedFloorLevel = value ?? DefaultFloorLevel;

                    OnPropertyChanged();
                    SetFloorVisibility(true);
                }
            }
        }

        /// <summary>
        /// Keep floors in an observable collection.
        /// Currently visible floors are updated when viewpoint changes.
        /// </summary>
        public IEnumerable<string> CurrentVisibleFloors
        {
            get => _currentVisibleFloors;
            private set
            {
                if (!Equals(_currentVisibleFloors, value))
                {
                    _currentVisibleFloors = value;
                    OnPropertyChanged();
                }
            }
        }

        public RouteResult CurrentRoute
        {
            get => _route;
            private set
            {
                if (value != _route)
                {
                    _route = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public UiState CurrentState
        {
            get => _currentState;
            private set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    OnPropertyChanged();
                }
            }
        }

        public Room CurrentRoom
        {
            get => _currentRoom;
            private set
            {
                if (value != _currentRoom)
                {
                    _currentRoom = value;
                    if (_currentRoom?.Floor != null)
                    {
                        SelectedFloorLevel = _currentRoom.Floor;
                    }

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the mmpk.
        /// </summary>
        /// <value>The mmpk.</value>
        public LocatorTask Locator { get; set; }

        public Feature FromLocationFeature { get; set; }
        public Feature ToLocationFeature { get; set; }

        #endregion Properties

        #region Helper methods
        /// <summary>
        /// Gets the floors in visible area.
        /// </summary>
        /// <returns>The floors in visible area.</returns>
        private async void UpdateFloors()
        {
            try
            {
                // Run query to get all the polygons in the visible area
                if (Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] is FeatureLayer roomsLayer)
                {
                    try
                    {
                        var roomsTable = roomsLayer.FeatureTable;

                        // Set query parameters
                        var queryParams = new QueryParameters()
                        {
                            ReturnGeometry = false,
                            Geometry = CurrentViewArea
                        };

                        // Query the feature table 
                        var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);

                        if (queryResult != null)
                        {
                            // Group by floors to get the distinct list of floors in the table selection
                            CurrentVisibleFloors = queryResult.GroupBy(g => g.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName])
                                                            .Select(gr => gr.First().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString())
                                                            .OrderBy(f => f).ToList();
                        }

                        if (string.IsNullOrEmpty(SelectedFloorLevel) || !CurrentVisibleFloors.Contains(SelectedFloorLevel))
                        {
                            SelectFloor(CurrentVisibleFloors.FirstOrDefault());
                        }
                        
                        SetFloorVisibility(true);
                    }
                    catch
                    {
                        CurrentVisibleFloors = null;
                    }
                }
                else
                {
                    CurrentVisibleFloors = null;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Gets the location suggestions from the mmpk.
        /// </summary>
        /// <returns>List of location suggestions.</returns>
        /// <param name="userInput">User input.</param>
        public async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestionsAsync(string userInput)
        {
            try
            {
                var locatorInfo = Locator.LocatorInfo;

                if (locatorInfo.SupportsSuggestions)
                {
                    // restrict the search to return no more than 10 suggestions
                    var suggestParams = new SuggestParameters { MaxResults = 10 };

                    // get suggestions for the text provided by the user
                    return await Locator.SuggestAsync(userInput, suggestParams);
                }
            }
            catch
            {
                return null;
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
                var formattedSearchString = FormatStringForQuery(searchString);

                // Geocode location and return the best match from the list
                var matches = await Locator.GeocodeAsync(formattedSearchString);
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
            if (Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] is FeatureLayer roomsLayer)
            {
                var roomsTable = roomsLayer.FeatureTable;

                var formattedSearchString = FormatStringForQuery(searchString);

                // Set query parameters
                var queryParams = new QueryParameters
                {
                    ReturnGeometry = true,
                    // Matches any locator field that is equal to search string
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
            try
            {
                await Map.LoadAsync(); // Doesn't do anything if map already loaded

                var routeTask = await RouteTask.CreateAsync(Map.TransportationNetworks[0]);

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
                    return await routeTask.SolveRouteAsync(routeParams);
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

        #endregion Helper methods

        #region Commands

        /// <summary>
        /// Loads the mobile map package and the map 
        /// </summary>
        /// <returns>Async task</returns>
        internal async Task InitializeAsync()
        {
            // Get Mobile Map Package from the location on device
            var mmpk = await MobileMapPackage.OpenAsync(Path.Combine(DownloadViewModel.GetDataFolder(), AppSettings.CurrentSettings.PortalItemName));

            // Display map from the mmpk. Assumption is made that the first map of the mmpk is the one used
            Map = mmpk.Maps.First();

            // Sets a basemap from ArcGIS Online if specified
            // Replace basemap with any online basemap 
            if (AppSettings.CurrentSettings.UseOnlineBasemap)
            {
                Map.Basemap = Basemap.CreateLightGrayCanvasVector();
            }

            // Load map
            await Map.LoadAsync();

            // Get the locator to be used in the app
            Locator = mmpk.LocatorTask;
            await Locator.LoadAsync();

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
                foreach (var coordinatePart in AppSettings.CurrentSettings.InitialViewpointCoordinates)
                {
                    switch (coordinatePart.Key)
                    {
                        case "X":
                            x = coordinatePart.Value;
                            break;
                        case "Y":
                            y = coordinatePart.Value;
                            break;
                        case "WKID":
                            wkid = coordinatePart.Value;
                            break;
                        case "ZoomLevel":
                            zoomLevel = coordinatePart.Value;
                            break;
                    }
                }

                // Location based, location services are on
                // Home settings, location services are off but user has a home set
                // Default setting, Location services are off and user has no home set
                if (!AppSettings.CurrentSettings.IsLocationServicesEnabled)
                {
                    Map.InitialViewpoint = new Viewpoint(new MapPoint(x, y, new SpatialReference(Convert.ToInt32(wkid))), zoomLevel);
                }
            }
            catch
            {
                // Suppress all errors. If initial viewpoint cannot be set, the map will just load to the default extent of the map.
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
            Debug.Assert(!string.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation), "This shouldn't be called if home isn't set");

            CurrentRoom = Room.ConstructHome();

            if (CurrentRoom != null)
            {
                CurrentRoute = null;

                CurrentState = UiState.LocationFound;
            }
        }

        /// <summary>
        /// Changes the visibility of the rooms and walls layers based on floor selected
        /// </summary>
        /// <param name="areLayersOn">If set to <c>true</c> operational layers are turned on</param>
        internal void SetFloorVisibility(bool areLayersOn)
        {
            foreach (var featureLayer in Map.OperationalLayers.OfType<FeatureLayer>())
            {
                // select chosen floor
                featureLayer.DefinitionExpression = $"{AppSettings.CurrentSettings.RoomsLayerFloorColumnName} = '{SelectedFloorLevel}'";

                featureLayer.IsVisible = areLayersOn;
            }
        }

        public async void PerformRouteSearch()
        {
            try
            {
                if (OriginSearchText == "CurrentLocationLabel".Localize())
                {
                    FromLocationFeature = null;
                }
                else
                {
                    FromLocationFeature = await GetRoomFeatureAsync(OriginSearchText);
                }

                if (DestinationSearchText == "CurrentLocationLabel".Localize())
                {
                    ToLocationFeature = null;
                }
                else
                {
                    ToLocationFeature = await GetRoomFeatureAsync(DestinationSearchText);
                }

                CurrentRoute = await GetRequestedRouteAsync(FromLocationFeature?.Geometry?.Extent?.GetCenter() ?? CurrentUserLocation,
                                                            ToLocationFeature?.Geometry?.Extent?.GetCenter() ?? CurrentUserLocation);

                if (CurrentRoute != null)
                {
                    CurrentState = UiState.RouteFound;
                }
                else
                {
                    CurrentState = UiState.RouteNotFound;
                }
            }
            catch
            {
                CurrentRoute = null;
                CurrentState = UiState.RouteNotFound;
            }
        }

        public void StartSearchFromFoundFeature()
        {
            Debug.Assert(CurrentRoom != null);

            if (CurrentRoom?.IsHome == true)
            {
                OriginSearchText = CurrentRoom?.RoomNumber;
                DestinationSearchText = string.Empty;
            }
            else
            {
                OriginSearchText = string.Empty;
                DestinationSearchText = CurrentRoom?.RoomNumber;
            }

            CurrentState = UiState.PlanningRoute;
        }

        public void CloseLocationInfo()
        {
            CurrentRoom = null;
            FeatureSearchText = null;
            CurrentState = UiState.ReadyWaiting;
        }

        public void CloseRouteResult()
        {
            CurrentRoute = null;
            CurrentState = UiState.PlanningRoute;
        }

        public void ReturnToWaitingState() => CurrentState = UiState.ReadyWaiting;

        public void SelectOriginSearch() => CurrentState = UiState.SearchingForOrigin;

        public void SelectDestinationSearch() => CurrentState = UiState.SearchingForDestination;

        public void SwapOriginDestinationSearch()
        {
            string oldOriginSearch = OriginSearchText;
            string oldDestinationSearch = DestinationSearchText;

            DestinationSearchText = oldOriginSearch;
            OriginSearchText = oldDestinationSearch;
        }

        public void DismissNotFound()
        {
            if (CurrentState == UiState.LocationNotFound)
            {
                CurrentState = UiState.SearchingForFeature;
            }
            else if (CurrentState == UiState.RouteNotFound)
            {
                CurrentState = UiState.PlanningRoute;
            }
        }

        public void CancelRouteSearch()
        {
            OriginSearchText = null;
            DestinationSearchText = null;

            if (CurrentRoom != null)
            {
                CurrentState = UiState.LocationFound;
            }
            else
            {
                CurrentState = UiState.ReadyWaiting;
            }
        }

        public void StartEditingInLocationSearch()
        {
            if (CurrentState == UiState.ReadyWaiting)
            {
                CurrentState = UiState.SearchingForFeature;
            }
        }

        public void StopEditingInLocationSearch()
        {
            switch (CurrentState)
            {
                case UiState.SearchingForDestination:
                case UiState.SearchingForOrigin:
                    CurrentState = UiState.PlanningRoute;
                    break;
                case UiState.SearchingForFeature:
                    FeatureSearchText = null;
                    CurrentState = UiState.ReadyWaiting;
                    break;
            }
        }

        public async Task CommitSearchAsync(string text)
        {
            switch (CurrentState)
            {
                case UiState.SearchingForFeature:
                    FeatureSearchText = text;
                    CurrentState = UiState.FeatureSearchEntered;
                    await SearchFeatureAsync(FeatureSearchText);
                    break;
                case UiState.SearchingForOrigin:
                    OriginSearchText = text;
                    CurrentState = UiState.PlanningRoute;
                    break;
                case UiState.SearchingForDestination:
                    DestinationSearchText = text;
                    CurrentState = UiState.PlanningRoute;
                    break;
            }
        }

        public void IdentifyRoomFromLayerResult(IdentifyLayerResult result)
        {
            CurrentRoom = Room.ConstructFromIdentifyResult(result);

            CurrentState = CurrentRoom != null ? UiState.LocationFound : UiState.LocationNotFound;
        }

        public void MoveToCurrentLocation()
        {
            Debug.Assert(AppSettings.CurrentSettings.IsLocationServicesEnabled, "This method should only be called if location services are enabled");

            CurrentRoom = Room.ConstructCurrentLocation();

            CurrentState = UiState.LocationFound;

            SelectedFloorLevel = null; // Enhancement - set floor based on user position
        }

        /// <summary>
        /// ONLY CALL AFTER UPDATING APPSETTINGS
        /// </summary>
        public void UpdateHomeLocation()
        {
            if (CurrentRoom?.IsHome == true)
            {
                var newHome = Room.ConstructHome();

                if (newHome == null)
                {
                    var oldHome = CurrentRoom;
                    CurrentRoom = null;
                    oldHome.IsHome = false;
                    CurrentRoom = oldHome;
                }
                else
                {
                    CurrentRoom = newHome;
                }
            }
        }

        public void SelectFloor(string floor) => SelectedFloorLevel = floor;

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

                if (geocodeResult != null)
                {
                    // Get the feature to populate the Contact Card
                    Feature roomFeature = await GetRoomFeatureAsync(searchText);

                    CurrentRoom = Room.ConstructFromFeature(roomFeature);

                    if (roomFeature != null)
                    {
                        CurrentState = UiState.LocationFound;
                    }
                    else
                    {
                        CurrentState = UiState.LocationNotFound;
                    }
                }
                else
                {
                    CurrentState = UiState.LocationNotFound;
                }
            }
        }

        #endregion Commands

        #region INPC implementation
        /// <summary>
        /// Event handler property changed. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when a property changes to trigger PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
