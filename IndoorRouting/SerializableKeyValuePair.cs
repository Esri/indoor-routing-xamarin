using System;
using System.Collections.Generic;
using System.Text;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting
{
    /// <summary>
    /// Key-Value Pair which, unlike the built-in KVP, can be serialized and deserialized.
    /// </summary>
    /// <typeparam name="TKey">The Key parameter.</typeparam>
    /// <typeparam name="TValue">The Value parameter.</typeparam>
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="key">Key parameter.</param>
        /// <param name="value">Value parameter.</param>
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public TKey Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public TValue Value { get; set; }
    }
}
