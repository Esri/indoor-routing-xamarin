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
    public class Room
    {
        public Geometry.Geometry Geometry { get; set; }

        public string RoomNumber { get; set; }

        public string EmployeeNameLabel { get; set; }

        public bool IsHome { get; set; }

        public bool IsCurrentLocation { get; set; }

        public string Floor { get; set; }

        public MapPoint CenterPoint
        {
            get
            {
                if (IsCurrentLocation)
                {
                    return null;
                }
                switch (Geometry)
                {
                    case MapPoint mp:
                        return mp;
                    case Geometry.Geometry gm:
                        return gm.Extent.GetCenter();
                }

                return null;
            }
        }

        private Room()
        {
        }

        public static Room ConstructFromIdentifyResult(IdentifyLayerResult rawResult)
        {
            GeoElement inputGeoElement = rawResult.GeoElements?.FirstOrDefault();

            if (inputGeoElement == null)
            {
                return null;
            }

            // Get room attribute from the settings. First attribute should be set as the searchable one
            string roomAttributeKey = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
            object roomNumber = inputGeoElement.Attributes[roomAttributeKey];

            if (roomNumber != null)
            {
                string employeeNameLabel = string.Empty;
                if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                {
                    string employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                    employeeNameLabel = inputGeoElement.Attributes[employeeNameAttribute]?.ToString() ?? string.Empty;
                }

                return new Room
                {
                    RoomNumber = roomNumber.ToString(),
                    EmployeeNameLabel = employeeNameLabel,
                    Geometry = inputGeoElement.Geometry,
                    Floor = inputGeoElement.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString()
                };
            }
            return null;
        }

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
                    RoomNumber = roomNumber?.ToString() ?? string.Empty,
                    EmployeeNameLabel = employeeNameLabel,
                    Floor = feature.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString()
                };
            }

            return null;
        }

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
                RoomNumber = AppSettings.CurrentSettings.HomeLocation,
                Floor = AppSettings.CurrentSettings.HomeFloorLevel
            };
        }

        public static Room ConstructCurrentLocation()
        {
            return new Room
            {
                IsCurrentLocation = true,
                RoomNumber = "Current Location"
            };
        }
    }
}