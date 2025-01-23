using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estructura que almacena los datos del usuario para la red
[Serializable]
public struct Username : INetworkSerializable
{
    public string username; // Nombre del usuario
    public ulong userId;    // ID único del usuario

    // Serialización de los datos para la red
    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref username); // Serializa el nombre
        serializer.SerializeValue(ref userId);   // Serializa el ID
    }
}

public class ConnectionManager : NetworkBehaviour
{
    // --- Campos serializados para la UI ---
    [SerializeField] private TMP_InputField ipAddressInput; // Campo para la dirección IP
    [SerializeField] private TMP_InputField portNumberInput; // Campo para el número de puerto
    [SerializeField] private TMP_InputField userNameInput; // Campo para el nombre del usuario
    [SerializeField] private GameObject connectionGroupPanel; // Panel de conexión
    [SerializeField] private GameObject connectingFeedbackPanel; // Panel de retroalimentación durante la conexión
    [SerializeField] private GameObject disconnectedFeedbackPanel; // Panel de retroalimentación cuando se pierde conexión
    [SerializeField] private GameObject disconnectButton; // Botón de desconexión
    [SerializeField] private GameObject lobbyGroupPanel; // Panel del lobby
    [SerializeField] private TMP_Text userList; // Texto para mostrar la lista de usuarios

    // --- Campos privados ---
    private static ConnectionManager singleton; // Instancia única del ConnectionManager
    private List<Username> usersname; // Lista de usuarios
    private List<Username> connectedClients = new List<Username>(); // Lista de jugadores conectados
    private float clientConnectionTimeout = 10f; // Tiempo máximo de espera para la conexión

    // --- Propiedad Singleton ---
    public static ConnectionManager Singleton => singleton;

    // --- Métodos Unity ---
    private void Awake()
    {
        // Verifica si ya existe una instancia del singleton, si no, asigna esta instancia
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Destroy(this.gameObject); // Destruye la instancia duplicada
        }
    }

    private void Start()
    {
        usersname = new List<Username>(); // Inicializa la lista de usuarios

        // Suscripción a los eventos de NetworkManager
        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedMethod;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;

        // Configuración inicial de los paneles de UI
        connectionGroupPanel.SetActive(false);
        disconnectedFeedbackPanel.SetActive(false);
        connectingFeedbackPanel.SetActive(false);
        lobbyGroupPanel.SetActive(false);
    }

    public override void OnDestroy()
    {
        // Desuscripción de los eventos de NetworkManager al destruir el objeto
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStartedMethod;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedMethod;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedMethod;
            NetworkManager.Singleton.OnClientStopped -= OnClientStoppedMethod;
        }
    }

    // --- Métodos de Conexión ---
    public void out_ConnectAsHost()
    {
        // Inicia el servidor (host)
        NetworkManager.Singleton.StartHost();
    }

    public void out_ConnectAsClient()
    {
        // Inicia el cliente
        NetworkManager.Singleton.StartClient();
    }

    public void out_Disconnect()
    {
        // Desconecta al jugador si está conectado como cliente o host
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            if (IsServer)
            {
                // Si eres el servidor, elimina el cliente de la lista de jugadores
                var clientToRemove = connectedClients.FirstOrDefault(client => client.userId == NetworkManager.Singleton.LocalClientId);
                if (!clientToRemove.Equals(default(Username)))
                {
                    connectedClients.Remove(clientToRemove);
                    UpdatePlayerListClientRpc(connectedClients.ToArray());
                }
            }

            // Cierra la conexión
            NetworkManager.Singleton.Shutdown();

            // Actualiza la UI tras la desconexión
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
        // Espera el tiempo de conexión máximo y luego cierra la conexión
        yield return new WaitForSeconds(clientConnectionTimeout);
        NetworkManager.Singleton.Shutdown();
    }

    // --- Métodos de Eventos ---
    private void OnClientStartedMethod()
    {
        // Evento cuando el cliente comienza a conectarse
        connectionGroupPanel.SetActive(false);
        connectingFeedbackPanel.SetActive(true);
        StartCoroutine(cancelConnectionBecauseTimeout());
    }

    private void OnClientConnectedMethod(ulong connectedClientID)
    {
        // Evento cuando un cliente se conecta
        Debug.Log($"Cliente conectado: {connectedClientID}");

        // Si es el cliente local, actualiza la UI y registra el nombre de usuario
        if (NetworkManager.Singleton.LocalClientId == connectedClientID)
        {
            StopAllCoroutines(); // Detiene cualquier coroutine de timeout

            lobbyGroupPanel.SetActive(true);
            disconnectedFeedbackPanel.SetActive(false);
            connectingFeedbackPanel.SetActive(false);
            disconnectButton.SetActive(true);

            if (userNameInput != null && !string.IsNullOrEmpty(userNameInput.text))
            {
                var newUser = new Username { userId = connectedClientID, username = userNameInput.text };
                NotifyPlayerReadyServerRpc(newUser); // Notifica al servidor que el jugador está listo
            }
            else
            {
                Debug.LogError("El campo de nombre de usuario está vacío o no ha sido asignado.");
            }
        }
    }

    private void OnClientDisconnectedMethod(ulong disconnectedClientID)
    {
        // Evento cuando un cliente se desconecta
        if (IsServer)
        {
            // Elimina al cliente desconectado de la lista de jugadores si eres el servidor
            var clientToRemove = connectedClients.FirstOrDefault(client => client.userId == disconnectedClientID);
            if (!clientToRemove.Equals(default(Username)))
            {
                connectedClients.Remove(clientToRemove);
                UpdatePlayerListClientRpc(connectedClients.ToArray());
            }
        }

        // Si el cliente local se desconectó, actualiza la UI
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
        // Evento cuando el cliente se detiene
        StopAllCoroutines();
        disconnectedFeedbackPanel.SetActive(true);
        disconnectButton.SetActive(false);
        connectingFeedbackPanel.SetActive(false);
        connectionGroupPanel.SetActive(true);
    }

    // --- Métodos RPC ---
    [ServerRpc(RequireOwnership = false)]
    public void NotifyPlayerReadyServerRpc(Username clientData)
    {
        // Notifica al servidor que un jugador está listo y añade su nombre a la lista de jugadores
        if (!connectedClients.Any(client => client.userId == clientData.userId))
        {
            connectedClients.Add(clientData);
            UpdatePlayerListClientRpc(connectedClients.ToArray());
        }
    }

    [ClientRpc]
    private void UpdatePlayerListClientRpc(Username[] clientList)
    {
        // Actualiza la lista de jugadores conectados en la UI
        userList.text = "Usuarios conectados:";
        foreach (var client in clientList)
        {
            userList.text += $"\n{client.username} (ID: {client.userId})";
        }
    }

    // --- Métodos Auxiliares ---
    public string GetUsernameById(ulong userId)
    {
        // Devuelve el nombre de usuario dado su ID
        var user = usersname.FirstOrDefault(u => u.userId == userId);
        return user.username ?? "Unknown";
    }

    public void LoadMenuScene()
    {
        // Desconecta al jugador y carga la escena del menú
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("multiplayer");
    }
}
