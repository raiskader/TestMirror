namespace PlayFab.Networking
{
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using Mirror;
    using UnityEngine.Events;

    public class MirrorNetworkServer : NetworkManager
    {
        public Configuration configuration;

        public PlayerEvent OnPlayerAdded = new PlayerEvent();
        public PlayerEvent OnPlayerRemoved = new PlayerEvent();

        public List<MirrorNetworkConnection> Connections
        {
            get { return _connections; }
            private set { _connections = value; }
        }
        private List<MirrorNetworkConnection> _connections = new List<MirrorNetworkConnection>();

        public class PlayerEvent : UnityEvent<string> 
        {
        }

        /// <summary>
        /// Called when Server Starts
        /// </summary>
        public event Action onServerStarted;

        /// <summary>
        /// Called when Server Stops
        /// </summary>
        public event Action onServerStopped;

        public override void Awake()
        {
            configuration = FindObjectOfType<Configuration>();

            if (configuration.buildType == BuildType.REMOTE_SERVER)
            {
                AddRemoteServerListeners();
            }

            base.Awake();
        }

        private void AddRemoteServerListeners()
        {
            Debug.Log("[UnityNetworkServer].AddRemoteServerListeners");
            NetworkServer.RegisterHandler<OnServerConnectMessage>(OnMirrorServerConnect);
            NetworkServer.RegisterHandler<OnServerDisconnectMessage>(OnMirrorServerDisconnect);
            NetworkServer.RegisterHandler<OnServerErrorMessage>(OnServerError);
            NetworkServer.RegisterHandler<ReceiveAuthenticateMessage>(OnReceiveAuthenticate);
        }

        private void OnServerError(NetworkConnection arg1, OnServerErrorMessage arg2)
        {
            try
            {
                // todo
                Debug.Log("Unity Network Connection Status: code ");
            }
            catch (Exception)
            {
                Debug.Log("Unity Network Connection Status, but we could not get the reason, check the Unity Logs for more info.");
            }
        }


        public struct OnServerErrorMessage : NetworkMessage
        {
        }

        public void OnMirrorServerConnect(NetworkConnection conn, OnServerConnectMessage message)
        {
            OnServerConnect(conn);
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            Debug.LogWarning("Client Connected");
            var connection = _connections.Find(c => c.ConnectionId == conn.connectionId);
            if (connection == null)
            {
                _connections.Add(new MirrorNetworkConnection()
                {
                    Connection = conn,
                    ConnectionId = conn.connectionId,
                    LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig.SessionId
                });
            }
        } 

        public struct OnServerConnectMessage : NetworkMessage
        {
        }

        public void OnMirrorServerDisconnect(NetworkConnection conn, OnServerDisconnectMessage message)
        {
            OnServerDisconnect(conn);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            var connection = _connections.Find(c => c.ConnectionId == conn.connectionId);
            if (connection != null)
            {
                if (!string.IsNullOrEmpty(connection.PlayFabId))
                {
                    OnPlayerRemoved.Invoke(connection.PlayFabId);
                }
                _connections.Remove(connection);
            }

            //base.OnServerDisconnect(conn);
        }

        public struct OnServerDisconnectMessage : NetworkMessage
        {
        }

        public override void OnStartServer()
        {
            onServerStarted?.Invoke();
        }

        public override void OnStopServer()
        {
            onServerStopped?.Invoke();
        }

        private void OnReceiveAuthenticate(NetworkConnection conn, ReceiveAuthenticateMessage message)
        {
            var connection = _connections.Find(c => c.ConnectionId == conn.connectionId);
            if (connection != null)
            {
                connection.PlayFabId = message.PlayFabId;
                connection.IsAuthenticated = true;
                OnPlayerAdded.Invoke(message.PlayFabId);
            }
        }
    }

    [Serializable]
    public class MirrorNetworkConnection
    {
        public bool IsAuthenticated;
        public string PlayFabId;
        public string LobbyId;
        public int ConnectionId;
        public NetworkConnection Connection;
    }

    /*public class CustomGameServerMessageTypes
    {
        public const short ReceiveAuthenticate = 900;
        public const short ShutdownMessage = 901;
        public const short MaintenanceMessage = 902;
    }*/

    public struct ReceiveAuthenticateMessage : NetworkMessage
    {
        public string PlayFabId;
    }

    public struct ShutdownMessage : NetworkMessage
    {
    }

    [Serializable]
    public struct MaintenanceMessage : NetworkMessage
    {
        public DateTime ScheduledMaintenanceUTC;
    }
}

