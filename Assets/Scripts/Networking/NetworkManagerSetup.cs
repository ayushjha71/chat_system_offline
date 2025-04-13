using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace SparkVRTest.Networking
{
    /// <summary>
    /// Configures the Unity Transport settings for the NetworkManager at runtime.
    /// Useful for setting port, address, and registering network callbacks for logging/debugging.
    /// </summary>
    public class NetworkManagerSetup : MonoBehaviour
    {
        [SerializeField] private ushort port = 7777; // Default port to use for hosting or connecting

        void Awake()
        {
            // Get required components
            NetworkManager networkManager = GetComponent<NetworkManager>();
            UnityTransport transport = GetComponent<UnityTransport>();

            if (transport != null)
            {
                // Configure transport with custom port
                transport.ConnectionData.Port = port;

                // Listen on all network interfaces (0.0.0.0) for LAN hosting
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";

                Debug.Log($"Network transport configured with port: {port}");
            }
            else
            {
                Debug.LogError("UnityTransport component not found!");
            }

            // Register to network manager events for logging/debugging purposes
            if (networkManager != null)
            {
                // Called when the server successfully starts
                networkManager.OnServerStarted += () =>
                    Debug.Log("Server started");

                // Called when a client connects
                networkManager.OnClientConnectedCallback += (id) =>
                    Debug.Log($"Client connected: {id}");

                // Called when a client disconnects
                networkManager.OnClientDisconnectCallback += (id) =>
                    Debug.Log($"Client disconnected: {id}");

                // Called when the server stops (optional, only available in some versions)
                networkManager.OnServerStopped += (reason) =>
                    Debug.Log($"Server stopped: {reason}");
            }
        }
    }
}
