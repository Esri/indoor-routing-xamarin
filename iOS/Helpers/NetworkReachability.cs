using System.Net;
using SystemConfiguration;
using CoreFoundation;

enum NetworkStatus
{
	NotReachable,
	ReachableViaCarrierDataNetwork,
	ReachableViaWiFiNetwork
}

/// <summary>
/// Reachability class helps determine if device is online. This will be different for every platform. 
/// </summary>
static class Reachability
{
	static NetworkReachability _defaultRouteReachability;


	internal static bool IsNetworkAvailable()
	{
		if (_defaultRouteReachability == null)
		{
			_defaultRouteReachability = new NetworkReachability(new IPAddress(0));
			_defaultRouteReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
		}

		NetworkReachabilityFlags flags;

		return _defaultRouteReachability.TryGetFlags(out flags) &&
			IsReachableWithoutRequiringConnection(flags);
	}

	/// <summary>
	/// Is the network reachable without requiring connection.
	/// </summary>
	/// <returns><c>true</c>, if reachable without requiring connection, <c>false</c> otherwise.</returns>
	/// <param name="flags">Flags.</param>
	static bool IsReachableWithoutRequiringConnection(NetworkReachabilityFlags flags)
	{
		// Is it reachable with the current network configuration?
		var isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;

		// Do we need a connection to reach it?
		var noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;

		// Since the network stack will automatically try to get the WAN up,
		// probe that
		if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
			noConnectionRequired = true;

		return isReachable && noConnectionRequired;
	}

}

