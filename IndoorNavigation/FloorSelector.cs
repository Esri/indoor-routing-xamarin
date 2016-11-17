using System;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace IndoorNavigation
{
	public class FloorSelector
	{
		public FloorSelector(MapView mapView)
		{
			// Do not display floors option unless scale is below 750

			if (mapView.MapScale <= 750)
			{
				// Get the current viewpoint for the map
				var currentViewPoint = mapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry);

				Map map = mapView.Map;

				LayerCollection layers = map.OperationalLayers;

				// Grab the first polygon feature layer, this is the rooms layer. 
				// Depending on your data you might need to be more specific


			
			}
		}
	} 

}
