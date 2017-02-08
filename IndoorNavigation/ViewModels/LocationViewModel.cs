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
    /// Location view model handles all shared logic to do with locator and geocoding
    /// </summary>
    internal static class LocationViewModel
    {
        /// <summary>
        /// Gets or sets the mmpk.
        /// </summary>
        /// <value>The mmpk.</value>
        public static MobileMapPackage MMPK { get; set; }

        /// <summary>
        /// Gets the location suggestions from the mmpk.
        /// </summary>
        /// <returns>List of location suggestions.</returns>
        /// <param name="userInput">User input.</param>
        internal static async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestionsAsync(string userInput)
        {
            // Load the locator from the mobile map package
            var locator = MMPK.LocatorTask;
            await locator.LoadAsync();
            var locatorInfo = locator.LocatorInfo;

            if (locatorInfo.SupportsSuggestions)
            {
                try
                {
                    // restrict the search to return no more than 10 suggestions
                    var suggestParams = new SuggestParameters { MaxResults = 10 };

                    // get suggestions for the text provided by the user
                    var suggestions = await locator.SuggestAsync(userInput, suggestParams);
                    return suggestions;
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
        internal static async Task<GeocodeResult> GetSearchedLocationAsync(string searchString)
        {
            // Load the locator from the mobile map package
            var locator = MMPK.LocatorTask;
            await locator.LoadAsync();
            var locatorInfo = locator.LocatorInfo;

            try
            {
                // Geocode location and return the best match from the list
                var matches = await locator.GeocodeAsync(searchString);
                var bestMatch = matches.FirstOrDefault();
                return bestMatch;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the requested route based on start and end location points.
        /// </summary>
        /// <returns>The requested route.</returns>
        /// <param name="fromLocation">From location.</param>
        /// <param name="toLocation">To location.</param>
        internal static async Task<RouteResult> GetRequestedRouteAsync(MapPoint fromLocation, MapPoint toLocation)
        {
            if (MMPK.Maps[0].LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                await MMPK.Maps[0].LoadAsync();
            }

            var routeTask = await RouteTask.CreateAsync(MMPK.Maps[0].TransportationNetworks[0]);

            // Get the default route parameters
            var routeParams = await routeTask.CreateDefaultParametersAsync();

            // Explicitly set values for some params
            routeParams.ReturnDirections = false; // Indoor networks do not support turn by turn navigation
            routeParams.ReturnRoutes = true;

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
        /// Gets the features from query
        /// </summary>
        /// <returns>The features </returns>
        /// <param name="searchString">Search string.</param>
        internal static async Task<string> GetFloorLevelFromQueryAsync(string searchString)
        {
            // Run query to get the floor of the selected room
            var roomsLayer = MMPK.Maps[0].OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;
            var roomsTable = roomsLayer.FeatureTable;

            // Set query parametersin 
            var queryParams = new QueryParameters()
            {
                ReturnGeometry = true,
                WhereClause = string.Format(string.Join(" = '{0}' OR ", AppSettings.CurrentSettings.LocatorFields) + " = '{0}'", searchString)
            };

            // Query the feature table 
            var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);
            var floorResult = queryResult.FirstOrDefault().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString();
            return floorResult;
        }
    }
}
