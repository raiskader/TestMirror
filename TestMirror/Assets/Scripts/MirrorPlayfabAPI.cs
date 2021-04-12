
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
                manager.onServerStarted += StartServer;
                manager.onServerStopped += OnApplicationQuit;
            }
        }

        void StartServer()
        {
            NetworkServer.Listen(GetComponent<KcpTransport>().Port);
        }

        void OnApplicationQuit()
        {
            NetworkServer.Shutdown();
        }
    }
}
