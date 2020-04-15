// // <copyright file="/Users/nathancastle/Documents/Dev/indoor-routing-xamarin/IndoorRouting/Models/IdentifiedRoom.cs" company="Esri, Inc">
// //     Copyright (c) Esri. All rights reserved.
// // </copyright>
// // <author>Mara Stoica</author>
using System.Linq;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models
{
    public class IdentifiedRoom
    {
        public Geometry.Geometry Geometry { get; set; }

        public MapPoint CenterPoint
        {
            get
            {
                if (IsCurrentLocation)
                {
                    return null;
                }
                else if (Geometry is MapPoint mp)
                {
                    return mp;
                }
                else if (Geometry is Geometry.Geometry gm)
                {
                    return gm.Extent.GetCenter();
                }
                return null;
            }
        }

        public string RoomNumber { get; set; }

        public string EmployeeNameLabel { get; set; }

        public bool IsHome { get; set; } = false;

        public bool IsCurrentLocation { get; set; } = false;

        public string Floor { get; set; }

        private IdentifiedRoom() { }

        public static IdentifiedRoom ConstructFromIdentifyResult(IdentifyLayerResult rawResult)
        {
            GeoElement inputGeoElement = rawResult.GeoElements?.FirstOrDefault();

            if (inputGeoElement == null)
            {
                return null;
            }

            // Get room attribute from the settings. First attribute should be set as the searcheable one
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

                return new IdentifiedRoom {
                    RoomNumber = roomNumber.ToString(),
                    EmployeeNameLabel = employeeNameLabel,
                    Geometry = inputGeoElement.Geometry,
                    Floor = inputGeoElement.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString()
                };
            }
            else
            {
                return null;
            }
        }

        public static IdentifiedRoom ConstructFromFeature(Feature feature)
        {
            if (feature != null)
            {
                // Get room attribute from the settings. First attribute should be set as the searcheable one
                var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
                var roomNumber = feature.Attributes[roomAttribute];

                var employeeNameLabel = string.Empty;
                if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                {
                    var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                    var employeeName = feature.Attributes[employeeNameAttribute];
                    employeeNameLabel = employeeName as string ?? string.Empty;
                }

                return new IdentifiedRoom
                {
                    Geometry = feature.Geometry,
                    RoomNumber = roomNumber?.ToString() ?? string.Empty,
                    EmployeeNameLabel = employeeNameLabel.ToString(),
                    Floor = feature.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName].ToString()
                };
            }
            return null;
        }

        public static IdentifiedRoom ConstructHome()
        {
            if (string.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation))
            {
                return null;
            }
            else
            {
                double x = 0, y = 0, wkid = 0;

                for (int i = 0; i < AppSettings.CurrentSettings.HomeCoordinates.Length; i++)
                {
                    switch (AppSettings.CurrentSettings.HomeCoordinates[i].Key)
                    {
                        case "X":
                            x = AppSettings.CurrentSettings.HomeCoordinates[i].Value;
                            break;
                        case "Y":
                            y = AppSettings.CurrentSettings.HomeCoordinates[i].Value;
                            break;
                        case "WKID":
                            wkid = AppSettings.CurrentSettings.HomeCoordinates[i].Value;
                            break;
                        default:
                            break;
                    }
                }

                var homeLocation = new MapPoint(x, y, new SpatialReference((int)wkid));

                return new IdentifiedRoom
                {
                    IsHome = true,
                    Geometry = homeLocation,
                    RoomNumber = AppSettings.CurrentSettings.HomeLocation,
                    Floor = AppSettings.CurrentSettings.HomeFloorLevel
                };
            }
        }

        public static IdentifiedRoom ConstructCurrentLocation()
        {
            return new IdentifiedRoom
            {
                IsCurrentLocation = true,
                RoomNumber = "Current Location"
            };
        }
    }
}
