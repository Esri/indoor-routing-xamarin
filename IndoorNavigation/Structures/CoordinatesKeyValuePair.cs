// <copyright file="CoordinatesKeyValuePair.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation
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
