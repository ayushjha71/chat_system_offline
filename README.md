# chat_system_offline
For this local multiplayer messaging system project, I chose to implement Unity Transport + Netcode for GameObjects as the networking solution. This approach provides several key advantages for creating a robust LAN-based multiplayer application.

Why Unity Transport + Netcode for GameObjects?

Local Network Discovery
The implementation uses a custom UDP-based network discovery system that allows players to automatically find each other on the same WiFi network without requiring manual IP address input. This enables seamless connection between devices.

Client-Server Architecture
The system uses a traditional client-server model where one player hosts the game (acting as both server and client) while other players connect as clients. This architecture ensures consistent messaging and player state management.

Efficient Message Broadcasting
Netcode for GameObjects provides built-in Remote Procedure Calls (RPCs) that allow efficient message broadcasting from one client to all connected clients. This ensures instant message delivery to all participants.

Easy Player Management
The NetworkManager component handles player connections and disconnections automatically, making it easy to maintain a list of connected players and update the UI accordingly.

Robust Error Handling
The implementation includes comprehensive error handling for network discovery, connection failures, and disconnections, creating a more reliable user experience.

Technical Implementation Highlights

Network Discovery: Custom UDP broadcast system for finding games on the local network
Connection Management: Streamlined connection workflow with visual feedback
Messaging System: Server-validated message broadcasting to prevent cheating/manipulation
UI Integration: Real-time updates of connected players and message history
