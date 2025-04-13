using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SparkVRTest.Handler
{
    /// <summary>
    /// Manages player names and UI entries in a multiplayer game using Netcode for GameObjects.
    /// </summary>
    public class PlayerManager : NetworkBehaviour
    {
        [SerializeField] private Transform playerListContent;      // Parent transform to hold player UI entries
        [SerializeField] private GameObject playerEntryPrefab;     // Prefab used to represent each player in the UI

        // Stores player names mapped by their clientId
        private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();

        // Stores references to instantiated UI GameObjects for each player
        private Dictionary<ulong, GameObject> playerUIEntries = new Dictionary<ulong, GameObject>();

        /// <summary>
        /// Called when this network object spawns.
        /// Handles player tracking and UI initialization depending on client or server role.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Server listens to connection events
                NetworkManager.Singleton.OnClientConnectedCallback += AddPlayer;
                NetworkManager.Singleton.OnClientDisconnectCallback += RemovePlayer;

                // Add the host player manually
                AddPlayer(NetworkManager.Singleton.LocalClientId);
            }

            // If client (but not the host), request the player list from server
            if (IsClient && !IsHost)
            {
                Debug.Log("Client Joined Updating RPC");
                RequestPlayerListServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        /// <summary>
        /// ServerRpc called by a client to request the current list of players.
        /// Server responds by updating the client's UI.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RequestPlayerListServerRpc(ulong requestingClientId)
        {
            foreach (var player in playerNames)
            {
                UpdatePlayerListClientRpc(player.Key, player.Value);
            }
        }

        /// <summary>
        /// Called when this network object is despawned.
        /// Unsubscribes from callbacks and clears the UI.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= AddPlayer;
                NetworkManager.Singleton.OnClientDisconnectCallback -= RemovePlayer;
            }

            ClearPlayerList();
        }

        /// <summary>
        /// Destroys all player UI entries.
        /// </summary>
        private void ClearPlayerList()
        {
            foreach (var entry in playerUIEntries.Values)
            {
                if (entry != null)
                    Destroy(entry);
            }

            playerUIEntries.Clear();
        }

        /// <summary>
        /// Server-side method to add a player to the player list and notify all clients.
        /// </summary>
        private void AddPlayer(ulong clientId)
        {
            if (playerNames.ContainsKey(clientId)) return;

            string playerName = $"Player {clientId}";
            playerNames[clientId] = playerName;

            // Inform all clients about the new player
            UpdatePlayerListClientRpc(clientId, playerName);
        }

        /// <summary>
        /// Server-side method to remove a player and notify all clients.
        /// </summary>
        private void RemovePlayer(ulong clientId)
        {
            if (playerNames.ContainsKey(clientId))
            {
                playerNames.Remove(clientId);
                RemovePlayerClientRpc(clientId);
            }
        }

        /// <summary>
        /// ClientRpc to update the player list UI with a new or updated player entry.
        /// </summary>
        [ClientRpc]
        private void UpdatePlayerListClientRpc(ulong clientId, string playerName)
        {
            // Update existing entry
            if (playerUIEntries.ContainsKey(clientId))
            {
                playerUIEntries[clientId].GetComponentInChildren<Text>().text = playerName;
                return;
            }

            // Create a new UI entry
            GameObject playerEntry = Instantiate(playerEntryPrefab, playerListContent);
            playerEntry.name = $"Player_{clientId}";
            playerEntry.GetComponentInChildren<TMP_Text>().text = playerName;

            playerUIEntries[clientId] = playerEntry;
        }

        /// <summary>
        /// ClientRpc to remove a player UI entry from the player list.
        /// </summary>
        [ClientRpc]
        private void RemovePlayerClientRpc(ulong clientId)
        {
            if (playerUIEntries.TryGetValue(clientId, out GameObject entry))
            {
                if (entry != null)
                    Destroy(entry);

                playerUIEntries.Remove(clientId);
            }
        }

        /// <summary>
        /// Gets a player's display name from their clientId.
        /// </summary>
        public string GetPlayerName(ulong clientId)
        {
            if (playerNames.TryGetValue(clientId, out string name))
            {
                return name;
            }
            return $"Unknown Player ({clientId})";
        }
    }
}
