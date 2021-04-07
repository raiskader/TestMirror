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

        public class PlayerEvent : UnityEvent<string> { }

        /// <summary>
        /// Called when Server Starts
        /// </summary>
        public event Action onServerStarted;

        /// <summary>
        /// Called when Server Stops
        /// </summary>
        public event Action onServerStopped;

        /// <summary>
        /// Called when players leaves or joins the room
        /// </summary>
        public event OnRecieveAuthenticate onRecieveAuthenticate;

        public delegate void OnRecieveAuthenticate(NetworkConnection conn, string playFabID);

        public override void Awake()
        {
            configuration = FindObjectOfType<Configuration>();

            base.Awake();
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            Debug.LogWarning("Client Connected");
            MirrorNetworkConnection connection = _connections.Find(c => c.ConnectionId == conn.connectionId);
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

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);

            MirrorNetworkConnection connection = _connections.Find(c => c.ConnectionId == conn.connectionId);
            if (connection != null)
            {
                if (!string.IsNullOrEmpty(connection.PlayFabId))
                {
                    OnPlayerRemoved.Invoke(connection.PlayFabId);
                }
                _connections.Remove(connection);
            }
        }

        public override void OnStartServer()
        {
            onServerStarted?.Invoke();
        }

        public override void OnStopServer()
        {
            onServerStopped?.Invoke();
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

    public struct ShutdownMessage : NetworkMessage
    {
        public string message;
    }

    [Serializable]
    public struct MaintenanceMessage : NetworkMessage
    {
        public string message;
        public DateTime ScheduledMaintenanceUTC;

        public void Deserialize(NetworkReader reader)
        {
            var json = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            ScheduledMaintenanceUTC = json.DeserializeObject<DateTime>(reader.ReadString());
        }

        public void Serialize(NetworkWriter writer)
        {
            var json = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            var str = json.SerializeObject(ScheduledMaintenanceUTC);
            writer.Write(str);
        }
    }
}

