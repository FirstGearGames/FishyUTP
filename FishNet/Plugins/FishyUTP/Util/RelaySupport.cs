﻿using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace FishNet.Transporting.FishyUTPPlugin
{
    /// <summary>
    /// Static container for helping understand Relay allocations. This uses code from the `RelayAllocUtp.cs` file
    /// from the Unity Relay Samples.
    /// </summary>
    public static class RelaySupport
    {
        private const string RelayUDPNetworkOption = "udp";
        
        public static RelayServerData PlayerRelayData(JoinAllocation allocation)
        {
            // Select endpoint based on desired connectionType
            var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, RelayUDPNetworkOption);
            if (endpoint == null)
            {
                throw new Exception($"endpoint for connectionType udp not found");
            }

            // Prepare the server endpoint using the Relay server IP and port
            var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
            var key = ConvertFromHMAC(allocation.Key);

            // Prepare the Relay server data and compute the nonce values
            // A player joining the host passes its own connectionData as well as the host's
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref hostConnectionData, ref key, false);
            relayServerData.ComputeNewNonce();

            return relayServerData;
        }
        
        public static RelayServerData HostRelayData(Allocation allocation)
        {
            var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, RelayUDPNetworkOption);
            if (endpoint == null)
            {
                throw new Exception($"endpoint for connectionType UDP not found");
            }

            // Prepare the server endpoint using the Relay server IP and port
            var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var key = ConvertFromHMAC(allocation.Key);

            // Prepare the Relay server data and compute the nonce value
            // The host passes its connectionData twice into this function
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref connectionData, ref key, false);
            relayServerData.ComputeNewNonce();

            return relayServerData;
        }


        private static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.ConnectionType == connectionType)
                {
                    return endpoint;
                }
            }

            return null;
        }
        
        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }
    }
}