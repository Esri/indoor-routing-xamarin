// <copyright file="FloorSelectorViewModel.cs" company="Esri, Inc">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.UI.Controls;

    /// <summary>
    /// Floor selector view model.
    /// </summary>
    internal class FloorSelectorViewModel
    {
        /// <summary>
        /// Gets the floors in visible area.
        /// </summary>
        /// <returns>The floors in visible area.</returns>
        /// <param name="mapView">Map view.</param>
        internal async Task<string[]> GetFloorsInVisibleAreaAsync(MapView mapView)
        {
            // Run query to get all the polygons in the visible area
            var roomsLayer = mapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;

            if (roomsLayer != null)
            {
                try
                {
                    var roomsTable = roomsLayer.FeatureTable;

                    // Set query parameters
                    var queryParams = new QueryParameters()
                    {
                        ReturnGeometry = false,
                        Geometry = mapView.VisibleArea
                    };

                    // Query the feature table 
                    var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);

                    if (queryResult != null)
                    {
                        // Group by floors to get the distinct list of floors in the table selection
                        var distinctFloors = queryResult.GroupBy(g => g.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName])
                                                        .Select(gr => gr.First().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName]);

                        var tableItems = new List<string>();

                        foreach (var item in distinctFloors)
                        {
                            tableItems.Add(item.ToString());
                        }

                        // Sort list so floors show up in order
                        // Depending on the floors in your building, you might need to create a more complex sorting algorithm
                        tableItems.Sort();

                        return tableItems.ToArray();
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
            else
            {
                return null;
            }
        }
    }
}
