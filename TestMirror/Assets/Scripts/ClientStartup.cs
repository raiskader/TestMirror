using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using Mirror;
using kcp2k;

public class ClientStartup : MonoBehaviour
{
    public Configuration configuration;
    public ServerStartup serverStartUp;
    public NetworkManager networkManager;

    public TelepathyTransport telepathyTransport;
    public KcpTransport kcpTransport;

    public void OnLoginUserButtonClick()
    {
        if (configuration.buildType == BuildType.REMOTE_CLIENT)
        {
            if (configuration.buildId == "")
            {
                throw new Exception("A remote client build must have a buildId. Add it to the Configuration. Get this from your Multiplayer Game Manager in the PlayFab web console.");
            }
            else
            {
                LoginRemoteUser();
            }
        }
        else if (configuration.buildType == BuildType.LOCAL_CLIENT)
        {
            SetupTransport();

            networkManager.StartClient();
        }
    }

    private static void SetupTransport()
    {
        var ui = FindObjectOfType<LoginPanelUI>();
        var inputFieldIpAndPort = ui.ipAndPort;

        var ipAndPortText = inputFieldIpAndPort.text;

        if (string.IsNullOrEmpty(ipAndPortText))
        {
            Debug.Log($"[ClientStartUp.OnLoginUserButtonClick] Input field is empty!");
            return;
        }

        var ipAddress = ipAndPortText.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
        if (ipAddress.Length != 2)
        {
            Debug.LogError($"[ClientStartUp.OnLoginUserButtonClick] Wrong IP:Port configuration!");
            return;
        }

        var ip = ipAddress[0];
        var port = ipAddress[1];

        var kcpTransport = FindObjectOfType<KcpTransport>();
        if (kcpTransport == null)
        {
            Debug.LogError($"[ClientStartUp.OnLoginUserButtonClick] Wrong IP:Port configuration!");
            return;
        }

        ushort.TryParse(port, out var portUshort);
        kcpTransport.Port = portUshort;
        NetworkManager.singleton.networkAddress = ip;
    }

    public void LoginRemoteUser()
    {
        Debug.Log("[ClientStartUp].LoginRemoteUser");

        //We need to login a user to get at PlayFab API's. 
        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CreateAccount = true,
            CustomId = GUIDUtility.getUniqueID()
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnPlayFabLoginSuccess, OnLoginError);
    }

    private void OnLoginError(PlayFabError response)
    {
        Debug.Log(response.ToString());
    }

    private void OnPlayFabLoginSuccess(LoginResult response)
    {
        Debug.Log(response.ToString());
        if (configuration.ipAddress == "")
        {   //We need to grab an IP and Port from a server based on the buildId. Copy this and add it to your Configuration.
            RequestMultiplayerServer();
        }
        else
        {
            ConnectRemoteClient();
        }
    }

    private void RequestMultiplayerServer()
    {
        Debug.Log("[ClientStartUp].RequestMultiplayerServer");
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();
        requestData.BuildId = configuration.buildId;
        requestData.SessionId = System.Guid.NewGuid().ToString();
        requestData.PreferredRegions = new List<string>() { AzureRegion.EastUs.ToString(), AzureRegion.NorthEurope.ToString() };
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
    {
        Debug.Log(response.ToString());
        ConnectRemoteClient(response);
    }

    private void ConnectRemoteClient(RequestMultiplayerServerResponse response = null)
    {
        if (response == null)
        {
            networkManager.networkAddress = configuration.ipAddress;
            telepathyTransport.port = configuration.port;
            kcpTransport.Port = configuration.port;
        }
        else
        {
            Debug.Log("**** ADD THIS TO YOUR CONFIGURATION **** -- IP: " + response.IPV4Address + " Port: " + (ushort)response.Ports[0].Num);
            networkManager.networkAddress = response.IPV4Address;
            telepathyTransport.port = (ushort)response.Ports[0].Num;
            kcpTransport.Port = (ushort)response.Ports[0].Num;
        }

        networkManager.StartClient();
    }

    private void OnRequestMultiplayerServerError(PlayFabError error)
    {
        Debug.Log(error.ErrorDetails);
    }
}
