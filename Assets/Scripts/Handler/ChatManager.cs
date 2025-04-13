using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SparkVRTest.Handler
{
    /// <summary>
    /// Handles networked chat functionality for all players using Netcode for GameObjects.
    /// Manages input, message sending, and UI display.
    /// </summary>
    public class ChatManager : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField messageInputField;   // Input field for typing messages
        [SerializeField] private Button sendButton;                 // Button to send messages
        [SerializeField] private Transform messageContainer;        // Parent container for message UI elements
        [SerializeField] private GameObject messageEntryPrefab;     // Prefab for each chat message
        [SerializeField] private int maxMessages = 25;              // Max number of messages shown in UI

        private PlayerManager playerManager;                        // Reference to PlayerManager for resolving player names
        private Queue<GameObject> messagePool = new Queue<GameObject>(); // Pool to reuse message UI elements

        /// <summary>
        /// Unity Start method. Sets up references and UI listeners.
        /// </summary>
        private void Start()
        {
            playerManager = FindObjectOfType<PlayerManager>();

            // Attach listeners for send button and Enter key
            sendButton.onClick.AddListener(SendMessage);
            messageInputField.onEndEdit.AddListener(OnInputFieldEndEdit);

            // Disable input until connected to network
            SetInputActive(false);
        }

        /// <summary>
        /// Called when the object is spawned over the network.
        /// Enables the chat input field and button.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            SetInputActive(true);
        }

        /// <summary>
        /// Called when the object is despawned.
        /// Disables the chat input field and button.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            SetInputActive(false);
        }

        /// <summary>
        /// Checks for Enter/Return key press in the input field to trigger message sending.
        /// </summary>
        private void OnInputFieldEndEdit(string message)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendMessage();
            }
        }

        /// <summary>
        /// Called when the send button is pressed or Enter key is used.
        /// Sends the typed message to the server.
        /// </summary>
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(messageInputField.text))
                return;

            ulong senderId = NetworkManager.Singleton.LocalClientId;
            string message = messageInputField.text;

            // Clear and refocus input field
            messageInputField.text = "";
            messageInputField.ActivateInputField();

            // Send message to server for broadcasting
            SendMessageServerRpc(senderId, message);
        }

        /// <summary>
        /// ServerRpc: Receives a message from a client and sends it to all other clients.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void SendMessageServerRpc(ulong senderId, string message)
        {
            string senderName = playerManager.GetPlayerName(senderId);
            BroadcastMessageClientRpc(senderName, message);
        }

        /// <summary>
        /// ClientRpc: Called on all clients to display a received message in the chat UI.
        /// </summary>
        [ClientRpc]
        private void BroadcastMessageClientRpc(string senderName, string message)
        {
            DisplayMessage($"{senderName}: {message}");
        }

        /// <summary>
        /// Displays a message in the UI, using a pool for performance optimization.
        /// </summary>
        private void DisplayMessage(string message)
        {
            GameObject messageEntry;

            // Reuse old message entry if max reached
            if (messagePool.Count > 0 && messageContainer.childCount >= maxMessages)
            {
                messageEntry = messagePool.Dequeue();
                messageEntry.transform.SetAsLastSibling();
            }
            else
            {
                // Instantiate new message UI entry
                messageEntry = Instantiate(messageEntryPrefab, messageContainer);
            }

            // Set message text
            messageEntry.GetComponentInChildren<TMP_Text>().text = message;

            // Pool oldest message if over the limit
            if (messageContainer.childCount > maxMessages)
            {
                GameObject oldestMessage = messageContainer.GetChild(0).gameObject;
                oldestMessage.transform.SetAsLastSibling();
                messagePool.Enqueue(oldestMessage);
            }
        }

        /// <summary>
        /// Enables or disables the message input UI.
        /// </summary>
        private void SetInputActive(bool active)
        {
            messageInputField.interactable = active;
            sendButton.interactable = active;
        }
    }
}
