using System;
using System.Net;
using System.Text;
using UnityEngine;
using SparkVRTest.Utils;
using System.Net.Sockets;
using SparkVRTest.Handler;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

namespace SparkVRTest.Networking
{
    [Serializable]
    public class ServerData
    {
        public string Address;
        public ushort Port;
        public string ServerName;
    }

    public class NetworkDiscovery : MonoBehaviour
    {
        [SerializeField] private NetworkDiscoveryHandler discoveryHandler;
        [SerializeField] private string serverName = "Local Game"; // Name displayed when a server is found
        [SerializeField] private int discoveryPort = 47777; // UDP port for discovery

        private UdpClient udpClient;
        private IPEndPoint broadcastEndPoint;
        private bool isRunning = false;
        private UnityTransport transport;

        private void Awake()
        {
            // Ensure UnityMainThreadDispatcher exists in the scene for running actions on the main thread
            if (FindObjectOfType<UnityMainThreadDispatcher>() == null)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }

        private void Start()
        {
            // Cache UnityTransport component to retrieve server port
            transport = GetComponent<UnityTransport>();
        }

        /// <summary>
        /// Starts the server-side discovery system.
        /// Listens for client broadcast messages and responds with server info.
        /// </summary>
        public void StartServer()
        {
            if (isRunning) return;

            try
            {
                udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;

                // Start listening for discovery messages
                Task.Run(ServerListenAsync);
                isRunning = true;

                Debug.Log("Network discovery server started");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start network discovery server: {e.Message}");
            }
        }

        /// <summary>
        /// Starts the client-side discovery system.
        /// Sends broadcast messages and listens for server responses.
        /// </summary>
        public void StartClient()
        {
            if (isRunning) return;

            try
            {
                udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;

                // Bind to any available port
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

                // Setup broadcast endpoint to send discovery requests
                broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

                // Send discovery request and listen for responses
                Task.Run(ClientSendDiscoveryRequestsAsync);
                Task.Run(ClientListenForServerResponseAsync);

                isRunning = true;
                Debug.Log("Network discovery client started");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start network discovery client: {e.Message}");
            }
        }

        /// <summary>
        /// Stops the discovery system for both server and client.
        /// </summary>
        public void StopDiscovery()
        {
            if (!isRunning) return;

            isRunning = false;

            try
            {
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
                Debug.Log("Network discovery stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping network discovery: {e.Message}");
            }
        }

        /// <summary>
        /// Server loop that listens for "DISCOVER_UNITY_SERVER" requests and sends server info back.
        /// </summary>
        private async Task ServerListenAsync()
        {
            try
            {
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));

                while (isRunning)
                {
                    var result = await udpClient.ReceiveAsync();
                    string message = Encoding.ASCII.GetString(result.Buffer);

                    if (message == "DISCOVER_UNITY_SERVER")
                    {
                        var serverData = new ServerData
                        {
                            Address = GetLocalIPAddress(),
                            Port = transport.ConnectionData.Port,
                            ServerName = serverName
                        };

                        Debug.Log($"Sending server info: {serverData.Address}:{serverData.Port}");

                        string serializedData = JsonUtility.ToJson(serverData);
                        byte[] responseBytes = Encoding.ASCII.GetBytes(serializedData);

                        await udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Debug.Log("UDP client was closed");
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Network discovery server error: {e.Message}");
            }
        }

        /// <summary>
        /// Sends multiple discovery requests over broadcast from the client side.
        /// </summary>
        private async Task ClientSendDiscoveryRequestsAsync()
        {
            try
            {
                byte[] requestData = Encoding.ASCII.GetBytes("DISCOVER_UNITY_SERVER");

                // Retry a few times in case of missed packets
                for (int i = 0; i < 3 && isRunning; i++)
                {
                    await udpClient.SendAsync(requestData, requestData.Length, broadcastEndPoint);
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Error sending discovery request: {e.Message}");
            }
        }

        /// <summary>
        /// Listens for server responses on the client side and triggers the discovery handler.
        /// </summary>
        private async Task ClientListenForServerResponseAsync()
        {
            try
            {
                while (isRunning)
                {
                    var result = await udpClient.ReceiveAsync();
                    string jsonData = Encoding.ASCII.GetString(result.Buffer);

                    try
                    {
                        ServerData serverData = JsonUtility.FromJson<ServerData>(jsonData);
                        Debug.Log($"Received server info: {serverData.Address}:{serverData.Port}");

                        // Ensure handler is called on the main Unity thread
                        UnityMainThreadDispatcher.RunOnMainThread(() =>
                        {
                            discoveryHandler.OnServerFound(serverData);
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing server data: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                    Debug.LogError($"Network discovery client error: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            StopDiscovery();
        }

        /// <summary>
        /// Returns the first local IPv4 address of the host.
        /// </summary>
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}
