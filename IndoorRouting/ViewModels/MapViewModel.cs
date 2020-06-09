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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    /// <summary>
    /// Map view model handles all UI and business logic to do with the the primary map view
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
        private Map _map;

        /// <summary>
        /// The viewpoint of the map.
        /// </summary>
        private Envelope _currentVisibleArea;

        /// <summary>
        /// The selected floor level.
        /// </summary>
        private string _selectedFloorLevel;

        /// <summary>
        /// The list of floors that are currently visible.
        /// </summary>
        private IEnumerable<string> _currentVisibleFloors;

        /// <summary>
        /// The text query used for searching for a feature.
        /// </summary>
        private string _featureSearchText = string.Empty;

        /// <summary>
        /// The text query used for searching for a route origin.
        /// </summary>
        private string _originSearchText = string.Empty;

        /// <summary>
        /// The text query used for searching for a route destination.
        /// </summary>
        private string _destinationSearchText = string.Empty;

        /// <summary>
        /// The currently identified or selected room, which can be the user's home or the current device location.
        /// </summary>
        private Room _currentRoom;

        /// <summary>
        /// The current route result.
        /// </summary>
        private RouteResult _route;

        /// <summary>
        /// Enumeration tracking the current UI state.
        /// </summary>
        private UiState _currentState = UiState.ReadyWaiting;

        /// <summary>
        /// Tracks the device's current location if location services are enabled.
        /// </summary>
        private MapPoint _currentUserLocation;

        /// <summary>
        /// Event handler property changed. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the map used in the application.
        /// </summary>
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
        /// Gets or sets the envelope containing the current visible area.
        /// </summary>
        /// <remarks>This is used for tracking which floors should be visible.</remarks>
        public Envelope CurrentViewArea
        {
            get => _currentVisibleArea;

            set
            {
                if (_currentVisibleArea != value)
                {
                    var oldVisibleArea = _currentVisibleArea;
                    _currentVisibleArea = value;
                    OnPropertyChanged();

                    // There's a lot of jitter in the location data source,
                    // so this filter requires horizontal movement of at least one mercator unit before updating floors
                    if (oldVisibleArea != null && value != null &&
                        GeometryEngine.Distance(oldVisibleArea.GetCenter(), value.GetCenter()) < 1)
                    {
                        return;
                    }
                    UpdateFloors();
                }
            }
        }

        /// <summary>
        /// Tracks the device's current location so that it can be used for routing.
        /// </summary>
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
        /// Gets or sets the selected floor level. Floor visibility is updated when this is set.
        /// </summary>
        /// <value>The selected floor level.</value>
        public string SelectedFloorLevel
        {
            get => _selectedFloorLevel;
            private set
            {
                if (_selectedFloorLevel != value)
                {
                    // Use the default floor level if the selection is null
                    _selectedFloorLevel = value ?? DefaultFloorLevel;

                    OnPropertyChanged();

                    // Make sure only the selected floor is visible
                    SetFloorVisibility();
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of floors that can be selected within the current map extent.
        /// </summary>
        public IEnumerable<string> CurrentVisibleFloors
        {
            get => _currentVisibleFloors;
            private set
            {
                if (!Equals(_currentVisibleFloors, value))
                {
                    _currentVisibleFloors = value;

                    // Hide or show the floors layers
                    SetBuildingFloorsShown(_currentVisibleFloors?.Any() ?? false);

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current route result.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current feature search text.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current route origin search text.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current route destination search text.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current UI state.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the current identified/selected room.
        /// Setting this will also set <see cref="SelectedFloorLevel"/> based on the room's floor, if applicable.
        /// </summary>
        public Room CurrentRoom
        {
            get => _currentRoom;
            private set
            {
                if (value != _currentRoom)
                {
                    _currentRoom = value;

                    SelectedFloorLevel = _currentRoom?.Floor;

                    // Reset the route when a room is selected
                    CurrentRoute = null;

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the locator used for geocoding and search suggestions.
        /// </summary>
        /// <value>The mmpk.</value>
        public LocatorTask Locator { get; set; }

        /// <summary>
        /// Gets or sets the feature representing the currently selected origin.
        /// This is needed to enable showing stop details, including floor, in the route result view.
        /// </summary>
        public Feature FromLocationFeature { get; set; }

        /// <summary>
        /// Gets or sets the feature representing the currently selected destination.
        /// This is needed to enable showing stop details, including floor, in the route result view.
        /// </summary>
        public Feature ToLocationFeature { get; set; }

        /// <summary>
        /// Gets location search suggestions for the provided search string, or null if suggestions can't be found.
        /// </summary>
        /// <returns>List of location suggestions.</returns>
        /// <param name="userInput">User input.</param>
        public async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestionsAsync(string userInput)
        {
            try
            {
                if (Locator.LocatorInfo.SupportsSuggestions)
                {
                    // restrict the search to return no more than 15 suggestions
                    var suggestParams = new SuggestParameters { MaxResults = 15 };

                    // get suggestions for the text provided by the user
                    return await Locator.SuggestAsync(userInput, suggestParams);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                return null;
            }

            return null;
        }

        /// <summary>
        /// Initializes this view model, loading the first map in the mobile map package.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Get Mobile Map Package from the location on device
            var mmpk = await MobileMapPackage.OpenAsync(DownloadViewModel.TargetFileName);

            // Display map from the mmpk. Assumption is made that the first map of the mmpk is the one used
            Map = mmpk.Maps.First();

            // Sets a basemap from ArcGIS Online if online basemaps are enabled
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
        /// Moves map to home location.
        /// </summary>
        /// <returns>The viewpoint with coordinates for the home location.</returns>
        public void MoveToHomeLocation()
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation), "This shouldn't be called if home isn't set");

            CurrentRoom = Room.ConstructHome();

            if (CurrentRoom != null)
            {
                CurrentState = UiState.LocationFound;
            }
            else if (CurrentRoom == null && AppSettings.CurrentSettings.ShowLocationNotFoundCard)
            {
                CurrentState = UiState.LocationNotFound;
            }
            else
            {
                CurrentState = UiState.ReadyWaiting;
            }
        }

        /// <summary>
        /// Searches for a route, using the current device's location for origin or destination as needed.
        /// </summary>
        public async Task PerformRouteSearch()
        {
            try
            {
                // If the origin is the current location, leave the origin feature null, otherwise find the matching feature
                if (OriginSearchText == AppSettings.LocalizedCurrentLocationString)
                {
                    FromLocationFeature = null;
                }
                else
                {
                    FromLocationFeature = await GetRoomFeatureAsync(OriginSearchText);
                }

                if (DestinationSearchText == AppSettings.LocalizedCurrentLocationString)
                {
                    ToLocationFeature = null;
                }
                else
                {
                    ToLocationFeature = await GetRoomFeatureAsync(DestinationSearchText);
                }

                // throw exception if current location is used when location services are not available
                if (AppSettings.CurrentSettings.IsLocationServicesEnabled &&
                    (FromLocationFeature == null || ToLocationFeature == null))
                {
                    throw new InvalidOperationException("Attempted to route to or from current location, but location services aren't enabled");
                }

                // Find the route from origin to destination, using the current user location for each 
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
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                CurrentRoute = null;
                CurrentState = UiState.RouteNotFound;
            }
        }

        /// <summary>
        /// Starts a route search after an location has been found.
        /// If the current identified/selected location is the user's home, it is used as the origin search, otherwise, the destination.
        /// </summary>
        public void StartSearchFromFoundFeature()
        {
            // Existing flow is not designed to handle starting a route search from scratch (i.e. before finding an origin or destination)
            Debug.Assert(CurrentRoom != null, "A route search is only valid when starting from a found location.");

            // If the identified feature is the home location, use it as the origin, otherwise use it as the destination.
            if (CurrentRoom?.IsHome == true)
            {
                OriginSearchText = CurrentRoom?.PrimaryDisplayField;
                DestinationSearchText = null;
            }
            else
            {
                OriginSearchText = null;
                DestinationSearchText = CurrentRoom?.PrimaryDisplayField;
            }

            CurrentState = UiState.PlanningRoute;
        }

        /// <summary>
        /// Closes the location result view and returns to the ready waiting state.
        /// </summary>
        public void CloseLocationInfo()
        {
            // Reset search and return to ready, waiting state
            CurrentRoom = null;
            FeatureSearchText = null;
            CurrentState = UiState.ReadyWaiting;
        }

        /// <summary>
        /// Clears the current route result and returns the UI to the route planning/search state.
        /// </summary>
        public void CloseRouteResult()
        {
            // Reset search fields
            CurrentRoute = null;
            CurrentRoom = null;
            
            OriginSearchText = null;
            DestinationSearchText = null;
            FeatureSearchText = null;

            CurrentState = UiState.ReadyWaiting;
        }

        /// <summary>
        /// Returns the UI to the waiting state.
        /// </summary>
        public void ReturnToWaitingState() => CurrentState = UiState.ReadyWaiting;

        /// <summary>
        /// Begins searching for a route origin.
        /// </summary>
        /// <remarks>This should only be called when route planning is in progress.</remarks>
        public void SelectOriginSearch() => CurrentState = UiState.SearchingForOrigin;

        /// <summary>
        /// Begins searching for a route destination.
        /// </summary>
        /// <remarks>This should only be called when route planning is in progress.</remarks>
        public void SelectDestinationSearch() => CurrentState = UiState.SearchingForDestination;

        /// <summary>
        /// Swaps the origin and destination search fields.
        /// </summary>
        public void SwapOriginDestinationSearch()
        {
            string oldOriginSearch = OriginSearchText;
            string oldDestinationSearch = DestinationSearchText;

            DestinationSearchText = oldOriginSearch;
            OriginSearchText = oldDestinationSearch;
        }

        /// <summary>
        /// Returns to the location search state if a location couldn't be found or the route planning state if a route couldn't be found.
        /// </summary>
        public void DismissNotFound()
        {
            switch (CurrentState)
            {
                case UiState.LocationNotFound:
                    CurrentState = UiState.SearchingForFeature;
                    break;
                case UiState.RouteNotFound:
                    CurrentState = UiState.PlanningRoute;
                    break;
            }
        }

        /// <summary>
        /// Cancels a route search, returning to the current identified/selected room if there is one.
        /// </summary>
        public void CancelRouteSearch()
        {
            // Reset search fields
            OriginSearchText = null;
            DestinationSearchText = null;

            // Return to the selected feature or go back to the ready, waiting state if there is none
            if (CurrentRoom != null)
            {
                CurrentState = UiState.LocationFound;
            }
            else
            {
                CurrentState = UiState.ReadyWaiting;
            }
        }

        /// <summary>
        /// Transitions from the ready, waiting state to the search in progress state.
        /// </summary>
        public void StartEditingInLocationSearch()
        {
            if (CurrentState == UiState.ReadyWaiting)
            {
                CurrentState = UiState.SearchingForFeature;
            }
        }

        /// <summary>
        /// Stops a search in progress, returning to the route planning state or the location search state as appropriate.
        /// </summary>
        public void StopEditingInLocationSearch()
        {
            // Handle canceling a location search
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

        /// <summary>
        /// Searches for a feature, route origin, or route destination depending on current state.
        /// </summary>
        /// <param name="text">Search query</param>
        public async Task CommitSearchAsync(string text)
        {
            // Save the search text to the right field and transition the UI to the next state.
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

        /// <summary>
        /// Identifies and sets <see cref="CurrentRoom"/> based on an identify result.
        /// </summary>
        /// <param name="result">Identify result from a GeoView.</param>
        public void IdentifyRoomFromLayerResult(IdentifyLayerResult result)
        {
            CurrentRoom = Room.ConstructFromIdentifyResult(result);

            // Set state based on whether the room was found
            if (CurrentRoom != null)
            {
                CurrentState = UiState.LocationFound;
            }
            else if (CurrentRoom == null && AppSettings.CurrentSettings.ShowLocationNotFoundCard)
            {
                CurrentState = UiState.LocationNotFound;
            }
            else
            {
                CurrentState = UiState.ReadyWaiting;
            }
        }

        /// <summary>
        /// Sets <see cref="CurrentRoom"/> to a room representing the device's current location.
        /// </summary>
        public void MoveToCurrentLocation()
        {
            Debug.Assert(AppSettings.CurrentSettings.IsLocationServicesEnabled, "This method should only be called if location services are enabled");

            // Create a placeholder room representing the user's current location
            CurrentRoom = Room.ConstructCurrentLocation();

            CurrentState = UiState.LocationFound;
        }

        /// <summary>
        /// Call this after updating the user's home location.
        /// If the currently identified/selected room was the user's home location,
        /// this method will select/identify the user's new home location.
        /// </summary>
        public void UpdateHomeLocation()
        {
            // Handle the case where the home is changed while the user is actively viewing it
            if (CurrentRoom?.IsHome == true)
            {
                // Create room for new home value, or null if none is set (e.g. user unset home)
                var newHome = Room.ConstructHome();

                if (newHome == null)
                {
                    // Mark the current room as not being home, then re-select it
                    var oldHome = CurrentRoom;
                    CurrentRoom = null;
                    oldHome.IsHome = false;
                    CurrentRoom = oldHome;
                }
                else
                {
                    // Select the new home
                    CurrentRoom = newHome;
                }
            }
        }

        /// <summary>
        /// Selects the given floor.
        /// </summary>
        /// <param name="floor">Floor to select</param>
        public void SelectFloor(string floor) => SelectedFloorLevel = floor;

        /// <summary>
        /// Selects/identifies the geocode result for the search text and transitions to the <see cref="UiState.LocationFound"/> state,
        /// or transitions to the <see cref="UiState.LocationNotFound"/> or <see cref="UiState.ReadyWaiting"/> state if nothing is found.
        /// </summary>
        /// <param name="searchText">Search text entered by user.</param>
        public async Task SearchFeatureAsync(string searchText)
        {
            // If the search is for the current location, select and go to the current location
            if (searchText == AppSettings.LocalizedCurrentLocationString)
            {
                MoveToCurrentLocation();
            }
            else
            {
                // Use the locator to search for the location
                var geocodeResult = await GetSearchedLocationAsync(searchText);

                // If the location can be found, use it
                if (geocodeResult != null)
                {
                    // Get the feature to populate the Contact Card
                    Feature roomFeature = await GetRoomFeatureAsync(searchText);

                    // Set the room based on the feature, or null if the feature is null
                    CurrentRoom = Room.ConstructFromFeature(roomFeature);

                    // Set the UI state to location found or location not found
                    if (roomFeature != null)
                    {
                        CurrentState = UiState.LocationFound;
                    }
                    else if (roomFeature == null && AppSettings.CurrentSettings.ShowLocationNotFoundCard)
                    {
                        CurrentState = UiState.LocationNotFound;
                    }
                    else
                    {
                        CurrentState = UiState.ReadyWaiting;
                    }
                }
                else
                {
                    CurrentState = AppSettings.CurrentSettings.ShowLocationNotFoundCard ? UiState.LocationNotFound : UiState.ReadyWaiting;
                }
            }
        }

        /// <summary>
        /// Gets a GeocodeResult for the search string, or null if nothing is found.
        /// </summary>
        /// <returns>The searched location.</returns>
        /// <param name="searchString">User input.</param>
        public async Task<GeocodeResult> GetSearchedLocationAsync(string searchString)
        {
            try
            {
                // Format the query
                var formattedSearchString = FormatStringForQuery(searchString ?? string.Empty);

                // Geocode location
                var matches = await Locator.GeocodeAsync(formattedSearchString);

                // Return the first match, or null if there is none
                return matches.FirstOrDefault();
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the room feature for the search string, or null if nothing is found.
        /// </summary>
        /// <returns>The room feature.</returns>
        /// <param name="searchString">Search string for the query.</param>
        public async Task<Feature> GetRoomFeatureAsync(string searchString)
        {
            if (Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] is FeatureLayer roomsLayer)
            {
                var formattedSearchString = FormatStringForQuery(searchString ?? string.Empty);

                // Set query parameters
                var queryParams = new QueryParameters
                {
                    ReturnGeometry = true,
                    // Matches any locator field that is equal to search string
                    WhereClause = string.Format(string.Join(" = '{0}' OR ", AppSettings.CurrentSettings.LocatorFields) + " = '{0}'", formattedSearchString)
                };

                try
                {
                    // Query the feature table and return the first result, or null if there is none
                    return (await roomsLayer.FeatureTable.QueryFeaturesAsync(queryParams)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    ErrorLogger.Instance.LogException(ex);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates <see cref="CurrentVisibleFloors"/> based on <see cref="CurrentViewArea"/>.
        /// Also sets <see cref="SelectedFloorLevel"/> to the default value if the existing selection is no longer valid.
        /// </summary>
        /// <returns>The floors in visible area.</returns>
        private async void UpdateFloors()
        {
            try
            {
                // Run query to get all the polygons in the visible area
                if (Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] is FeatureLayer roomsLayer)
                {
                    // Create query parameters
                    var queryParams = new QueryParameters
                    {
                        ReturnGeometry = false,
                        Geometry = CurrentViewArea
                    };

                    // Query the feature table 
                    var queryResult = await roomsLayer.FeatureTable.QueryFeaturesAsync(queryParams);

                    if (queryResult != null)
                    {
                        // Set current visible floors to the sorted floor results
                        CurrentVisibleFloors = queryResult.GroupBy(g => g.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName])
                                                        .Select(gr => gr.First().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString())
                                                        .OrderBy(f => f).ToList();
                    }

                    // Reset the selected floor if it is now invalid
                    if (string.IsNullOrEmpty(SelectedFloorLevel) || CurrentVisibleFloors?.Contains(SelectedFloorLevel) != true)
                    {
                        SelectFloor(CurrentVisibleFloors?.FirstOrDefault());
                    }
                }
                else
                {
                    throw new InvalidOperationException("Configuration mismatch between map and app settings.");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                CurrentVisibleFloors = null;
            }
        }

        /// <summary>
        /// Gets the requested route based on start and end location points.
        /// </summary>
        /// <returns>The requested route or null if one can't be found.</returns>
        /// <param name="fromLocation">Location of the origin.</param>
        /// <param name="toLocation">Location of the destination.</param>
        private async Task<RouteResult> GetRequestedRouteAsync(MapPoint fromLocation, MapPoint toLocation)
        {
            try
            {
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
                return null;
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Formats the string for query by escaping quotes.
        /// </summary>
        /// <returns>The formatted string.</returns>
        /// <param name="searchString">String to be formatted.</param>
        private string FormatStringForQuery(string searchString) => searchString.Replace("'", "''");

        /// <summary>
        /// Sets the initial view point based on user settings. 
        /// </summary>
        /// <returns>Async task</returns>
        private void SetInitialViewpoint()
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
        /// Restricts visibility of the rooms and walls layers to only those on the <see cref="SelectedFloorLevel"/>.
        /// </summary>
        private void SetFloorVisibility(bool isVisible = true)
        {
            foreach (var featureLayer in Map.OperationalLayers.OfType<FeatureLayer>())
            {
                // Select the floor
                featureLayer.DefinitionExpression = $"{AppSettings.CurrentSettings.RoomsLayerFloorColumnName} = '{SelectedFloorLevel}'";
            }
        }

        /// <summary>
        /// Hides or shows all operational feature layers
        /// </summary>
        /// <param name="areShown">true if layers should be shown</param>
        private void SetBuildingFloorsShown(bool areShown = true)
        {
            foreach (var featureLayer in Map.OperationalLayers.OfType<FeatureLayer>())
            {
                // Set layer visibility
                featureLayer.IsVisible = areShown;
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
