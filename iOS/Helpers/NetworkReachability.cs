// <copyright file="NetworkReachability.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>

using System.Net;
using SystemConfiguration;
using CoreFoundation;

/// <summary>
/// Network status.
/// </summary>
internal enum NetworkStatus
{
    /// <summary>
    /// Network is not reacheable
    /// </summary>
    NotReachable,

    /// <summary>
    /// Network is reacheable 
    /// </summary>
    ReachableViaCarrierDataNetwork,

    /// <summary>
    /// Network is reacheable only via WiFi.
    /// </summary>
    ReachableViaWiFiNetwork
}

/// <summary>
/// Reachability class helps determine if device is online. This will be different for every platform. 
/// </summary>
internal static class Reachability
{
    /// <summary>
    /// The default route reachability.
    /// </summary>
    private static NetworkReachability defaultRouteReachability;

    /// <summary>
    /// Test if the network is available.
    /// </summary>
    /// <returns><c>true</c>, if network available was ised, <c>false</c> otherwise.</returns>
    internal static bool IsNetworkAvailable()
    {
        if (defaultRouteReachability == null)
        {
            defaultRouteReachability = new NetworkReachability(new IPAddress(0));
            defaultRouteReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
        }

        NetworkReachabilityFlags flags;

        return defaultRouteReachability.TryGetFlags(out flags) &&
        IsReachableWithoutRequiringConnection(flags);
    }

    /// <summary>
    /// Is the network reachable without requiring connection.
    /// </summary>
    /// <returns><c>true</c>, if reachable without requiring connection, <c>false</c> otherwise.</returns>
    /// <param name="flags">Network Flags.</param>
    private static bool IsReachableWithoutRequiringConnection(NetworkReachabilityFlags flags)
    {
        // Is it reachable with the current network configuration?
        var isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;

        // Do we need a connection to reach it?
        var isConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;

        // Since the network stack will automatically try to get the WAN up,
        // probe that
        if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
        {
            isConnectionRequired = true;
        }

        return isReachable && isConnectionRequired;
    }
}
