using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.Networking;
using System.Collections.Generic;
using PlayFab.MultiplayerAgent.Model;
using Mirror;

public class ServerStartup : MonoBehaviour
{
    public Configuration configuration;

    private List<ConnectedPlayer> _connectedPlayers;
    public MirrorNetworkServer MirrorServer;

    void Start()
    {
        if (configuration.buildType == BuildType.REMOTE_SERVER)
        {
            StartRemoteServer();
        }
    }

    public void OnStartLocalServerButtonClick()
    {
        if (configuration.buildType == BuildType.LOCAL_SERVER)
        {
            MirrorServer.StartServer();
        }
    }

    private void StartRemoteServer()
    {
        Debug.Log("[ServerStartUp].StartRemoteServer");
        _connectedPlayers = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = configuration.playFabDebugging;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        MirrorServer.OnPlayerAdded.AddListener(OnPlayerAdded);
        MirrorServer.OnPlayerRemoved.AddListener(OnPlayerRemoved);

        StartCoroutine(ReadyForPlayers());
        StartCoroutine(ShutdownServerInXTime());
    }

    IEnumerator ShutdownServerInXTime()
    {
        yield return new WaitForSeconds(300f);
        StartShutdownProcess();
    }

    IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }

    private void OnServerActive()
    {
        MirrorServer.StartServer();
        Debug.Log("Server Started From Agent Activation");
    }

    private void OnPlayerRemoved(string playfabId)
    {
        ConnectedPlayer player = _connectedPlayers.Find(x => x.PlayerId.Equals(playfabId, StringComparison.OrdinalIgnoreCase));
        _connectedPlayers.Remove(player);
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
        CheckPlayerCountToShutdown();
    }

    private void CheckPlayerCountToShutdown()
    {
        if (_connectedPlayers.Count <= 0)
        {
            StartShutdownProcess();
        }
    }

    private void OnPlayerAdded(string playfabId)
    {
        _connectedPlayers.Add(new ConnectedPlayer(playfabId));
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        StartShutdownProcess();
    }

    private void StartShutdownProcess()
    {
        

        Debug.Log("Server is shutting down");
        foreach (var conn in MirrorServer.Connections)
        {
            conn.Connection.Send(CreateShutDownMessage());
        }
        StartCoroutine(ShutdownServer());
    }

    IEnumerator ShutdownServer()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.LogFormat("Maintenance scheduled for: {0}", NextScheduledMaintenanceUtc.Value.ToLongDateString());
        foreach (var conn in MirrorServer.Connections)
        {
            conn.Connection.Send(CreateMaintenanceMessage(NextScheduledMaintenanceUtc));
        }
    }

    private ShutdownMessage CreateShutDownMessage()
    {
        ShutdownMessage shutdownMessage = new ShutdownMessage
        {
            message = "Server is Shutting Down"
        };
        return shutdownMessage;
    }

    private MaintenanceMessage CreateMaintenanceMessage(DateTime? NextScheduledMaintenanceUtc)
    {
        MaintenanceMessage maintenanceMessage = new MaintenanceMessage
        {
            message = "Server is Shutting Down for maintenance",
            ScheduledMaintenanceUTC = (DateTime)NextScheduledMaintenanceUtc
        };
        return maintenanceMessage;
    }
}
