// <copyright file="LocationViewModel.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;

    /// <summary>
    /// Location view model.
    /// </summary>
    public sealed class LocationViewModel
    {
        /// <summary>
        /// The location view model instance.
        /// </summary>
        private static volatile LocationViewModel locationViewModelInstance;

        /// <summary>
        /// The sync root.
        /// </summary>
        private static object syncRoot = new object();

        /// <summary>
        /// Prevents a default instance of the <see cref="LocationViewModel" /> class from being created. (IndoorNavigation)
        /// </summary>
        private LocationViewModel()
        {
        }

        /// <summary>
        /// Gets the location view model instance.
        /// </summary>
        /// <value>The location view model instance.</value>
        public static LocationViewModel LocationViewModelInstance
        {
            get
            {
                if (locationViewModelInstance == null)
                {
                    lock (syncRoot)
                    {
                        if (locationViewModelInstance == null)
                        {
                            locationViewModelInstance = new LocationViewModel();
                        }
                    }
                }

                return locationViewModelInstance;
            }
        }

        /// <summary>
        /// Gets or sets the mmpk.
        /// </summary>
        /// <value>The mmpk.</value>
        public MobileMapPackage Mmpk { get; set; }

        /// <summary>
        /// Gets the location suggestions from the mmpk.
        /// </summary>
        /// <returns>List of location suggestions.</returns>
        /// <param name="userInput">User input.</param>
        internal async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestionsAsync(string userInput)
        {
            // Load the locator from the mobile map package
            var locator = this.Mmpk.LocatorTask;
            try
            {
                await locator.LoadAsync();
                var locatorInfo = locator.LocatorInfo;

                if (locatorInfo.SupportsSuggestions)
                {
                    // restrict the search to return no more than 10 suggestions
                    var suggestParams = new SuggestParameters { MaxResults = 10 };

                    // get suggestions for the text provided by the user
                    var suggestions = await locator.SuggestAsync(userInput, suggestParams);
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
            var roomsLayer = this.Mmpk.Maps[0].OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;
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
            var floorResult = queryResult.FirstOrDefault().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString();
            return floorResult;
        }

        /// <summary>
        /// Gets the searched location based on search terms user entered.
        /// </summary>
        /// <returns>The searched location.</returns>
        /// <param name="searchString">User input.</param>
        internal async Task<GeocodeResult> GetSearchedLocationAsync(string searchString)
        {
            // Load the locator from the mobile map package
            var locator = this.Mmpk.LocatorTask;
            await locator.LoadAsync();
            var locatorInfo = locator.LocatorInfo;
            var formattedSearchString = this.FormatStringForQuery(searchString);

            try
            {
                // Geocode location and return the best match from the list
                var matches = await locator.GeocodeAsync(formattedSearchString);
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
            var roomsLayer = this.Mmpk.Maps[0].OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;
            var roomsTable = roomsLayer.FeatureTable;

            var formattedSearchString = this.FormatStringForQuery(searchString);

            // Set query parametersin 
            var queryParams = new QueryParameters()
            {
                ReturnGeometry = true,
                WhereClause = string.Format(string.Join(" = '{0}' OR ", AppSettings.CurrentSettings.LocatorFields) + " = '{0}'", formattedSearchString)
            };

            // Query the feature table 
            var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);
            return queryResult.FirstOrDefault();
        }

        /// <summary>
        /// Gets the requested route based on start and end location points.
        /// </summary>
        /// <returns>The requested route.</returns>
        /// <param name="fromLocation">From location.</param>
        /// <param name="toLocation">To location.</param>
        internal async Task<RouteResult> GetRequestedRouteAsync(MapPoint fromLocation, MapPoint toLocation)
        {
            if (this.Mmpk.Maps[0].LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                await this.Mmpk.Maps[0].LoadAsync();
            }

            var routeTask = await RouteTask.CreateAsync(this.Mmpk.Maps[0].TransportationNetworks[0]);

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
    }
}
