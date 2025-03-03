using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WinGame : NetworkBehaviour
{
    [SerializeField] private GameObject winPanel; // Panel de victoria en el Canvas, se activa cuando se cumplen las condiciones.

    [SerializeField] private string lobbySceneName = "Multiplayer"; // Nombre de la escena del lobby.
    [SerializeField] private Button closeButton; // Nombre de la escena del lobby.

    private ConnectionManager connectionManager;

    private HashSet<ulong> playersInZone = new HashSet<ulong>(); // Almacena los IDs de los jugadores que entran a la zona de victoria.

    private void Start()
    {
        connectionManager = FindObjectOfType<ConnectionManager>(); // Buscar el ConnectionManager en la escena

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked); // Asignar el evento al bot�n
        }
    }

    // M�todo que se ejecuta cuando un jugador entra en la zona de victoria.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            PlayerEnteredZoneServerRpc(networkObject.OwnerClientId); // Notifica al servidor sobre el jugador que entr� en la zona.
        }
    }

    // M�todo que se ejecuta cuando un jugador sale de la zona de victoria.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            PlayerExitedZoneServerRpc(networkObject.OwnerClientId); // Notifica al servidor sobre el jugador que sali� de la zona.
        }
    }

  
    [ServerRpc(RequireOwnership = false)]
    private void PlayerEnteredZoneServerRpc(ulong clientId)
    {
        playersInZone.Add(clientId); 
        CheckPlayersInZone(); 
    }

   
    [ServerRpc(RequireOwnership = false)]
    private void PlayerExitedZoneServerRpc(ulong clientId)
    {
        playersInZone.Remove(clientId); 
    }

    // Revisa si todos los jugadores est�n en la zona y si ambas palancas est�n activadas.
    private void CheckPlayersInZone()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count; // Obtiene el n�mero total de jugadores.

        // Si todos los jugadores est�n en la zona y las palancas est�n activadas, muestra el panel de victoria.
        if (playersInZone.Count == totalPlayers && Lever.activatedLeversCount == Lever.totalLevers)
        {
            Debug.Log("Todos los jugadores est�n en la zona y ambas palancas est�n activadas. Mostrando panel de victoria.");

            ShowWinPanel();
        }
    }

    // Muestra el panel de victoria en el servidor.
    private void ShowWinPanel()
    {
        if (IsServer)
        {
            NotifyClientsWinClientRpc(); // Llama a un RPC para que todos los clientes vean el panel de victoria.
        }
    }


    [ClientRpc]
    private void NotifyClientsWinClientRpc()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true); // Activa el panel de victoria en todos los clientes.
        }
    }

    // M�todo que se llama cuando un jugador presiona el bot�n de regresar al lobby.
    public void RequestReturnToLobby()
    {
        if (IsServer)
        {
            ChangeSceneForAllClients(); // Si el servidor presiona el bot�n, cambia la escena para todos.
        }
        else
        {
            RequestReturnToLobbyServerRpc(); // Si es un cliente, pide al servidor que cambie la escena.
        }
    }

    
    [ServerRpc(RequireOwnership = false)]
    private void RequestReturnToLobbyServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Jugador {rpcParams.Receive.SenderClientId} solicit� volver al lobby.");
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // Cambia la escena para todos los jugadores.
    private void ChangeSceneForAllClients()
    {
        DestroyAllPlayers(); // Destruye los personajes antes de cambiar de escena.
        NotifyClientsToChangeSceneClientRpc(lobbySceneName); // Notifica a los clientes sobre el cambio de escena.
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single); // Cambia la escena en el servidor.
    }

    // M�todo que destruye todos los personajes antes de cambiar de escena.
    private void DestroyAllPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                Destroy(client.PlayerObject.gameObject); // Elimina al jugador de la escena.
            }
        }
    }

 
    [ClientRpc]
    private void NotifyClientsToChangeSceneClientRpc(string sceneName)
    {
        Debug.Log($"Cambiando a la escena {sceneName} para todos los clientes.");
    }
    private void OnCloseButtonClicked()
    {
        if (connectionManager != null)
        {
            connectionManager.out_Disconnect(); // Llamar al m�todo de desconexi�n
        }
        else
        {
            Debug.LogWarning("ConnectionManager no encontrado.");
        }

        // Cambiar la escena a "Multiplayer"
        ReturnToMultiplayer();
    }
    private void ReturnToMultiplayer()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            RequestReturnToLobbyServerRpc(); // Pedir al servidor cambiar la escena
        }
    }
}
