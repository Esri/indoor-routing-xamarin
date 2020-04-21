// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Linq;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models
{
    /// <summary>
    /// Represents a room that a user has selected, as an origin, destination, or identify result.
    /// This room can represent a home location or the devices current location.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Geometry representing the feature, can be a polygon. Null if representing device's current location.
        /// </summary>
        public Geometry.Geometry Geometry { get; set; }

        /// <summary>
        /// The primary field to display, for example as a title.
        /// </summary>
        public string PrimaryDisplayField { get; set; }

        /// <summary>
        /// The secondary field to display, for example as a subtitle.
        /// </summary>
        public string SecondaryDisplayField { get; set; }

        /// <summary>
        /// True if this is the user's home location.
        /// </summary>
        public bool IsHome { get; set; }

        /// <summary>
        /// True if this is representing the current device location.
        /// </summary>
        public bool IsCurrentLocation { get; set; }

        /// <summary>
        /// The string name of the floor this feature is on.
        /// </summary>
        public string Floor { get; set; }

        /// <summary>
        /// Center point of feature, or null if no geometry is set or <see cref="IsCurrentLocation"/> is <value>true</value>.
        /// </summary>
        public MapPoint CenterPoint
        {
            get
            {
                // If this room represents the current location,
                // the location value should be taken directly from the view's location display.
                if (IsCurrentLocation)
                {
                    return null;
                }
                switch (Geometry)
                {
                    // If the geometry is already a MapPoint, return that, otherwise return its center.
                    case MapPoint mp:
                        return mp;
                    case Geometry.Geometry gm:
                        return gm.Extent.GetCenter();
                }

                return null;
            }
        }

        /// <summary>
        /// Constructor hidden to ensure that only static factory methods are used.
        /// </summary>
        private Room()
        {
        }

        /// <summary>
        /// Creates a room for the first feature in the identify result.
        /// </summary>
        /// <param name="rawResult">IdentifyLayerResult returned by GeoView</param>
        /// <returns>Room for current identify result, or null if nothing is found.</returns>
        public static Room ConstructFromIdentifyResult(IdentifyLayerResult rawResult)
        {
            // Get the first identified element.
            // TODO - adjust this if you have multiple identifiable layers and not all represent rooms
            GeoElement inputGeoElement = rawResult.GeoElements?.FirstOrDefault();

            if (inputGeoElement == null)
            {
                return null;
            }

            // Get room attribute from the settings. First attribute should be set as the searchable one
            string primaryDisplayAttributeKey = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
            object primaryDisplayValue = inputGeoElement.Attributes[primaryDisplayAttributeKey];

            // In the default MMPK and settings, primary display value is the 'Room Number', e.g. 'M3-115'
            if (primaryDisplayValue != null)
            {
                string secondaryDisplayValue = string.Empty;
                if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                {
                    string employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                    secondaryDisplayValue = inputGeoElement.Attributes[employeeNameAttribute]?.ToString() ?? string.Empty;
                }

                return new Room
                {
                    PrimaryDisplayField = primaryDisplayValue.ToString(),
                    SecondaryDisplayField = secondaryDisplayValue,
                    Geometry = inputGeoElement.Geometry,
                    Floor = inputGeoElement.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString()
                };
            }
            return null;
        }

        /// <summary>
        /// Creates a room from a feature.
        /// </summary>
        /// <param name="feature">Feature representing a room.</param>
        /// <returns>Room representing the feature, or null.</returns>
        public static Room ConstructFromFeature(Feature feature)
        {
            if (feature != null)
            {
                // Get room attribute from the settings. First attribute should be set as the searchable one
                var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
                var roomNumber = feature.Attributes[roomAttribute];

                var employeeNameLabel = string.Empty;
                if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                {
                    var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                    var employeeName = feature.Attributes[employeeNameAttribute];
                    employeeNameLabel = employeeName as string ?? string.Empty;
                }

                return new Room
                {
                    Geometry = feature.Geometry,
                    PrimaryDisplayField = roomNumber?.ToString() ?? string.Empty,
                    SecondaryDisplayField = employeeNameLabel,
                    Floor = feature.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString()
                };
            }

            return null;
        }
        
        /// <summary>
        /// Creates a room for the user's home location or null if no home location is set.
        /// </summary>
        /// <returns>Room for the user's home location.</returns>
        public static Room ConstructHome()
        {
            if (string.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation))
            {
                return null;
            }

            double x = 0, y = 0, wkid = 0;

            foreach (var coordinatePart in AppSettings.CurrentSettings.HomeCoordinates)
            {
                switch (coordinatePart.Key)
                {
                    case "X":
                        x = coordinatePart.Value;
                        break;
                    case "Y":
                        y = coordinatePart.Value;
                        break;
                    case "WKID":
                        wkid = coordinatePart.Value;
                        break;
                }
            }

            var homeLocation = new MapPoint(x, y, new SpatialReference((int) wkid));

            return new Room
            {
                IsHome = true,
                Geometry = homeLocation,
                PrimaryDisplayField = AppSettings.CurrentSettings.HomeLocation,
                Floor = AppSettings.CurrentSettings.HomeFloorLevel
            };
        }

        /// <summary>
        /// Creates a room without geometry representing the current device location.
        /// </summary>
        /// <returns>Room representing the device's current location.</returns>
        public static Room ConstructCurrentLocation()
        {
            return new Room
            {
                IsCurrentLocation = true,
                PrimaryDisplayField = "Current Location"
            };
        }
    }
}