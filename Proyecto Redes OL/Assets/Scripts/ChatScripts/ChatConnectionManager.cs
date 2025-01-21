using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ChatConnectionManager : MonoBehaviour
{
    private static ChatConnectionManager singleton;
    public static ChatConnectionManager Singleton => singleton;

    [SerializeField] TMPro.TMP_InputField ipAddressInput;
    [SerializeField] TMPro.TMP_InputField portNumberInput;
    [SerializeField] TMPro.TMP_InputField chatUserNameInput;
    public TMPro.TMP_InputField ChatUserNameInput => chatUserNameInput;


    [SerializeField] GameObject connectionGroupPanel;
    [SerializeField] GameObject connectFeedbackPanel;
    [SerializeField] GameObject disconnectedFeedbackPanel;
    [SerializeField] GameObject chatPanel;
    private float clientConnectionTimeout = 10f;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else { Destroy(this.gameObject); }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedMethod;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;

        connectionGroupPanel.SetActive(true);
        disconnectedFeedbackPanel.SetActive(false);
        connectFeedbackPanel.SetActive(false);
        chatPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStartedMethod;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedMethod;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedMethod;
            NetworkManager.Singleton.OnClientStopped -= OnClientStoppedMethod;
        }
    }
    private IEnumerator cancelConnectionBecauseTimeout()
    {
        yield return new WaitForSeconds(clientConnectionTimeout);
        NetworkManager.Singleton.Shutdown();
    }
    private void OnClientStartedMethod()
    {
        connectionGroupPanel.SetActive(false);
        connectFeedbackPanel.SetActive(true);

        StartCoroutine(cancelConnectionBecauseTimeout());
    }
    private void OnClientConnectedMethod(ulong connectedClientID)
    {
        if (NetworkManager.Singleton.LocalClientId == connectedClientID)
        {
            StopAllCoroutines();
            disconnectedFeedbackPanel.SetActive(false);
            connectionGroupPanel.SetActive(false);
            connectFeedbackPanel.SetActive(false);
            chatPanel.SetActive(true);
        }
    }

    private void OnClientDisconnectedMethod(ulong disconnectedClientID)
    {
        if (NetworkManager.Singleton.LocalClientId == disconnectedClientID)
        {
            disconnectedFeedbackPanel.SetActive(true);
            connectFeedbackPanel.SetActive(false);
            chatPanel.SetActive(false);
            connectionGroupPanel.SetActive(false);
        }
    }
    private void OnClientStoppedMethod(bool obj)
    {
        StopAllCoroutines();
        disconnectedFeedbackPanel.SetActive(true);
        connectFeedbackPanel.SetActive(false);
        chatPanel.SetActive(false);
        connectionGroupPanel.SetActive(true);
    }


    public void out_ConnectAsHost()
    {
        SetConnectionData();
        NetworkManager.Singleton.StartHost();
    }

    public void out_ConnectAsClient()
    {
        SetConnectionData();
        NetworkManager.Singleton.StartClient();

    }

    public void out_Disconnect()
    {
        NetworkManager.Singleton.Shutdown();

    }

    private void SetConnectionData()
    {
        UnityTransport myNetworkTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        myNetworkTransport.ConnectionData.Address = ipAddressInput.text;

        if (ushort.TryParse(portNumberInput.text, out ushort portSelected))
        {
            myNetworkTransport.ConnectionData.Port = portSelected;
        }
        else
        {
            Debug.Log("Port selected is not valid");
        }
    }

}
