// <copyright file="Exception.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      https://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class.
    /// </summary>
    [Serializable]
    public class Exception : System.Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Exception"/> class.
        /// </summary>
        public Exception()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Exception"/> class.
        /// </summary>
        /// <param name="message">Exception Message.</param>
        public Exception(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Exception"/> class.
        /// </summary>
        /// <param name="message">Exception Message.</param>
        /// <param name="innerException">Inner exception.</param>
        public Exception(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Exception"/> class.
        /// </summary>
        /// <param name="info">Serialization Info.</param>
        /// <param name="context">Context param.</param>
        protected Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}