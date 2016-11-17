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
		string itemID;
		string itemName;
		DateTime mmpkDate;
		string homeLocation;
		bool isLocationServicesEnabled;
		bool isPreferElevatorsEnabled;

		[XmlElement]
		public string ItemID
		{
			get
			{
				return itemID;
			}

			set
			{
				itemID = value;
			}
		}

		[XmlElement]
		public string ItemName
		{
			get
			{
				return itemName;
			}

			set
			{
				itemName = value;
			}
		}

		[XmlElement]
		public DateTime MmpkDate
		{
			get
			{
				return mmpkDate;
			}

			set
			{
				mmpkDate = value;
			}
		}
		[XmlElement]
		public string HomeLocation
		{
			get
			{
				return homeLocation;
			}

			set
			{
				homeLocation = value;
			}
		}
		[XmlElement]
		public bool IsLocationServicesEnabled
		{
			get
			{
				return isLocationServicesEnabled;
			}

			set
			{
				isLocationServicesEnabled = value;
			}
		}
		[XmlElement]
		public bool IsPreferElevatorsEnabled
		{
			get
			{
				return isPreferElevatorsEnabled;
			}

			set
			{
				isPreferElevatorsEnabled = value;
			}
		}

		[XmlElement]
		public int RoomsLayerIndex
		{
			get;set;
		}

		[XmlElement]
		public int FloorplanLinesLayerIndex
		{
			get;set;
		}



		public AppSettings()
		{ }

		public static async Task<AppSettings> LoadAppSettings(string filePath)
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

				var serializer = new XmlSerializer(appSettings.GetType());
				await Task.Factory.StartNew(delegate
				{
					using (var fileStream = new FileStream(filePath, FileMode.Create))
					{
						serializer.Serialize(fileStream, appSettings);
					}
				});
			}
			using (var fileStream = new FileStream(filePath, FileMode.Open))
			{
				var appSettings = new AppSettings();
				var serializer = new XmlSerializer(appSettings.GetType());
				return serializer.Deserialize(fileStream) as AppSettings;
			}
		}

		public static void SaveSettings(string filePath)
		{
			var serializer = new XmlSerializer(GlobalSettings.currentSettings.GetType());

			using (var fileStream = new FileStream(filePath, FileMode.Open))
			{
				serializer.Serialize(fileStream, GlobalSettings.currentSettings);
			}
		}

	}

	public static class GlobalSettings
	{
		public static AppSettings currentSettings { get; set; }
	}
}
