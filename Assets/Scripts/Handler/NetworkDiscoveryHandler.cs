using TMPro;
using UnityEngine;
using Unity.Netcode;
using SparkVRTest.Networking;
using Unity.Netcode.Transports.UTP;

namespace SparkVRTest.Handler
{
    /// <summary>
    /// Handles LAN game discovery and connection logic using Unity Transport and custom NetworkDiscovery.
    /// </summary>
    public class NetworkDiscoveryHandler : MonoBehaviour
    {
        public UnityTransport unityTransport; // Unity Transport component for setting connection data
        [SerializeField] private TMP_Text statusText; // UI text to show network connection status

        private NetworkManager networkManager; // Reference to the NetworkManager
        private NetworkDiscovery networkDiscovery; // Custom network discovery component
        private bool isConnecting = false; // Flag to prevent multiple simultaneous connection attempts

        /// <summary>
        /// Initialize components and register network callbacks.
        /// </summary>
        private void Start()
        {
            networkManager = GetComponent<NetworkManager>();
            networkDiscovery = GetComponent<NetworkDiscovery>();
            unityTransport = GetComponent<UnityTransport>();

            // Register network events
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnect;
            networkManager.OnTransportFailure += OnTransportFailure;
        }

        /// <summary>
        /// Unregister network callbacks to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
                networkManager.OnTransportFailure -= OnTransportFailure;
            }
        }

        /// <summary>
        /// Starts the host and makes it discoverable on the local network.
        /// </summary>
        public void StartHost()
        {
            Debug.Log($"Host starting with port: {unityTransport.ConnectionData.Port}");

            bool success = networkManager.StartHost();
            if (success)
            {
                Debug.Log("Host started successfully");
                networkDiscovery.StartServer(); // Advertise the server over LAN
            }
            else
            {
                Debug.LogError("Failed to start host");
                UpdateStatus("Failed to start host");
            }
        }

        /// <summary>
        /// Initiates search for LAN games using discovery broadcast.
        /// </summary>
        public void JoinGame()
        {
            networkDiscovery.StartClient();
            UpdateStatus("Searching for games...");
        }

        /// <summary>
        /// Called when a server is found via network discovery.
        /// </summary>
        public void OnServerFound(ServerData serverData)
        {
            if (isConnecting) return; // Prevent duplicate connection attempts

            Debug.Log($"Server found at {serverData.Address}:{serverData.Port}");
            UpdateStatus($"Server found: {serverData.ServerName}");

            // Set target connection address and port
            unityTransport.ConnectionData.Address = serverData.Address;
            unityTransport.ConnectionData.Port = serverData.Port;

            Debug.Log($"Attempting to connect to {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}");

            isConnecting = true;
            Invoke("ConnectToServer", 0.5f); // Slight delay before attempting connection
        }

        /// <summary>
        /// Connects to the discovered server as a client.
        /// </summary>
        private void ConnectToServer()
        {
            networkDiscovery.StopDiscovery(); // Stop listening for more servers

            bool success = networkManager.StartClient();
            if (!success)
            {
                Debug.LogError("Failed to connect to server");
                UpdateStatus("Connection failed");
                isConnecting = false;
            }
            else
            {
                UpdateStatus("Connecting...");
            }
        }

        /// <summary>
        /// Callback when a client connects to a server.
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");
            UpdateStatus("Connected!");
            isConnecting = false;
        }

        /// <summary>
        /// Callback when a client disconnects from a server.
        /// </summary>
        private void OnClientDisconnect(ulong clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");
            UpdateStatus("Disconnected");
            isConnecting = false;
        }

        /// <summary>
        /// Handles low-level transport failure (e.g., network dropout).
        /// </summary>
        private void OnTransportFailure()
        {
            Debug.LogError("Transport failure occurred");
            UpdateStatus("Connection failed: Transport error");
            isConnecting = false;
        }

        /// <summary>
        /// Updates the status UI text and logs it to the console.
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log($"Status: {message}");
        }
    }
}
