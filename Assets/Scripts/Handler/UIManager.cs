using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

namespace SparkVRTest.Handler
{
    /// <summary>
    /// Handles UI interactions for network connection and chat switching.
    /// Manages hosting/joining and transitions between UI panels.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject connectionPanel; // UI panel for connection options
        [SerializeField] private GameObject chatPanel;       // UI panel for the chat interface

        [Header("Connection Buttons")]
        [SerializeField] private Button hostButton;          // Button to start hosting
        [SerializeField] private Button joinButton;          // Button to join an existing host
        [SerializeField] private TMP_Text statusText;        // Text element to show current connection status

        [Header("Network Components")]
        [SerializeField] private NetworkDiscoveryHandler networkDiscoveryHandler; // Reference to LAN discovery handler

        /// <summary>
        /// Initialize UI state and set up button/network callbacks.
        /// </summary>
        private void Start()
        {
            // Show the connection panel initially and hide chat
            connectionPanel.SetActive(true);
            chatPanel.SetActive(false);

            // Hook up button click listeners
            hostButton.onClick.AddListener(OnHostButtonClicked);
            joinButton.onClick.AddListener(OnJoinButtonClicked);

            // Register callbacks for network events
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        /// <summary>
        /// Clean up network callbacks when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        /// <summary>
        /// Triggered when the user clicks the "Host" button.
        /// Starts hosting and switches to the chat panel.
        /// </summary>
        private void OnHostButtonClicked()
        {
            networkDiscoveryHandler.StartHost();
            SetStatus("Hosting...");

            // Immediately show chat panel since host connects instantly
            SwitchToChatPanel();
        }

        /// <summary>
        /// Triggered when the user clicks the "Join" button.
        /// Starts searching for LAN games.
        /// </summary>
        private void OnJoinButtonClicked()
        {
            networkDiscoveryHandler.JoinGame();
            SetStatus("Searching for games...");

            // Disable interaction to prevent multiple clicks
            hostButton.interactable = false;
            joinButton.interactable = false;
        }

        /// <summary>
        /// Called when any client connects. Switches UI for local client.
        /// </summary>
        private void OnClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                SetStatus("Connected!");
                SwitchToChatPanel();
            }
        }

        /// <summary>
        /// Called when any client disconnects. Handles local client disconnect.
        /// </summary>
        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                SetStatus("Disconnected");
                SwitchToConnectionPanel();

                // Re-enable buttons for retry
                hostButton.interactable = true;
                joinButton.interactable = true;
            }
        }

        /// <summary>
        /// Switches from connection panel to chat panel.
        /// </summary>
        public void SwitchToChatPanel()
        {
            connectionPanel.SetActive(false);
            chatPanel.SetActive(true);
        }

        /// <summary>
        /// Switches from chat panel to connection panel.
        /// </summary>
        public void SwitchToConnectionPanel()
        {
            connectionPanel.SetActive(true);
            chatPanel.SetActive(false);
        }

        /// <summary>
        /// Updates the status message shown in the UI.
        /// </summary>
        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
