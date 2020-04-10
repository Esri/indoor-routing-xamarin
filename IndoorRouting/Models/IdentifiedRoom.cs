// // <copyright file="/Users/nathancastle/Documents/Dev/indoor-routing-xamarin/IndoorRouting/Models/IdentifiedRoom.cs" company="Esri, Inc">
// //     Copyright (c) Esri. All rights reserved.
// // </copyright>
// // <author>Mara Stoica</author>
using System;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models
{
    public class IdentifiedRoom
    {
        private IdentifiedRoom() { }

        public Geometry.Geometry FeatureLocation { get; private set; }

        public string RoomNumber { get; private set; }

        public string EmployeeNameLabel { get; private set; }

        public bool IsHome { get; private set; } = false;

        public bool IsCurrentLocation { get; private set; } = false;

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
                    string employeeName = inputGeoElement.Attributes[employeeNameAttribute].ToString();
                    employeeNameLabel = employeeName as string ?? string.Empty;
                }

                return new IdentifiedRoom {
                    RoomNumber = roomNumber.ToString(),
                    EmployeeNameLabel = employeeNameLabel,
                    FeatureLocation = inputGeoElement.Geometry };
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
                    FeatureLocation = feature.Geometry,
                    RoomNumber = roomNumber?.ToString() ?? string.Empty,
                    EmployeeNameLabel = employeeNameLabel.ToString()
                };
            }
            return null;
        }

        public static async Task<IdentifiedRoom> ConstructHome()
        {
            // Get the feature to populate the Contact Card
            var roomFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(AppSettings.CurrentSettings.HomeLocation);

            IdentifiedRoom room = ConstructFromFeature(roomFeature);
            room.IsHome = true;

            return room;
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
