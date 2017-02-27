// <copyright file="LocationViewModel.cs" company="Esri, Inc">
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
        /// Gets or sets the location view model instance.
        /// </summary>
        public static LocationViewModel Instance { get; set; }

        /// <summary>
        /// Gets or sets the mmpk.
        /// </summary>
        /// <value>The mmpk.</value>
        public Map Map { get; set; }

        /// <summary>
        /// Gets or sets the mmpk.
        /// </summary>
        /// <value>The mmpk.</value>
        public LocatorTask Locator { get; set; }

        /// <summary>
        /// Gets or sets the user's current location.
        /// </summary>
        /// <value>The current location.</value>
        public MapPoint CurrentLocation { get; set; }

        /// <summary>
        /// Creates instance of LocatorViewModel
        /// </summary>
        /// <returns>LocatorViewModel instance.</returns>
        /// <param name="map">Current Map .</param>
        /// <param name="locator">Locator task.</param>
        internal static LocationViewModel Create(Map map, LocatorTask locator)
        {
            var locationViewModel = new LocationViewModel();
            locationViewModel.Map = map;
            locationViewModel.Locator = locator;

            return locationViewModel;               
        }

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
            if (this.Map.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
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
    }
}
