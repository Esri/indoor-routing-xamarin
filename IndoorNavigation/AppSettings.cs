using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IndoorNavigation
{
	public class AppSettings
	{
		/// <summary>
		/// Gets or sets the item identifier.
		/// </summary>
		/// <value>Portal Item ID</value>
		[XmlElement]
		public string ItemID
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the name of the item.
		/// </summary>
		/// <value>The name of the Portal item</value>
		[XmlElement]
		public string ItemName
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the mmpk date.
		/// </summary>
		/// <value>The date the mobile map package was downloaded</value>
		[XmlElement]
		public DateTime MmpkDate
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the home location.
		/// </summary>
		/// <value>The home location set by the user. By default this is set to "Set home location"</value>
		[XmlElement]
		public string HomeLocation
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:IndoorNavigation.AppSettings"/> is location services enabled.
		/// </summary>
		/// <value><c>true</c> if is location services switch enabled; otherwise, <c>false</c>.</value>
		[XmlElement]
		public bool IsLocationServicesEnabled
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:IndoorNavigation.AppSettings"/> is prefer elevators enabled.
		/// </summary>
		/// <value><c>true</c> if is prefer elevators switch is enabled; otherwise, <c>false</c>.</value>
		[XmlElement]
		public bool IsPreferElevatorsEnabled
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the index of the rooms layer.
		/// </summary>
		/// <value>The index of the rooms layer.</value>
		[XmlElement]
		public int RoomsLayerIndex
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the index of the floorplan lines layer.
		/// </summary>
		/// <value>The index of the floorplan lines layer.</value>
		[XmlElement]
		public int FloorplanLinesLayerIndex
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the zoom level to display room layers.
		/// </summary>
		/// <value>The zoom level to display room layers.</value>
		[XmlElement]
		public float ZoomLevelToDisplayRoomLayers
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the home coordinates.
		/// </summary>
		/// <value>The coordinates and floor level for the home location. This also includes the WKID</value>
		[XmlArray("HomeCoordinates")]
		public CoordinatesKeyValuePair<string, double>[] HomeCoordinates
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the current settings.
		/// </summary>
		/// <value>Static instance of the settings for the application</value>
		public static AppSettings currentSettings { get; set; }

		/// <summary>
		/// Loads the app settings if the file exists, otherwise it creates default settings. 
		/// </summary>
		/// <returns>The app settings.</returns>
		/// <param name="filePath">File path.</param>
		internal static AppSettings LoadAppSettings(string filePath)
		{
			// Get all the files in the device directory
			List<string> files = Directory.EnumerateFiles(Path.GetDirectoryName(filePath)).ToList();

			// If the settings file doesn't exist, create it
			if (!files.Contains(filePath))
			{
				var appSettings = new AppSettings();
				appSettings.ItemID = "018f779883434a8daadfb51524ec3498";
				appSettings.ItemName = "EsriCampus.mmpk";
				appSettings.MmpkDate = new DateTime(1900, 1, 1);
				appSettings.HomeLocation = "Set home location";
				appSettings.IsLocationServicesEnabled = false;
				appSettings.IsPreferElevatorsEnabled = false;
				appSettings.RoomsLayerIndex = 1;
				appSettings.FloorplanLinesLayerIndex = 2;
				appSettings.ZoomLevelToDisplayRoomLayers = 500;

				var serializer = new XmlSerializer(appSettings.GetType());

				// Create settings file on a separate thread
				// this does not need to be awaited since the return is already set
			    Task.Factory.StartNew(delegate
				{
					using (var fileStream = new FileStream(filePath, FileMode.Create))
					{
						serializer.Serialize(fileStream, appSettings);
					}
				});
				return appSettings;
			}
			// Otherwise load the settings from the settings file
			else
			{
				using (var fileStream = new FileStream(filePath, FileMode.Open))
				{
					var appSettings = new AppSettings();
					var serializer = new XmlSerializer(appSettings.GetType());
					return serializer.Deserialize(fileStream) as AppSettings;
				}
			}
		}

		/// <summary>
		/// Saves the settings.
		/// </summary>
		/// <param name="filePath">File path.</param>
		internal static void SaveSettings(string filePath)
		{
			var serializer = new XmlSerializer(currentSettings.GetType());

			using (var fileStream = new FileStream(filePath, FileMode.Open))
			{
				serializer.Serialize(fileStream, currentSettings);
			}
		}

	}

	/// <summary>
	/// Coordinates key value pair.
	/// </summary>
	[Serializable]
	public struct CoordinatesKeyValuePair<K, V>
	{
		public K Key { get; set; }
		public V Value { get; set; }

		public CoordinatesKeyValuePair(K k, V v)
		{
			Key = k;
			Value = v;
		}
	}
}
