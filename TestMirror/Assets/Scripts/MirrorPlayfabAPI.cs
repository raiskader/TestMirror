
namespace PlayFab.Networking
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using Mirror;
    using kcp2k;

    public class MirrorPlayfabAPI : MonoBehaviour
    {
        public Configuration configuration;

        [SerializeField] MirrorNetworkServer manager;

        public string gameName = "Game";

        void Awake()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<MirrorNetworkServer>();
            }
            Debug.Assert(manager != null, "MirrorPlayfabAPI could not find MirrorNetworkServer");

            if (configuration.buildType == BuildType.REMOTE_SERVER)
            {
                manager.onRecieveAuthenticate += RecieveAuthenticateHandler;
                manager.onServerStarted += ServerStartedHandler;
                manager.onServerStopped += ServerStoppedHandler;
            }
        }

        void RecieveAuthenticateHandler(NetworkConnection conn, string playFabID)
        {
            MirrorNetworkConnection connection = manager.Connections.Find(c => c.ConnectionId == conn.connectionId);
            if (connection != null)
            {
                connection.PlayFabId = playFabID;
                connection.IsAuthenticated = true;
                manager.OnPlayerAdded.Invoke(playFabID);
            }
        }

        void ServerStartedHandler()
        {
            NetworkServer.Listen(GetComponent<KcpTransport>().Port);
        }

        void ServerStoppedHandler()
        {
            NetworkServer.Shutdown();
        }
    }
}
