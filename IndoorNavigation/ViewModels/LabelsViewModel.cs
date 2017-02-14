using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace IndoorNavigation
{
    public class LabelsViewModel
    {
        /// <summary>
        /// Gets the floors in visible area.
        /// </summary>
        /// <returns>The floors in visible area.</returns>
        /// <param name="mapView">Map view.</param>
        internal async Task<IEnumerable<Feature>> GetLabelsInVisibleAreaAsync(MapView mapView, string selectedFloor)
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
                        ReturnGeometry = true,
                        Geometry = mapView.VisibleArea
                    };

                    // Query the feature table 
                    var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);

                    // Group by floors to get the distinct list of floors in the table selection
                    var labelFeatures = queryResult.Where(f => f.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString() == selectedFloor);

                    return labelFeatures as IEnumerable<Feature>;
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