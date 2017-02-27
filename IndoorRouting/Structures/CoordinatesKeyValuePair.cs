// <copyright file="CoordinatesKeyValuePair.cs" company="Esri, Inc">
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

    /// <summary>
    /// Coordinates key value pair.
    /// </summary>
    /// <typeparam name="K">The Key parameter.</typeparam>
    /// <typeparam name="V">The Value parameter.</typeparam>
    [Serializable]
    public struct CoordinatesKeyValuePair<K, V>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.CoordinatesKeyValuePair`2"/> struct.
        /// </summary>
        /// <param name="k">Key parameter.</param>
        /// <param name="v">Value parameter.</param>
        public CoordinatesKeyValuePair(K k, V v)
        {
            this.Key = k;
            this.Value = v;
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public K Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public V Value { get; set; }
    }
}
