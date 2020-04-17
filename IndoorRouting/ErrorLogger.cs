// // <copyright file="/Users/nathancastle/Documents/Dev/indoor-routing-xamarin/IndoorRouting/ErrorLogger.cs" company="Esri, Inc">
// //     Copyright (c) Esri. All rights reserved.
// // </copyright>
// // <author>Mara Stoica</author>
using System;
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    public class ErrorLogger
    {
        // TODO - configure your app analytics/crash logging system
        public static ErrorLogger Instance;

        static ErrorLogger()
        {
            // This class doesn't currently maintain state, but you would want
            // to if you were to use something like AppCenter analytics.
            Instance = new ErrorLogger();
        }

        public void LogException(Exception ex)
        {
            // TODO - configure your app analytics/crash logging system here
            System.Diagnostics.Debug.WriteLine(ex);
        }

        public void LogMessage(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
