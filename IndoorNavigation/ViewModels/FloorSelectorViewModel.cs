// <copyright file="FloorSelectorViewModel.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorRouting 
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
