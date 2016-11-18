using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace IndoorNavigation
{
	public class MapHelper
	{
		public MapHelper()
		{
		}

		/// <summary>
		/// Sets the initial view point based on user settings. 
		/// </summary>
		/// <param name="map">Map.</param>
		public void SetInitialViewPoint(Map map)
		{
			// Location based, location services are on
			if (GlobalSettings.currentSettings.IsLocationServicesEnabled == true)
			{
				MoveToCurrentLocation(map);
			}
			// Home settings, location services are off but user has a home set
			else if (GlobalSettings.currentSettings.HomeLocation != "Set home location")
			{
				MoveToHomeLocation(map);
			}
			// Default setting, Location services are off and user has no home set
			else
			{
				map.InitialViewpoint = new Viewpoint(new MapPoint(-13046209, 4036456, SpatialReferences.WebMercator), 1600);
				map.MaxScale = 100;
				map.MinScale = 13000;
			}

		}

		/// <summary>
		/// Moves to current location of the user .
		/// </summary>
		public void MoveToCurrentLocation(Map map)
		{

		}

		/// <summary>
		/// Moves to location user has set as Home
		/// </summary>
		/// <param name="map">Map.</param>
		public async void MoveToHomeLocation(Map map)
		{
			//Run query to get all the polygons in the visible area
			await map.OperationalLayers[1].LoadAsync();
			var roomsLayer = map.OperationalLayers[1] as FeatureLayer;
			var roomsTable = roomsLayer.FeatureTable;

			// Set query parameters
			QueryParameters queryParams = new QueryParameters()
			{
				ReturnGeometry = true,
				WhereClause = string.Format("LONGNAME = '{0}'", GlobalSettings.currentSettings.HomeLocation)
			};

			// Query the feature table 
			FeatureQueryResult queryResult = await roomsTable.QueryFeaturesAsync(queryParams);
			var homeLocation = queryResult.FirstOrDefault();
	


			map.InitialViewpoint = new Viewpoint(homeLocation.Geometry);
		}

		public async Task<string[]> GetFloorsInVisibleArea(MapView mapView)
		{
			//Run query to get all the polygons in the visible area
			var roomsLayer = mapView.Map.OperationalLayers[GlobalSettings.currentSettings.RoomsLayerIndex] as FeatureLayer;
			var roomsTable = roomsLayer.FeatureTable;

			// Set query parameters
			QueryParameters queryParams = new QueryParameters()
			{
				ReturnGeometry = false,
				Geometry = mapView.VisibleArea
			};

			// Query the feature table 
			FeatureQueryResult queryResult = await roomsTable.QueryFeaturesAsync(queryParams);

			// Group by floors to get the distinct list of floors in the table selection
			var distinctFloors = queryResult.GroupBy(g => g.Attributes["FLOOR"]).Select(gr => gr.First().Attributes["FLOOR"]);

			List<string> tableItems = new List<string>();

			foreach (var item in distinctFloors)
			{
				tableItems.Add(item.ToString());
			}

			// Sort list so floors show up in order
			// Depending on the floors in your building, you might need to create a more complex sorting algorithm
			tableItems.Sort();

			return tableItems.ToArray();
		}

		public void TurnLayersOnOff(bool areLayersOn, MapView mapView, string selectedFloor)
		{
			for (int i = 1; i < mapView.Map.OperationalLayers.Count; i++)
			{
				var featureLayer = mapView.Map.OperationalLayers[i] as FeatureLayer;
				if (selectedFloor == "")
				{
					// select first floor by default
					featureLayer.DefinitionExpression = "FLOOR = '1'";
				}
				else
				{
					// select chosen floor
					featureLayer.DefinitionExpression = string.Format("FLOOR = '{0}'", selectedFloor);
				}
				mapView.Map.OperationalLayers[i].IsVisible = areLayersOn;
			}
		}
	}
}
