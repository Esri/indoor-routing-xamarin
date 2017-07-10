// <copyright file="AppSettings.cs" company="Esri, Inc">
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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// Class holding the settings for the application
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the current settings.
        /// </summary>
        /// <value>Static instance of the settings for the application</value>
        public static AppSettings CurrentSettings { get; set; }

        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>Portal Item ID</value>
        [XmlElement]
        public string PortalItemID
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the Portal item</value>
        [XmlElement]
        public string PortalItemName
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the mmpk date.
        /// </summary>
        /// <value>The date the mobile map package was downloaded</value>
        [XmlElement]
        public DateTime MmpkDownloadDate
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
        /// Gets or sets a value indicating whether this <see cref="T:IndoorNavigation.AppSettings"/> is routing enabled.
        /// </summary>
        /// <value><c>true</c> if is routing switch enabled; otherwise, <c>false</c>.</value>
        [XmlElement]
        public bool IsRoutingEnabled
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
        public double RoomsLayerMinimumZoomLevel
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the name of the floor column in rooms tabel.
        /// </summary>
        /// <value>The floor column in rooms tabel.</value>
        [XmlElement]
        public string RoomsLayerFloorColumnName
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
        /// Gets or sets the home floor level.
        /// </summary>
        /// <value>The home floor level.</value>
        [XmlElement]
        public string HomeFloorLevel
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the initial viewpoint coordinates.
        /// </summary>
        /// <value>The initial viewpoint coordinates used for the map.</value>
        [XmlArray("InitialViewpointCoordinates")]
        public CoordinatesKeyValuePair<string, double>[] InitialViewpointCoordinates
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the locator fields. If there is only one locator, make a list with one value 
        /// </summary>
        /// <value>The locator fields.</value>
        [XmlArray("LocatorFields")]
        public List<string> LocatorFields
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the contact card display fields. These are what is displayed on the Contact card when user searches or taps an office
        /// </summary>
        /// <value>The contact card display fields.</value>
        [XmlArray("ContactCardDisplayFields")]
        public List<string> ContactCardDisplayFields
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the minimum scale of the map.
        /// </summary>
        /// <value>The minimum scale.</value>
        [XmlElement]
        public int MapViewMinScale
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the maximum scale of the map.
        /// </summary>
        /// <value>The max scale.</value>
        [XmlElement]
        public int MapViewMaxScale
        {
            get; set;
        }

        [XmlElement]
        public bool UseOnlineBasemap
        {
            get; set;
        }

        /// <summary>
        /// Loads the app settings if the file exists, otherwise it creates default settings. 
        /// </summary>
        /// <returns>The app settings.</returns>
        /// <param name="filePath">File path.</param>
        internal static async Task<AppSettings> CreateAsync(string filePath)
        {
            // Get all the files in the device directory
            List<string> files = Directory.EnumerateFiles(Path.GetDirectoryName(filePath)).ToList();

            // If the settings file doesn't exist, create it
            // Otherwise load the settings from the settings file
            // If settings file is invalid, delete it and recreate it
            if (!files.Contains(filePath))
            {
               var appSettings = new AppSettings();

                // Change the Portal Item
                appSettings.PortalItemID = "52346d5fc4c348589f976b6a279ec3e6";
                appSettings.PortalItemName = "RedlandsCampus.mmpk";

                // Change the room and walls layers
                appSettings.RoomsLayerIndex = 1;
                appSettings.FloorplanLinesLayerIndex = 2;

                //Change the floor column name
                appSettings.RoomsLayerFloorColumnName = "FLOOR";

                // Decide if you want to use online basemap or not
                appSettings.UseOnlineBasemap = false;

                // Change fields used by the locator
                appSettings.LocatorFields = new List<string>() { "LONGNAME", "KNOWN_AS_N"  };

                // Change fields displayed in the bottom card
                appSettings.ContactCardDisplayFields = new List<string>() { "LONGNAME", "KNOWN_AS_N"  };

                // Change initial viewpoint
                CoordinatesKeyValuePair<string, double>[] initialViewpointCoordinates =
                {
                    new CoordinatesKeyValuePair<string, double>("X", -13046209),
                    new CoordinatesKeyValuePair<string, double>("Y", 4036456),
                    new CoordinatesKeyValuePair<string, double>("WKID", 3857),
                    new CoordinatesKeyValuePair<string, double>("ZoomLevel", 13000),
                };
                appSettings.InitialViewpointCoordinates = initialViewpointCoordinates;

                // Change at what zoom levels the room data becomes visible
                appSettings.RoomsLayerMinimumZoomLevel = 750;

                // Change map scales
                appSettings.MapViewMinScale = 100;
                appSettings.MapViewMaxScale = 13000;


                appSettings.MmpkDownloadDate = new DateTime(1900, 1, 1);
                appSettings.HomeLocation = "Set home location";
                appSettings.IsLocationServicesEnabled = false;
                appSettings.IsRoutingEnabled = false;
                appSettings.IsPreferElevatorsEnabled = false;

                var serializer = new XmlSerializer(appSettings.GetType());


                // Create settings file on a separate thread
                // this does not need to be awaited since the return is already set
                await Task.Factory.StartNew(delegate
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        serializer.Serialize(fileStream, appSettings);
                    }
                });
                return appSettings;
            }
            else
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var appSettings = new AppSettings();
                    try
                    {
                        var serializer = new XmlSerializer(appSettings.GetType());
                        return serializer.Deserialize(fileStream) as AppSettings;
                    }
                    catch (System.Xml.XmlException)
                    {
                        File.Delete(filePath);
                        return await AppSettings.CreateAsync(filePath).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the settings. If error occurs settings are not saved but application does not stop
        /// </summary>
        /// <param name="filePath">File path.</param>
        internal static void SaveSettings(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppSettings));

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fileStream, CurrentSettings);
                }
            }
            catch
            {
            }
        }
    }
}
