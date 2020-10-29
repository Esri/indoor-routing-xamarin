// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// https://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    /// <summary>
    /// Class implements shared error logging functionality. Edit this to integrate your own app analytics (e.g. AppCenter).
    /// </summary>
    public class ErrorLogger
    {
        public static ErrorLogger Instance;

        static ErrorLogger()
        {
            // This class doesn't currently maintain state, but you would want
            // to if you were to use something like AppCenter analytics.
            Instance = new ErrorLogger();
        }

        /// <summary>
        /// Logs the exception to the console.
        /// </summary>
        /// <param name="ex"></param>
        public void LogException(Exception ex)
        {
            // TODO - configure your app analytics/crash logging system here
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
