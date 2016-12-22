using System;
namespace IndoorNavigation
{
	/// <summary>
	/// Serializable coordinates key value pair.
	/// </summary>
	[Serializable]
	public struct CoordinatesKeyValuePair<K, V>
	{
		public K Key { get; set; }
		public V Value { get; set; }

		public CoordinatesKeyValuePair(K k, V v)
		{
			Key = k;
			Value = v;
		}
	}
}
