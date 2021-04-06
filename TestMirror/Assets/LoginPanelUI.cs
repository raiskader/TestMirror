using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanelUI : MonoBehaviour
{

    public ClientStartup clientStartUp;
    public ServerStartup serverStartUp;

    public Button loginButton;
    public Button startLocalServerButton;

    void Start()
    {
        loginButton.onClick.AddListener(clientStartUp.OnLoginUserButtonClick);
        startLocalServerButton.onClick.AddListener(serverStartUp.OnStartLocalServerButtonClick);
    }
}
