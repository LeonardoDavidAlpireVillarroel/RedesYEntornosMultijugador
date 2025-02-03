using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct Username : INetworkSerializable
{
    public string username;
    public ulong userId;

    // Serialización para red
    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref username);
        serializer.SerializeValue(ref userId);
    }
}
public class ConnectionManager : NetworkBehaviour
{
    public static Username CurrentUser;

    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TMP_InputField portNumberInput;
    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private GameObject connectionGroupPanel;
    [SerializeField] private GameObject connectingFeedbackPanel;
    [SerializeField] private GameObject disconnectedFeedbackPanel;
    [SerializeField] private GameObject disconnectButton;
    [SerializeField] private GameObject lobbyGroupPanel;
    [SerializeField] private TMP_Text userList;
    [SerializeField] private GameObject chatPanel;
    [SerializeField] public TMP_Text errorMessage; // Campo para mostrar mensajes de error


    private static ConnectionManager singleton;
    public List<Username> connectedClients = new List<Username>(); // Lista de jugadores conectados

    public static ConnectionManager Singleton => singleton;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(gameObject); // ⬅️ No destruir al cambiar de escena
        }
        else
        {
            Destroy(gameObject); // Evita duplicados
        }
    }

    private void Start()
    {
        // Inicializa la lista de usuarios y suscribe eventos
        connectedClients = new List<Username>();
        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedMethod;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;

        // Configura los paneles de UI
        connectionGroupPanel.SetActive(false);
        disconnectedFeedbackPanel.SetActive(false);
        connectingFeedbackPanel.SetActive(false);
        lobbyGroupPanel.SetActive(false);
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStartedMethod;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedMethod;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedMethod;
            NetworkManager.Singleton.OnClientStopped -= OnClientStoppedMethod;
        }
    }

    // Iniciar como Host
    public void out_ConnectAsHost()
    {
        if (string.IsNullOrWhiteSpace(userNameInput.text))
        {
            errorMessage.text = "Debes introducir un nombre para ingresar.";
            return; // No permite continuar si el nombre está vacío
        }

        errorMessage.text = ""; // Borra cualquier mensaje de error previo
        NetworkManager.Singleton.StartHost();
    }

    // Iniciar como Cliente
    public void out_ConnectAsClient()
    {
        if (string.IsNullOrWhiteSpace(userNameInput.text))
        {
            errorMessage.text = "Debes introducir un nombre para ingresar.";
            return; // No permite continuar si el nombre está vacío
        }

        errorMessage.text = ""; // Borra cualquier mensaje de error previo
        NetworkManager.Singleton.StartClient();
    }

    // Desconectar jugador
    public void out_Disconnect()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            if (IsServer)
            {
                var clientToRemove = connectedClients.FirstOrDefault(client => client.userId == NetworkManager.Singleton.LocalClientId);
                if (!clientToRemove.Equals(default(Username)))
                {
                    connectedClients.Remove(clientToRemove);
                    UpdatePlayerListClientRpc(connectedClients.ToArray());
                }
            }

            NetworkManager.Singleton.Shutdown();

            disconnectedFeedbackPanel.SetActive(true);
            disconnectButton.SetActive(false);
            connectingFeedbackPanel.SetActive(false);
            connectionGroupPanel.SetActive(true);
            lobbyGroupPanel.SetActive(false);

            Debug.Log("Jugador desconectado correctamente.");
        }
        else
        {
            Debug.LogWarning("No estás conectado actualmente.");
        }
    }

    private IEnumerator cancelConnectionBecauseTimeout()
    {
        yield return new WaitForSeconds(10f);
        NetworkManager.Singleton.Shutdown();
    }

    private void OnClientStartedMethod()
    {
        connectionGroupPanel.SetActive(false);
        connectingFeedbackPanel.SetActive(true);
        chatPanel.SetActive(false);
        StartCoroutine(cancelConnectionBecauseTimeout());
    }

    private void OnClientConnectedMethod(ulong connectedClientID)
    {
        Debug.Log($"Cliente conectado: {connectedClientID}");

        if (NetworkManager.Singleton.LocalClientId == connectedClientID)
        {
            StopAllCoroutines();
            lobbyGroupPanel.SetActive(true);
            disconnectedFeedbackPanel.SetActive(false);
            connectingFeedbackPanel.SetActive(false);
            disconnectButton.SetActive(true);
            chatPanel.SetActive(true);
            

            if (userNameInput != null && !string.IsNullOrEmpty(userNameInput.text))
            {
                var newUser = new Username { userId = connectedClientID, username = userNameInput.text };
                NotifyPlayerReadyServerRpc(newUser);
            }
            else
            {
                Debug.LogError("El campo de nombre de usuario está vacío o no ha sido asignado.");
            }
        }
    }

    private void OnClientDisconnectedMethod(ulong disconnectedClientID)
    {
        if (IsServer)
        {
            var clientToRemove = connectedClients.FirstOrDefault(client => client.userId == disconnectedClientID);
            if (!clientToRemove.Equals(default(Username)))
            {
                connectedClients.Remove(clientToRemove);
                UpdatePlayerListClientRpc(connectedClients.ToArray());
            }
        }

        if (NetworkManager.Singleton.LocalClientId == disconnectedClientID)
        {
            disconnectedFeedbackPanel.SetActive(true);
            disconnectButton.SetActive(false);
            connectingFeedbackPanel.SetActive(false);
            connectionGroupPanel.SetActive(false);
            lobbyGroupPanel.SetActive(false);
        }
    }

    private void OnClientStoppedMethod(bool obj)
    {
        StopAllCoroutines();
        disconnectedFeedbackPanel.SetActive(true);
        disconnectButton.SetActive(false);
        connectingFeedbackPanel.SetActive(false);
        connectionGroupPanel.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyPlayerReadyServerRpc(Username clientData)
    {
        if (!connectedClients.Any(client => client.userId == clientData.userId))
        {
            connectedClients.Add(clientData);
            UpdatePlayerListClientRpc(connectedClients.ToArray());
        }
    }

    [ClientRpc]
    private void UpdatePlayerListClientRpc(Username[] clientList)
    {
        userList.text = "Usuarios conectados:";
        foreach (var client in clientList)
        {
            userList.text += $"\n{client.username} (ID: {client.userId})";
        }
    }
    public static void SetUserData(string name, ulong id)
    {
        CurrentUser.username = name;
        CurrentUser.userId = id;
    }
}
